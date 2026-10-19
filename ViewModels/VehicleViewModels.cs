using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.ViewModels
{
    public class VehicleSearchViewModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public VehicleSearchViewModel() { }

        [ActivatorUtilitiesConstructor]
        public VehicleSearchViewModel(MiotoDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public VehicleCategory? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? City { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Brand { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? RequiresLicenseA1 { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MinBatteryRangeKm { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? InstantBookingOnly { get; set; }

        public List<Vehicle> Results { get; set; } = new();
        public int TotalCount { get; set; }

        public async Task OnGetAsync()
        {
            if (_context == null) return;

            var query = _context.Vehicles
                .Where(v => v.Status == VehicleStatus.Active)
                .AsQueryable();

            if (Category.HasValue)
                query = query.Where(v => v.Category == Category.Value);

            if (!string.IsNullOrWhiteSpace(City))
                query = query.Where(v => v.City.Contains(City) || v.Address.Contains(City));

            if (!string.IsNullOrWhiteSpace(Brand))
                query = query.Where(v => v.Brand.Contains(Brand));

            if (RequiresLicenseA1.HasValue)
                query = query.Where(v => v.RequiresLicenseA1 == RequiresLicenseA1.Value);

            if (MinBatteryRangeKm.HasValue && MinBatteryRangeKm.Value > 0)
                query = query.Where(v => v.BatteryRangeKm >= MinBatteryRangeKm.Value);

            if (MaxPrice.HasValue && MaxPrice.Value > 0)
                query = query.Where(v => v.PricePerDay <= MaxPrice.Value);

            if (InstantBookingOnly == true)
                query = query.Where(v => v.InstantBooking);

            if (StartDate.HasValue && EndDate.HasValue)
            {
                var sDate = StartDate.Value;
                var eDate = EndDate.Value;
                var busyVehicleIds = await _context.CalendarBlockeds
                    .Where(c => c.StartDate < eDate && c.EndDate > sDate)
                    .Select(c => c.VehicleId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(v => !busyVehicleIds.Contains(v.Id));
            }

            var vehiclesList = await query.ToListAsync();
            Results = vehiclesList.OrderBy(v => v.PricePerDay).ToList();
            TotalCount = Results.Count;
        }
    }

    public class VehicleDetailViewModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public VehicleDetailViewModel() { }

        [ActivatorUtilitiesConstructor]
        public VehicleDetailViewModel(MiotoDbContext context)
        {
            _context = context;
        }

        public Vehicle Vehicle { get; set; } = null!;
        public List<Review> Reviews { get; set; } = new();
        public double AverageRating { get; set; } = 5.0;

        public DateTime DefaultStartDate { get; set; } = DateTime.Today.AddDays(1);
        public DateTime DefaultEndDate { get; set; } = DateTime.Today.AddDays(3);

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (_context == null) return NotFound();

            var vehicle = await _context.Vehicles
                .Include(v => v.Host)
                .Include(v => v.Images)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null) return NotFound();

            Vehicle = vehicle;
            Reviews = await _context.Reviews
                .Include(r => r.Reviewer)
                .Where(r => r.Booking.VehicleId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            AverageRating = Reviews.Any() ? Reviews.Average(r => r.Rating) : 5.0;
            return Page();
        }
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
