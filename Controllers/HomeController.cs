using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.Controllers
{
    public class HomeController : Controller
    {
        private readonly MiotoDbContext _context;

        public HomeController(MiotoDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredCars = await _context.Vehicles
                .Where(v => v.Status == VehicleStatus.Active)
                .OrderByDescending(v => v.InstantBooking)
                .Take(6)
                .ToListAsync();

            ViewBag.FeaturedCars = featuredCars;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Stations()
        {
            return View();
        }
    }
}
