using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.ViewModels
{
    [Authorize(Roles = "Host,Admin")]
    public class CreateVehicleViewModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public CreateVehicleViewModel() { }

        [ActivatorUtilitiesConstructor]
        public CreateVehicleViewModel(MiotoDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng chọn Phân loại nhóm xe")]
        public VehicleCategory Category { get; set; } = VehicleCategory.MidRange;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng chọn hoặc nhập Hãng xe")]
        public string Brand { get; set; } = "VinFast";

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Dòng xe / Model")]
        public string Model { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Năm sản xuất")]
        [Range(2020, 2026, ErrorMessage = "Năm sản xuất từ 2020 đến 2026")]
        public int YearManufacture { get; set; } = 2024;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Biển số xe")]
        public string LicensePlate { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Quãng đường 1 lần sạc")]
        [Range(50, 400, ErrorMessage = "Quãng đường từ 50km đến 400km")]
        public int BatteryRangeKm { get; set; } = 200;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Tốc độ tối đa")]
        [Range(30, 120, ErrorMessage = "Tốc độ từ 30km/h đến 120km/h")]
        public int MaxSpeedKmH { get; set; } = 70;

        [BindProperty]
        public string BatteryType { get; set; } = "Pin LFP 3.5 kWh";

        [BindProperty]
        public bool RequiresLicenseA1 { get; set; } = true;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Giá thuê theo ngày")]
        [Range(50000, 1000000, ErrorMessage = "Giá thuê từ 50.000đ đến 1.000.000đ/ngày")]
        public decimal PricePerDay { get; set; } = 130000;

        [BindProperty]
        public bool InstantBooking { get; set; } = true;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ nhận xe")]
        public string Address { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng chọn Thành phố")]
        public string City { get; set; } = "TP. Hồ Chí Minh";

        [BindProperty]
        public string District { get; set; } = "Quận 1";

        [BindProperty]
        public int LimitKmPerDay { get; set; } = 150;

        [BindProperty]
        public decimal OverKmFee { get; set; } = 1000;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public string? ThumbnailUrl { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || _context == null)
            {
                return Page();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int hostId);

            if (await _context.Vehicles.AnyAsync(v => v.LicensePlate.ToLower() == LicensePlate.ToLower().Trim()))
            {
                ModelState.AddModelError("LicensePlate", "Biển số xe này đã tồn tại trên hệ thống.");
                return Page();
            }

            string finalThumbnail = string.IsNullOrWhiteSpace(ThumbnailUrl)
                ? (Brand == "Dat Bike"
                    ? "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?auto=format&fit=crop&w=800&q=80"
                    : "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?auto=format&fit=crop&w=800&q=80")
                : ThumbnailUrl.Trim();

            var vehicle = new Vehicle
            {
                HostId = hostId,
                Category = Category,
                Brand = Brand.Trim(),
                Model = Model.Trim(),
                YearManufacture = YearManufacture,
                LicensePlate = LicensePlate.ToUpper().Trim(),
                BatteryRangeKm = BatteryRangeKm,
                MaxSpeedKmH = MaxSpeedKmH,
                BatteryType = BatteryType,
                RequiresLicenseA1 = RequiresLicenseA1,
                PricePerDay = PricePerDay,
                InstantBooking = InstantBooking,
                Address = Address.Trim(),
                City = City.Trim(),
                District = District.Trim(),
                LimitKmPerDay = LimitKmPerDay,
                OverKmFee = OverKmFee,
                Description = Description,
                ThumbnailUrl = finalThumbnail,
                FeaturesJson = "[\"Pin LFP sạc đầy\", \"Chống nước IP67\", \"Kèm 2 nón bảo hiểm\", \"Củ sạc di động\"]",
                Status = VehicleStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đăng xe máy điện [{vehicle.Brand} {vehicle.Model}] thành công! Xe đã sẵn sàng cho khách thuê.";
            return Redirect("/Host");
        }
    }

    [Authorize(Roles = "Host,Admin")]
    public class HostDashboardViewModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public HostDashboardViewModel() { }

        [ActivatorUtilitiesConstructor]
        public HostDashboardViewModel(MiotoDbContext context)
        {
            _context = context;
        }

        public List<Vehicle> MyVehicles { get; set; } = new();
        public List<Booking> IncomingBookings { get; set; } = new();
        public int TotalVehicles { get; set; }
        public int ActiveBookingsCount { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task OnGetAsync()
        {
            if (_context == null) return;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int hostId);

            MyVehicles = await _context.Vehicles
                .Where(v => v.HostId == hostId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            var myVehicleIds = MyVehicles.Select(v => v.Id).ToList();

            IncomingBookings = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Renter)
                .Where(b => myVehicleIds.Contains(b.VehicleId))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            TotalRevenue = IncomingBookings
                .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.InProgress)
                .Sum(b => b.RentalFee);

            TotalVehicles = MyVehicles.Count;
            ActiveBookingsCount = IncomingBookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress);
        }
    }
}
