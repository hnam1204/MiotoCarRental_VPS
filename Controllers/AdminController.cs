using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.ViewModels;

namespace MiotoCarRental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MiotoDbContext _context;

        public AdminController(MiotoDbContext context)
        {
            _context = context;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {

            var vm = new AdminDashboardViewModel
            {
                TotalVehicles = await _context.Vehicles.CountAsync(),
                ActiveVehicles = await _context.Vehicles.CountAsync(v => v.Status == VehicleStatus.Active),
                TotalBookings = await _context.Bookings.CountAsync(),
                CompletedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Completed),
                TotalRevenue = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress)
                    .SumAsync(b => (decimal?)b.TotalAmount) ?? 0,
                PendingLicenseCount = await _context.Users.CountAsync(u => u.DriverLicenseStatus == DriverLicenseStatus.Pending || u.DriverLicenseStatus == DriverLicenseStatus.Unverified),
                TotalUsers = await _context.Users.CountAsync(),
                PendingLicenseUsers = await _context.Users
                    .Where(u => u.DriverLicenseStatus == DriverLicenseStatus.Pending || u.DriverLicenseStatus == DriverLicenseStatus.Unverified)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync(),
                Vehicles = await _context.Vehicles
                    .Include(v => v.Host)
                    .OrderByDescending(v => v.Id)
                    .ToListAsync(),
                Bookings = await _context.Bookings
                    .Include(b => b.Renter)
                    .Include(b => b.Vehicle)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(30)
                    .ToListAsync(),
                AllUsers = await _context.Users
                    .OrderByDescending(u => u.Id)
                    .Take(30)
                    .ToListAsync()
            };

            return View(vm);
        }

        // POST: /Admin/ApproveLicense
        [HttpPost]
        public async Task<IActionResult> ApproveLicense(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.DriverLicenseStatus = DriverLicenseStatus.Approved;
                user.RejectionReason = null;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã phê duyệt Giấy phép lái xe A1 cho khách hàng {user.FullName}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/RejectLicense
        [HttpPost]
        public async Task<IActionResult> RejectLicense(int userId, string reason)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.DriverLicenseStatus = DriverLicenseStatus.Rejected;
                user.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Hình ảnh GPLX bị mờ hoặc không trùng khớp thông tin CCCD." : reason.Trim();
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã từ chối hồ sơ GPLX của khách hàng {user.FullName}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/ToggleVehicleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleVehicleStatus(int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle != null)
            {
                vehicle.Status = vehicle.Status == VehicleStatus.Active ? VehicleStatus.Inactive : VehicleStatus.Active;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã chuyển trạng thái xe {vehicle.Model} ({vehicle.LicensePlate}) sang {(vehicle.Status == VehicleStatus.Active ? "HOẠT ĐỘNG" : "TẠM KHÓA")}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/CancelBooking
        [HttpPost]
        public async Task<IActionResult> CancelBooking(int bookingId, string reason)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Admin hủy đơn theo yêu cầu kiểm duyệt." : reason.Trim();

                // Giải phóng lịch khóa
                var blockedDates = await _context.CalendarBlockeds.Where(c => c.BookingId == bookingId).ToListAsync();
                if (blockedDates.Any())
                {
                    _context.CalendarBlockeds.RemoveRange(blockedDates);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã hủy chuyến thuê #{booking.BookingCode} và giải phóng lịch xe thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/UpdateUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int userId, UserRole newRole)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng cần phân quyền.";
                return RedirectToAction(nameof(Index));
            }

            var currentAdminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(currentAdminIdStr, out int currentAdminId);

            if (user.Id == currentAdminId && newRole != UserRole.Admin)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự hạ quyền Admin của chính tài khoản đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            var oldRole = user.Role;
            user.Role = newRole;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Phân quyền thành công! Tài khoản [{user.FullName} ({user.Email})] đã được chuyển từ [{oldRole}] sang [{newRole}].";
            return RedirectToAction(nameof(Index));
        }
    }
}
