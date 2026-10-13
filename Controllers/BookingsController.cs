using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.Services;
using MiotoCarRental.ViewModels;

namespace MiotoCarRental.Controllers
{
    public class BookingsController : Controller
    {
        private readonly MiotoDbContext _context;
        private readonly IBookingService _bookingService;

        public BookingsController(MiotoDbContext context, IBookingService bookingService)
        {
            _context = context;
            _bookingService = bookingService;
        }

        // POST: /Bookings/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateBookingViewModel model)
        {
            // Kiểm tra trạng thái đăng nhập
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập trước khi tiến hành đặt xe.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Vehicles", new { id = model.VehicleId }) });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int renterId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(renterId);
            if (user != null && user.DriverLicenseStatus != DriverLicenseStatus.Approved)
            {
                TempData["ErrorMessage"] = "Giấy phép lái xe của bạn chưa được phê duyệt. Vui lòng cập nhật hồ sơ để đặt xe.";
                return RedirectToAction("Profile", "Account");
            }

            var result = await _bookingService.CreateBookingAsync(renterId, model);

            if (!result.Success || result.Booking == null)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Details", "Vehicles", new { id = model.VehicleId });
            }

            TempData["SuccessMessage"] = "Tạo đơn đặt xe thành công! Vui lòng thanh toán cọc giữ chỗ.";
            return RedirectToAction(nameof(Details), new { id = result.Booking.Id });
        }

        // GET: /Bookings/Details/5 (Chỉ Renter của đơn, Host của xe, hoặc Admin mới có quyền xem)
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v.Host)
                .Include(b => b.Renter)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int currentUserId);
            bool isAdmin = User.IsInRole("Admin");
            bool isRenter = booking.RenterId == currentUserId;
            bool isHost = booking.Vehicle != null && booking.Vehicle.HostId == currentUserId;

            if (!isAdmin && !isRenter && !isHost)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(booking);
        }

        // GET: /Bookings/Contract/5 (Hợp đồng thuê xe điện tử & Biên bản bàn giao)
        [Authorize]
        public async Task<IActionResult> Contract(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v.Host)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int currentUserId);
            bool isAdmin = User.IsInRole("Admin");
            bool isRenter = booking.RenterId == currentUserId;
            bool isHost = booking.Vehicle != null && booking.Vehicle.HostId == currentUserId;

            if (!isAdmin && !isRenter && !isHost)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(booking);
        }

        // POST: /Bookings/ConfirmPayment (Chỉ Renter hoặc Admin)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int currentUserId);
            bool isAdmin = User.IsInRole("Admin");
            bool isRenter = booking.RenterId == currentUserId;

            if (!isAdmin && !isRenter)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var success = await _bookingService.ConfirmDepositAsync(id, $"PAY-{Guid.NewGuid().ToString()[..8].ToUpper()}");
            if (success)
            {
                TempData["SuccessMessage"] = "Thanh toán cọc 30% thành công! Chuyến xe của bạn đã được xác nhận.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Bookings/MyBookings
        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int renterId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _context.Bookings
                .Include(b => b.Vehicle)
                .Where(b => b.RenterId == renterId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // POST: /Bookings/Checkin
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkin(int id, int odo, int fuelLevel)
        {
            var booking = await _context.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int currentUserId);
            bool isAdmin = User.IsInRole("Admin");
            bool isRenter = booking.RenterId == currentUserId;
            bool isHost = booking.Vehicle != null && booking.Vehicle.HostId == currentUserId;

            if (!isAdmin && !isRenter && !isHost)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            await _bookingService.CheckinVehicleAsync(id, odo, fuelLevel);
            TempData["SuccessMessage"] = "Bàn giao xe thành công! Chúc bạn có một hành trình an toàn.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Bookings/Checkout
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(int id, int odo, int fuelLevel)
        {
            var booking = await _context.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int currentUserId);
            bool isAdmin = User.IsInRole("Admin");
            bool isRenter = booking.RenterId == currentUserId;
            bool isHost = booking.Vehicle != null && booking.Vehicle.HostId == currentUserId;

            if (!isAdmin && !isRenter && !isHost)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            await _bookingService.CheckoutVehicleAsync(id, odo, fuelLevel);
            TempData["SuccessMessage"] = "Nghiệm thu trả xe thành công! Cảm ơn bạn đã sử dụng dịch vụ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Bookings/AddReview
        [HttpPost]
        public async Task<IActionResult> AddReview(int bookingId, int rating, string? comment)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int renterId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.RenterId == renterId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chuyến đi hợp lệ.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.Status != BookingStatus.Completed)
            {
                TempData["ErrorMessage"] = "Chỉ có thể đánh giá sau khi đã hoàn tất trả xe.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            if (booking.Reviews.Any())
            {
                TempData["ErrorMessage"] = "Bạn đã gửi đánh giá cho chuyến đi này rồi.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            var review = new Review
            {
                BookingId = bookingId,
                ReviewerId = renterId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá và nhận xét!";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }
    }
}
