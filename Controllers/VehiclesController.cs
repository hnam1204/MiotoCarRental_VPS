using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.ViewModels;

namespace MiotoCarRental.Controllers
{
    public class VehiclesController : Controller
    {
        private readonly MiotoDbContext _context;

        public VehiclesController(MiotoDbContext context)
        {
            _context = context;
        }

        // GET: /Vehicles
        public async Task<IActionResult> Index([FromQuery] VehicleSearchViewModel filter)
        {
            var query = _context.Vehicles
                .Where(v => v.Status == VehicleStatus.Active)
                .AsQueryable();

            if (filter.Category.HasValue)
            {
                query = query.Where(v => v.Category == filter.Category.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(v => v.City.Contains(filter.City) || v.Address.Contains(filter.City));
            }

            if (!string.IsNullOrWhiteSpace(filter.Brand))
            {
                query = query.Where(v => v.Brand.Contains(filter.Brand));
            }

            if (filter.RequiresLicenseA1.HasValue)
            {
                query = query.Where(v => v.RequiresLicenseA1 == filter.RequiresLicenseA1.Value);
            }

            if (filter.MinBatteryRangeKm.HasValue && filter.MinBatteryRangeKm.Value > 0)
            {
                query = query.Where(v => v.BatteryRangeKm >= filter.MinBatteryRangeKm.Value);
            }

            if (filter.MaxPrice.HasValue && filter.MaxPrice.Value > 0)
            {
                query = query.Where(v => v.PricePerDay <= filter.MaxPrice.Value);
            }

            if (filter.InstantBookingOnly == true)
            {
                query = query.Where(v => v.InstantBooking);
            }

            // Lọc theo khoảng ngày (chống trùng lịch)
            if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            {
                var sDate = filter.StartDate.Value;
                var eDate = filter.EndDate.Value;

                var busyVehicleIds = await _context.CalendarBlockeds
                    .Where(c => c.StartDate < eDate && c.EndDate > sDate)
                    .Select(c => c.VehicleId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(v => !busyVehicleIds.Contains(v.Id));
            }

            var vehiclesList = await query.ToListAsync();
            filter.Results = vehiclesList.OrderBy(v => v.PricePerDay).ToList();
            filter.TotalCount = filter.Results.Count;

            return View(filter);
        }

        // GET: /Vehicles/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Host)
                .Include(v => v.Images)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            var reviews = await _context.Reviews
                .Include(r => r.Reviewer)
                .Where(r => r.Booking.VehicleId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var vm = new VehicleDetailViewModel
            {
                Vehicle = vehicle,
                Reviews = reviews,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 5.0
            };

            return View(vm);
        }
    }
}
