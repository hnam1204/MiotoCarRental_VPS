using System.Net;
using Microsoft.Extensions.Configuration;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.Services
{
    public class SepayWebhookPayload
    {
        public long Id { get; set; }
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Code { get; set; }
        public string? Content { get; set; }
        public string? TransferType { get; set; } // "in"
        public decimal TransferAmount { get; set; }
        public decimal Accumulated { get; set; }
        public string? SubAccount { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
    }

    public class SepayProcessResult
    {
        public bool IsSuccess { get; set; }
        public bool IsValidApiKey { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ReferenceCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public interface ISepayService
    {
        string BankCode { get; }
        string BankName { get; }
        string AccountNumber { get; }
        string AccountName { get; }
        string GetVietQrUrl(decimal amount, string content);
        SepayProcessResult ProcessWebhook(SepayWebhookPayload payload, string? authHeader, IEnumerable<Booking> pendingBookings);
    }

    public class SepayService : ISepayService
    {
        private readonly IConfiguration _configuration;

        public SepayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string BankCode => _configuration["Sepay:BankCode"] ?? "MB";
        public string BankName => _configuration["Sepay:BankName"] ?? "MBBank (Ngân hàng Quân Đội)";
        public string AccountNumber => _configuration["Sepay:AccountNumber"] ?? "0901000001";
        public string AccountName => _configuration["Sepay:AccountName"] ?? "ECOBIKE VIETNAM";

        public string GetVietQrUrl(decimal amount, string content)
        {
            long intAmount = (long)amount;
            string cleanContent = WebUtility.UrlEncode(content.Trim());
            string cleanName = WebUtility.UrlEncode(AccountName.Trim());

            // Tạo mã VietQR chuẩn Napas 24/7 theo định dạng ngân hàng MBBank
            return $"https://img.vietqr.io/image/{BankCode}-{AccountNumber}-compact2.png?amount={intAmount}&addInfo={cleanContent}&accountName={cleanName}";
        }

        public SepayProcessResult ProcessWebhook(SepayWebhookPayload payload, string? authHeader, IEnumerable<Booking> pendingBookings)
        {
            string expectedApiKey = _configuration["Sepay:ApiKey"] ?? string.Empty;

            // Kiểm tra API Key bảo mật (nếu có cấu hình trong SePay)
            bool isValidKey = true;
            if (!string.IsNullOrEmpty(expectedApiKey))
            {
                if (string.IsNullOrEmpty(authHeader) || !authHeader.Contains(expectedApiKey, StringComparison.OrdinalIgnoreCase))
                {
                    isValidKey = false;
                }
            }

            // Chỉ xử lý giao dịch tiền vào (tiền nạp/chuyển khoản đến)
            if (payload.TransferType != null && !payload.TransferType.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                return new SepayProcessResult
                {
                    IsSuccess = false,
                    IsValidApiKey = isValidKey,
                    Message = "Bỏ qua giao dịch không phải tiền vào (transferType != 'in')."
                };
            }

            string content = (payload.Content ?? string.Empty) + " " + (payload.Description ?? string.Empty);
            string normalizedContent = content.Replace("-", "").Replace(" ", "").ToUpperInvariant();

            // Tìm đơn hàng tương ứng dựa trên BookingCode hoặc Id
            Booking? matchedBooking = null;
            foreach (var b in pendingBookings)
            {
                string normCode = b.BookingCode.Replace("-", "").ToUpperInvariant();
                if (!string.IsNullOrEmpty(normCode) && normalizedContent.Contains(normCode))
                {
                    matchedBooking = b;
                    break;
                }

                // Kiểm tra cú pháp dạng EB{id}
                if (normalizedContent.Contains($"EB{b.Id}") || normalizedContent.Contains($"ECOBIKE{b.Id}"))
                {
                    matchedBooking = b;
                    break;
                }
            }

            if (matchedBooking == null)
            {
                return new SepayProcessResult
                {
                    IsSuccess = false,
                    IsValidApiKey = isValidKey,
                    Amount = payload.TransferAmount,
                    Message = $"Không tìm thấy đơn đặt xe phù hợp trong nội dung chuyển khoản: '{content}'."
                };
            }

            // Kiểm tra số tiền chuyển khoản có đủ cọc không (cho phép dung sai nếu chuyển dư)
            if (payload.TransferAmount < matchedBooking.DepositAmount)
            {
                return new SepayProcessResult
                {
                    IsSuccess = false,
                    IsValidApiKey = isValidKey,
                    BookingId = matchedBooking.Id,
                    BookingCode = matchedBooking.BookingCode,
                    Amount = payload.TransferAmount,
                    Message = $"Số tiền chuyển ({payload.TransferAmount:N0}đ) nhỏ hơn số tiền cọc quy định ({matchedBooking.DepositAmount:N0}đ)."
                };
            }

            string refCode = !string.IsNullOrEmpty(payload.ReferenceCode) 
                ? payload.ReferenceCode 
                : $"MB-{payload.Id}";

            return new SepayProcessResult
            {
                IsSuccess = true,
                IsValidApiKey = isValidKey,
                BookingId = matchedBooking.Id,
                BookingCode = matchedBooking.BookingCode,
                Amount = payload.TransferAmount,
                ReferenceCode = refCode,
                Message = "Xác thực giao dịch chuyển khoản MBBank thành công!"
            };
        }
    }
}
