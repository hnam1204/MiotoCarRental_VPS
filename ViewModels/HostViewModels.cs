using System.ComponentModel.DataAnnotations;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.ViewModels
{
    public class CreateVehicleViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn Phân loại nhóm xe")]
        public VehicleCategory Category { get; set; } = VehicleCategory.MidRange;

        [Required(ErrorMessage = "Vui lòng chọn hoặc nhập Hãng xe")]
        public string Brand { get; set; } = "VinFast";

        [Required(ErrorMessage = "Vui lòng nhập Dòng xe / Model")]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Năm sản xuất")]
        [Range(2020, 2026, ErrorMessage = "Năm sản xuất từ 2020 đến 2026")]
        public int YearManufacture { get; set; } = 2024;

        [Required(ErrorMessage = "Vui lòng nhập Biển số xe")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Quãng đường 1 lần sạc")]
        [Range(50, 400, ErrorMessage = "Quãng đường từ 50km đến 400km")]
        public int BatteryRangeKm { get; set; } = 200;

        [Required(ErrorMessage = "Vui lòng nhập Tốc độ tối đa")]
        [Range(30, 120, ErrorMessage = "Tốc độ từ 30km/h đến 120km/h")]
        public int MaxSpeedKmH { get; set; } = 70;

        public string BatteryType { get; set; } = "Pin LFP 3.5 kWh";

        public bool RequiresLicenseA1 { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng nhập Giá thuê theo ngày")]
        [Range(50000, 1000000, ErrorMessage = "Giá thuê từ 50.000đ đến 1.000.000đ/ngày")]
        public decimal PricePerDay { get; set; } = 130000;

        public bool InstantBooking { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ nhận xe")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Thành phố")]
        public string City { get; set; } = "TP. Hồ Chí Minh";

        public string District { get; set; } = "Quận 1";

        public int LimitKmPerDay { get; set; } = 150;
        public decimal OverKmFee { get; set; } = 1000;

        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class HostDashboardViewModel
    {
        public List<Vehicle> MyVehicles { get; set; } = new();
        public List<Booking> IncomingBookings { get; set; } = new();
        public int TotalVehicles { get; set; }
        public int ActiveBookingsCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
