using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.ViewModels
{
    [Authorize(Roles = "Admin")]
    [IgnoreAntiforgeryToken]
    public class AdminDashboardViewModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public AdminDashboardViewModel() { }

        [ActivatorUtilitiesConstructor]
        public AdminDashboardViewModel(MiotoDbContext context)
        {
            _context = context;
        }

        public int TotalVehicles { get; set; }
        public int ActiveVehicles { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingLicenseCount { get; set; }
        public int TotalUsers { get; set; }

        public List<User> PendingLicenseUsers { get; set; } = new();
        public List<User> AllUsers { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<Booking> Bookings { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (_context == null) return;

            TotalVehicles = await _context.Vehicles.CountAsync();
            ActiveVehicles = await _context.Vehicles.CountAsync(v => v.Status == VehicleStatus.Active);
            TotalBookings = await _context.Bookings.CountAsync();
            CompletedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Completed);
            TotalRevenue = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
            PendingLicenseCount = await _context.Users.CountAsync(u => u.DriverLicenseStatus == DriverLicenseStatus.Pending || u.DriverLicenseStatus == DriverLicenseStatus.Unverified);
            TotalUsers = await _context.Users.CountAsync();
            PendingLicenseUsers = await _context.Users
                .Where(u => u.DriverLicenseStatus == DriverLicenseStatus.Pending || u.DriverLicenseStatus == DriverLicenseStatus.Unverified)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            Vehicles = await _context.Vehicles
                .Include(v => v.Host)
                .OrderByDescending(v => v.Id)
                .ToListAsync();
            Bookings = await _context.Bookings
                .Include(b => b.Renter)
                .Include(b => b.Vehicle)
                .OrderByDescending(b => b.CreatedAt)
                .Take(30)
                .ToListAsync();
            AllUsers = await _context.Users
                .OrderByDescending(u => u.Id)
                .Take(30)
                .ToListAsync();
        }
    }
}
