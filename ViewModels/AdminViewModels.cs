using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.ViewModels
{
    public class AdminDashboardViewModel
    {
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
    }
}
