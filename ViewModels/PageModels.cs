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
    public class HomeIndexPageModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public HomeIndexPageModel() { }

        [ActivatorUtilitiesConstructor]
        public HomeIndexPageModel(MiotoDbContext context)
        {
            _context = context;
        }

        public List<Vehicle> FeaturedCars { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (_context == null) return;

            FeaturedCars = await _context.Vehicles
                .Where(v => v.Status == VehicleStatus.Active)
                .OrderByDescending(v => v.InstantBooking)
                .Take(6)
                .ToListAsync();

            ViewData["FeaturedCars"] = FeaturedCars;
        }
    }

    public class AccessDeniedPageModel : PageModel
    {
        public string? ReturnUrl { get; set; }

        public IActionResult OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            return Page();
        }
    }

    [Authorize]
    public class BecomeHostPageModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public BecomeHostPageModel() { }

        [ActivatorUtilitiesConstructor]
        public BecomeHostPageModel(MiotoDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (_context == null) return Page();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && (user.Role == UserRole.Host || user.Role == UserRole.Admin))
                {
                    return Redirect("/Host");
                }
            }
            return Page();
        }
    }

    [Authorize]
    public class BookingDetailsPageModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public BookingDetailsPageModel() { }

        [ActivatorUtilitiesConstructor]
        public BookingDetailsPageModel(MiotoDbContext context)
        {
            _context = context;
        }

        public Booking Booking { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (_context == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v.Host)
                .Include(b => b.Renter)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int currentUserId);
            bool isAdmin = User.IsInRole("Admin");
            bool isRenter = booking.RenterId == currentUserId;
            bool isHost = booking.Vehicle != null && booking.Vehicle.HostId == currentUserId;

            if (!isAdmin && !isRenter && !isHost)
            {
                return Redirect("/Account/AccessDenied");
            }

            Booking = booking;
            return Page();
        }
    }

    [Authorize]
    public class MyBookingsPageModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public MyBookingsPageModel() { }

        [ActivatorUtilitiesConstructor]
        public MyBookingsPageModel(MiotoDbContext context)
        {
            _context = context;
        }

        public List<Booking> Bookings { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (_context == null) return Redirect("/Account/Login");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int renterId))
            {
                return Redirect("/Account/Login");
            }

            Bookings = await _context.Bookings
                .Include(b => b.Vehicle)
                .Where(b => b.RenterId == renterId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Page();
        }
    }
}
