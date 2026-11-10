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

    public class PaymentChannelInfo
    {
        public string Key { get; set; } = string.Empty; // "MBBank", "VPBank", "MoMo"
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string QrUrl { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = string.Empty;
    }

    public class SepayProcessResult
    {
        public bool IsSuccess { get; set; }
        public bool IsValidApiKey { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Gateway { get; set; } = "MBBank";
        public string ReferenceCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public interface ISepayService
    {
        string BankCode { get; }
        string BankName { get; }
        string AccountNumber { get; }
        string AccountName { get; }
        string GetVietQrUrl(decimal amount, string content, string bankCode = "MB", string? accountNumber = null, string? accountName = null);
        List<PaymentChannelInfo> GetAvailableChannels(decimal amount, string content);
        SepayProcessResult ProcessWebhook(SepayWebhookPayload payload, string? authHeader, IEnumerable<Booking> pendingBookings);
    }

    public class SepayService : ISepayService
    {
        private readonly IConfiguration _configuration;

        public SepayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string BankCode => _configuration["PaymentAccounts:MBBank:BankCode"] ?? _configuration["Sepay:BankCode"] ?? "MB";
        public string BankName => _configuration["PaymentAccounts:MBBank:BankName"] ?? _configuration["Sepay:BankName"] ?? "MBBank (Ngân hàng Quân Đội)";
        public string AccountNumber => _configuration["PaymentAccounts:MBBank:AccountNumber"] ?? _configuration["Sepay:AccountNumber"] ?? "0901000001";
        public string AccountName => _configuration["PaymentAccounts:MBBank:AccountName"] ?? _configuration["Sepay:AccountName"] ?? "ECOBIKE VIETNAM";

        public string GetVietQrUrl(decimal amount, string content, string bankCode = "MB", string? accountNumber = null, string? accountName = null)
        {
            long intAmount = (long)amount;
            string targetBank = string.IsNullOrWhiteSpace(bankCode) ? "MB" : bankCode.Trim();
            string targetAcc = string.IsNullOrWhiteSpace(accountNumber) ? AccountNumber : accountNumber.Trim();
            string targetName = string.IsNullOrWhiteSpace(accountName) ? AccountName : accountName.Trim();

            string cleanContent = WebUtility.UrlEncode(content.Trim());
            string cleanName = WebUtility.UrlEncode(targetName);

            // Sinh mã QR Napas 24/7 theo từng ngân hàng / ví (MB, VPB, MOMO)
            return $"https://img.vietqr.io/image/{targetBank}-{targetAcc}-compact2.png?amount={intAmount}&addInfo={cleanContent}&accountName={cleanName}";
        }

        public List<PaymentChannelInfo> GetAvailableChannels(decimal amount, string content)
        {
            var list = new List<PaymentChannelInfo>();

            // 1. MBBank
            var mbAcc = _configuration["PaymentAccounts:MBBank:AccountNumber"] ?? "0901000001";
            var mbName = _configuration["PaymentAccounts:MBBank:AccountName"] ?? "ECOBIKE VIETNAM";
            list.Add(new PaymentChannelInfo
            {
                Key = "MBBank",
                Name = "MBBank (Quân Đội)",
                Code = "MB",
                AccountNumber = mbAcc,
                AccountName = mbName,
                QrUrl = GetVietQrUrl(amount, content, "MB", mbAcc, mbName),
                IconClass = "bi bi-bank2",
                BadgeColor = "blue"
            });

            // 2. VPBank
            var vpbAcc = _configuration["PaymentAccounts:VPBank:AccountNumber"] ?? "0902000002";
            var vpbName = _configuration["PaymentAccounts:VPBank:AccountName"] ?? "ECOBIKE VIETNAM";
            list.Add(new PaymentChannelInfo
            {
                Key = "VPBank",
                Name = "VPBank (Thịnh Vượng)",
                Code = "VPB",
                AccountNumber = vpbAcc,
                AccountName = vpbName,
                QrUrl = GetVietQrUrl(amount, content, "VPB", vpbAcc, vpbName),
                IconClass = "bi bi-building-fill",
                BadgeColor = "emerald"
            });

            // 3. Ví MoMo
            var momoPhone = _configuration["PaymentAccounts:MoMo:PhoneNumber"] ?? "0901000001";
            var momoName = _configuration["PaymentAccounts:MoMo:AccountName"] ?? "ECOBIKE VIETNAM";
            list.Add(new PaymentChannelInfo
            {
                Key = "MoMo",
                Name = "Ví Điện Tử MoMo",
                Code = "MOMO",
                AccountNumber = momoPhone,
                AccountName = momoName,
                QrUrl = GetVietQrUrl(amount, content, "MOMO", momoPhone, momoName),
                IconClass = "bi bi-wallet2",
                BadgeColor = "pink"
            });

            return list;
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

            string gateway = !string.IsNullOrEmpty(payload.Gateway) ? payload.Gateway : "MBBank";
            string refCode = !string.IsNullOrEmpty(payload.ReferenceCode) 
                ? payload.ReferenceCode 
                : $"{gateway.ToUpper()}-{payload.Id}";

            return new SepayProcessResult
            {
                IsSuccess = true,
                IsValidApiKey = isValidKey,
                BookingId = matchedBooking.Id,
                BookingCode = matchedBooking.BookingCode,
                Gateway = gateway,
                Amount = payload.TransferAmount,
                ReferenceCode = refCode,
                Message = $"Xác thực giao dịch chuyển khoản {gateway} thành công!"
            };
        }
    }
}
