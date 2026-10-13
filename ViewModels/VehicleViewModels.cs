using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.ViewModels
{
    public class VehicleSearchViewModel
    {
        public VehicleCategory? Category { get; set; }
        public string? City { get; set; }
        public string? Brand { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? RequiresLicenseA1 { get; set; }
        public int? MinBatteryRangeKm { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InstantBookingOnly { get; set; }

        public List<Vehicle> Results { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class VehicleDetailViewModel
    {
        public Vehicle Vehicle { get; set; } = null!;
        public List<Review> Reviews { get; set; } = new();
        public double AverageRating { get; set; } = 5.0;
        
        public DateTime DefaultStartDate { get; set; } = DateTime.Today.AddDays(1);
        public DateTime DefaultEndDate { get; set; } = DateTime.Today.AddDays(3);
    }

    public class CreateBookingViewModel
    {
        public int VehicleId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool NeedsDelivery { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? Note { get; set; }
    }
}
