using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.ViewModels;

namespace MiotoCarRental.Controllers
{
    [Authorize]
    public class HostController : Controller
    {
        private readonly MiotoDbContext _context;

        public HostController(MiotoDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int userId);
            return userId;
        }

        // GET: /Host/BecomeHost (Trang đăng ký làm đối tác chủ xe cho Renter)
        [HttpGet]
        public async Task<IActionResult> BecomeHost()
        {
            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user != null && (user.Role == UserRole.Host || user.Role == UserRole.Admin))
            {
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        // POST: /Host/BecomeHost (Kích hoạt quyền Host)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeHostConfirmed()
        {
            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            if (user.Role == UserRole.Renter)
            {
                user.Role = UserRole.Host;
                await _context.SaveChangesAsync();
                await SignInUserAsync(user);
                TempData["SuccessMessage"] = "Chúc mừng bạn đã kích hoạt tài khoản Đối Tác Chủ Xe (Host) thành công! Giờ đây bạn có thể đăng xe máy điện và quản lý đơn thuê.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Host (Dashboard Quản lý xe của tôi)
        [Authorize(Roles = "Host,Admin")]
        public async Task<IActionResult> Index()
        {
            int hostId = GetCurrentUserId();

            var vehicles = await _context.Vehicles
                .Where(v => v.HostId == hostId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            var myVehicleIds = vehicles.Select(v => v.Id).ToList();

            var incomingBookings = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Renter)
                .Where(b => myVehicleIds.Contains(b.VehicleId))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            decimal totalRevenue = incomingBookings
                .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.InProgress)
                .Sum(b => b.RentalFee);

            var vm = new HostDashboardViewModel
            {
                MyVehicles = vehicles,
                IncomingBookings = incomingBookings,
                TotalVehicles = vehicles.Count,
                ActiveBookingsCount = incomingBookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress),
                TotalRevenue = totalRevenue
            };

            return View(vm);
        }

        // GET: /Host/Create (Form Đăng xe máy điện mới)
        [Authorize(Roles = "Host,Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateVehicleViewModel());
        }

        // POST: /Host/Create
        [Authorize(Roles = "Host,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVehicleViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            int hostId = GetCurrentUserId();

            // Kiểm tra trùng biển số xe
            if (await _context.Vehicles.AnyAsync(v => v.LicensePlate.ToLower() == vm.LicensePlate.ToLower().Trim()))
            {
                ModelState.AddModelError("LicensePlate", "Biển số xe này đã tồn tại trên hệ thống.");
                return View(vm);
            }

            // Gán ảnh mẫu mặc định nếu chủ xe chưa tải ảnh
            string finalThumbnail = string.IsNullOrWhiteSpace(vm.ThumbnailUrl)
                ? (vm.Brand == "Dat Bike" 
                    ? "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?auto=format&fit=crop&w=800&q=80" 
                    : "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?auto=format&fit=crop&w=800&q=80")
                : vm.ThumbnailUrl.Trim();

            var vehicle = new Vehicle
            {
                HostId = hostId,
                Category = vm.Category,
                Brand = vm.Brand.Trim(),
                Model = vm.Model.Trim(),
                YearManufacture = vm.YearManufacture,
                LicensePlate = vm.LicensePlate.ToUpper().Trim(),
                BatteryRangeKm = vm.BatteryRangeKm,
                MaxSpeedKmH = vm.MaxSpeedKmH,
                BatteryType = vm.BatteryType,
                RequiresLicenseA1 = vm.RequiresLicenseA1,
                PricePerDay = vm.PricePerDay,
                InstantBooking = vm.InstantBooking,
                Address = vm.Address.Trim(),
                City = vm.City.Trim(),
                District = vm.District.Trim(),
                LimitKmPerDay = vm.LimitKmPerDay,
                OverKmFee = vm.OverKmFee,
                Description = vm.Description,
                ThumbnailUrl = finalThumbnail,
                FeaturesJson = "[\"Pin LFP sạc đầy\", \"Chống nước IP67\", \"Kèm 2 nón bảo hiểm\", \"Củ sạc di động\"]",
                Status = VehicleStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đăng xe máy điện [{vehicle.Brand} {vehicle.Model}] thành công! Xe đã sẵn sàng cho khách thuê.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Host/ToggleStatus/5 (Bật/Tắt trạng thái hoạt động của xe)
        [Authorize(Roles = "Host,Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            int hostId = GetCurrentUserId();
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.HostId == hostId);
            if (vehicle != null)
            {
                vehicle.Status = vehicle.Status == VehicleStatus.Active ? VehicleStatus.Inactive : VehicleStatus.Active;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái xe [{vehicle.Model}]: {(vehicle.Status == VehicleStatus.Active ? "Đang hoạt động" : "Tạm ngưng")}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Host/ApproveBooking/5 (Duyệt đơn thuê của khách)
        [Authorize(Roles = "Host,Admin")]
        [HttpPost]
        public async Task<IActionResult> ApproveBooking(int id)
        {
            int hostId = GetCurrentUserId();
            var booking = await _context.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id && b.Vehicle.HostId == hostId);
            if (booking != null && booking.Status == BookingStatus.Pending)
            {
                booking.Status = BookingStatus.AwaitingPayment;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã chấp nhận đơn đặt xe #{booking.BookingCode}. Khách hàng sẽ tiến hành thanh toán cọc.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Host/BlockDate (Chủ xe tự khóa lịch ngày bận)
        [Authorize(Roles = "Host,Admin")]
        [HttpPost]
        public async Task<IActionResult> BlockDate(int vehicleId, DateTime startDate, DateTime endDate)
        {
            int hostId = GetCurrentUserId();
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.HostId == hostId);
            if (vehicle != null && startDate < endDate)
            {
                var block = new CalendarBlocked
                {
                    VehicleId = vehicleId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Reason = "HOST_BUSY"
                };
                _context.CalendarBlockeds.Add(block);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã khóa lịch xe từ ngày {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("DriverLicenseStatus", user.DriverLicenseStatus.ToString()),
                new Claim("AvatarUrl", user.AvatarUrl ?? "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}
