namespace MiotoCarRental.Models.Entities
{
    public enum BookingStatus
    {
        Pending,          // Chờ duyệt
        AwaitingPayment,  // Chờ thanh toán cọc giữ chỗ
        Confirmed,        // Đã cọc thành công, giữ xe
        InProgress,       // Đang trong hành trình thuê (đã check-in bàn giao xe & pin)
        Completed,        // Đã hoàn thành (check-out nghiệm thu)
        Cancelled         // Đã hủy
    }

    public class Booking
    {
        public int Id { get; set; }
        public string BookingCode { get; set; } = string.Empty; // VD: "EB-20261008-001"

        public int RenterId { get; set; }
        public User Renter { get; set; } = null!;

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalDays { get; set; }

        // Tài chính
        public decimal RentalFee { get; set; }
        public decimal InsuranceFee { get; set; } = 15000;      // Phí bảo hiểm xe máy 15k/ngày
        public decimal DeliveryFee { get; set; } = 0;           // Phí giao xe tận nơi (nếu có)
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }              // Cọc giữ chỗ (30%)
        public string CollateralType { get; set; } = "CCCD bản gốc hoặc cọc 2.000.000 đ";

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        // Nghiệm thu số hóa khi nhận xe máy điện (Check-in)
        public int? CheckinOdo { get; set; }
        public int? CheckinBatteryPercent { get; set; }          // Phần trăm pin lúc nhận: VD 100%
        public string? CheckinPhotos { get; set; }
        public DateTime? CheckinTime { get; set; }

        // Nghiệm thu số hóa khi trả xe (Check-out)
        public int? CheckoutOdo { get; set; }
        public int? CheckoutBatteryPercent { get; set; }
        public string? CheckoutPhotos { get; set; }
        public DateTime? CheckoutTime { get; set; }

        // Phụ phí phát sinh
        public decimal ExtraKmFee { get; set; } = 0;
        public decimal ExtraOtherFee { get; set; } = 0;
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
