namespace MiotoCarRental.Models.Entities
{
    public enum VehicleStatus
    {
        Pending,
        Active,
        Inactive,
        Maintenance
    }

    public enum VehicleCategory
    {
        Economy,      // Phổ thông - Học sinh / Sinh viên (Không cần bằng A1, giá rẻ)
        MidRange,     // Đô thị & Đi làm (Tầm trung)
        Touring,      // Đường dài - Pin khủng (Phượt)
        SportPremium  // Thể thao & Cao cấp
    }

    public class Vehicle
    {
        public int Id { get; set; }
        public int HostId { get; set; }
        public User Host { get; set; } = null!;

        public VehicleCategory Category { get; set; } = VehicleCategory.MidRange; // Phân loại nhóm xe

        public string Brand { get; set; } = string.Empty;       // VinFast, Dat Bike, Selex, Yadea, Pega...
        public string Model { get; set; } = string.Empty;       // Evo200, Feliz S, Klara S, Quantum, Vento S...
        public int YearManufacture { get; set; }                // 2023, 2024...
        public string LicensePlate { get; set; } = string.Empty;// 59-E1 123.45
        
        // Đặc thù xe máy điện
        public int BatteryRangeKm { get; set; } = 200;          // Quãng đường đi được 1 lần sạc đầy (km)
        public int MaxSpeedKmH { get; set; } = 70;              // Tốc độ tối đa (km/h)
        public string BatteryType { get; set; } = "Pin LFP";    // Pin LFP an toàn chống cháy nổ, Pin Lithium-ion
        public bool RequiresLicenseA1 { get; set; } = true;     // true: Cần bằng A1, false: Không cần bằng lái (dưới 50cc/Lite)
        
        public decimal PricePerDay { get; set; }                // Đơn giá thuê theo ngày (VND): 120.000đ - 220.000đ
        public bool InstantBooking { get; set; } = true;        // Đặt xe nhanh không cần duyệt
        
        // Địa chỉ & Vị trí nhận xe
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;        // TP.HCM, Hà Nội, Đà Nẵng...
        public string District { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Phụ phí & Giới hạn
        public int LimitKmPerDay { get; set; } = 150;           // 150 km/ngày định mức
        public decimal OverKmFee { get; set; } = 1000;          // 1.000đ/km vượt
        public decimal CleaningFee { get; set; } = 30000;       // Phí rửa xe dơ: 30.000đ
        public decimal LateReturnFeePerHour { get; set; } = 20000; // Trễ giờ: 20.000đ/giờ

        public VehicleStatus Status { get; set; } = VehicleStatus.Active;
        public string? Description { get; set; }
        public string? FeaturesJson { get; set; }               // JSON: ["ĐỔI PIN NHANH", "CỐP RỘNG 41L", "KẾT NỐI APP", "SẠC NHANH"]
        public string? ThumbnailUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<VehicleImage> Images { get; set; } = new List<VehicleImage>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<CalendarBlocked> BlockedDates { get; set; } = new List<CalendarBlocked>();
    }
}
