using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(Booking booking, HttpContext httpContext);
        VnPayPaymentResult ProcessCallback(IQueryCollection query);
        string ComputeHmacSha512(string key, string inputData);
    }

    public class VnPayPaymentResult
    {
        public bool IsSuccess { get; set; }
        public bool IsValidSignature { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? TransactionNo { get; set; }
        public string? BankCode { get; set; }
        public decimal Amount { get; set; }
        public string? ResponseCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(Booking booking, HttpContext httpContext)
        {
            var vnpayConfig = _configuration.GetSection("VnPay");
            string tmnCode = vnpayConfig["TmnCode"] ?? "CGXZPL01";
            string hashSecret = vnpayConfig["HashSecret"] ?? "XNBCJFAKAZQSGTARRLGCHVZWCIOIGSHN";
            string baseUrl = vnpayConfig["BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string version = vnpayConfig["Version"] ?? "2.1.0";
            string command = vnpayConfig["Command"] ?? "pay";
            string currCode = vnpayConfig["CurrCode"] ?? "VND";
            string locale = vnpayConfig["Locale"] ?? "vn";

            var now = DateTime.UtcNow.AddHours(7); // Giờ chuẩn Việt Nam (GMT+7)
            string createDate = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string expireDate = now.AddMinutes(15).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            // Xử lý IP Address
            string ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1" || string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = "127.0.0.1";
            }

            // Return URL
            string returnUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Payment/VnPayCallback";

            // TxnRef bao gồm Id và BookingCode để đối soát
            string txnRef = $"{booking.Id}_{booking.BookingCode}_{DateTime.UtcNow.Ticks}";

            // Số tiền cọc quy đổi sang đơn vị VNPay (nhân 100)
            long amount = (long)(booking.DepositAmount * 100);

            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "vnp_Version", version },
                { "vnp_Command", command },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", amount.ToString(CultureInfo.InvariantCulture) },
                { "vnp_CurrCode", currCode },
                { "vnp_TxnRef", txnRef },
                { "vnp_OrderInfo", $"Thanh toan coc xe {booking.BookingCode} don hang #{booking.Id}" },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", locale },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_IpAddr", ipAddress },
                { "vnp_CreateDate", createDate },
                { "vnp_ExpireDate", expireDate }
            };

            var queryBuilder = new StringBuilder();
            var dataBuilder = new StringBuilder();

            foreach (var kv in parameters)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    if (queryBuilder.Length > 0)
                    {
                        queryBuilder.Append('&');
                        dataBuilder.Append('&');
                    }

                    queryBuilder.Append(WebUtility.UrlEncode(kv.Key));
                    queryBuilder.Append('=');
                    queryBuilder.Append(WebUtility.UrlEncode(kv.Value));

                    dataBuilder.Append(WebUtility.UrlEncode(kv.Key));
                    dataBuilder.Append('=');
                    dataBuilder.Append(WebUtility.UrlEncode(kv.Value));
                }
            }

            string secureHash = ComputeHmacSha512(hashSecret, dataBuilder.ToString());
            queryBuilder.Append("&vnp_SecureHash=");
            queryBuilder.Append(secureHash);

            return $"{baseUrl}?{queryBuilder}";
        }

        public VnPayPaymentResult ProcessCallback(IQueryCollection query)
        {
            var vnpayConfig = _configuration.GetSection("VnPay");
            string hashSecret = vnpayConfig["HashSecret"] ?? "XNBCJFAKAZQSGTARRLGCHVZWCIOIGSHN";

            string secureHashFromVnPay = query["vnp_SecureHash"].ToString();
            string responseCode = query["vnp_ResponseCode"].ToString();
            string transactionStatus = query["vnp_TransactionStatus"].ToString();
            string transactionNo = query["vnp_TransactionNo"].ToString();
            string bankCode = query["vnp_BankCode"].ToString();
            string txnRef = query["vnp_TxnRef"].ToString();
            string amountStr = query["vnp_Amount"].ToString();

            decimal amount = 0;
            if (long.TryParse(amountStr, out long rawAmount))
            {
                amount = rawAmount / 100m;
            }

            // Tách bookingId từ txnRef
            int bookingId = 0;
            string bookingCode = string.Empty;
            if (!string.IsNullOrEmpty(txnRef))
            {
                var parts = txnRef.Split('_');
                if (parts.Length > 0) int.TryParse(parts[0], out bookingId);
                if (parts.Length > 1) bookingCode = parts[1];
            }

            // Sắp xếp các tham số để tính chữ ký xác thực
            var sortedParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in query.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                {
                    if (!key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                        !key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                    {
                        sortedParams.Add(key, query[key].ToString());
                    }
                }
            }

            var hashData = new StringBuilder();
            foreach (var kv in sortedParams)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    if (hashData.Length > 0) hashData.Append('&');
                    hashData.Append(WebUtility.UrlEncode(kv.Key));
                    hashData.Append('=');
                    hashData.Append(WebUtility.UrlEncode(kv.Value));
                }
            }

            string myHash = ComputeHmacSha512(hashSecret, hashData.ToString());
            bool isValidSignature = string.Equals(myHash, secureHashFromVnPay, StringComparison.OrdinalIgnoreCase);

            bool isSuccess = isValidSignature && responseCode == "00" && transactionStatus == "00";

            string message = responseCode switch
            {
                "00" => "Giao dịch thanh toán cọc thành công.",
                "24" => "Khách hàng đã hủy giao dịch thanh toán.",
                "11" => "Giao dịch đã hết hạn chờ thanh toán.",
                "51" => "Tài khoản không đủ số dư để thanh toán.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                _ => $"Thanh toán không thành công (Mã phản hồi: {responseCode})."
            };

            return new VnPayPaymentResult
            {
                IsSuccess = isSuccess,
                IsValidSignature = isValidSignature,
                BookingId = bookingId,
                BookingCode = bookingCode,
                TransactionNo = transactionNo,
                BankCode = bankCode,
                Amount = amount,
                ResponseCode = responseCode,
                Message = message
            };
        }

        public string ComputeHmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var b in hashValue)
            {
                hash.Append(b.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
