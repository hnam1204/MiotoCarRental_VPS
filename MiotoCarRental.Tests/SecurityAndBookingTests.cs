using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.Services;
using MiotoCarRental.ViewModels;
using Xunit;

namespace MiotoCarRental.Tests;

public sealed class SecurityAndBookingTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly MiotoDbContext _db;
    private readonly PasswordService _passwords = new();

    public SecurityAndBookingTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<MiotoDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new MiotoDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void LegacyPlaintextPassword_IsVerifiedAndUpgraded()
    {
        var user = new User { FullName = "Legacy", Email = "legacy@example.test", Phone = "0900000001", PasswordHash = "secret123" };

        var result = _passwords.VerifyAndUpgrade(user, "secret123");

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        Assert.NotEqual("secret123", user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, _passwords.VerifyAndUpgrade(user, "secret123"));
    }

    [Fact]
    public void NewPasswordHash_RejectsWrongPassword()
    {
        var user = new User { FullName = "New", Email = "new@example.test", Phone = "0900000002" };
        user.PasswordHash = _passwords.HashPassword(user, "correct horse");

        Assert.Equal(PasswordVerificationResult.Failed, _passwords.VerifyAndUpgrade(user, "wrong horse"));
        Assert.Equal(PasswordVerificationResult.Success, _passwords.VerifyAndUpgrade(user, "correct horse"));
    }

    [Fact]
    public async Task Registration_CannotEscalateToAdmin()
    {
        var model = new RegisterViewModel(_db, _passwords)
        {
            FullName = "Attacker",
            Email = "attacker@example.test",
            Phone = "0900000003",
            Role = UserRole.Admin,
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(await _db.Users.ToListAsync());
    }

    [Fact]
    public async Task BookingService_RejectsOverlappingBookingAndInvalidTransitions()
    {
        var host = new User { FullName = "Host", Email = "host@example.test", Phone = "0900000010", PasswordHash = "hash", Role = UserRole.Host };
        var renter = new User { FullName = "Renter", Email = "renter@example.test", Phone = "0900000011", PasswordHash = "hash", Role = UserRole.Renter };
        var vehicle = new Vehicle { Host = host, Brand = "Test", Model = "Bike", LicensePlate = "TEST-01", PricePerDay = 100000, Status = VehicleStatus.Active, InstantBooking = false };
        _db.AddRange(host, renter, vehicle);
        await _db.SaveChangesAsync();

        var start = DateTime.UtcNow.AddHours(2);
        var service = new BookingService(_db);
        var first = await service.CreateBookingAsync(renter.Id, new CreateBookingViewModel
        {
            VehicleId = vehicle.Id,
            StartTime = start,
            EndTime = start.AddDays(2)
        });

        var overlap = await service.CreateBookingAsync(renter.Id, new CreateBookingViewModel
        {
            VehicleId = vehicle.Id,
            StartTime = start.AddHours(1),
            EndTime = start.AddDays(1)
        });

        Assert.True(first.Success);
        Assert.False(overlap.Success);
        Assert.False(await service.ConfirmDepositAsync(first.Booking!.Id, "SIM-PAY-1"));

        first.Booking.Status = BookingStatus.AwaitingPayment;
        await _db.SaveChangesAsync();
        Assert.True(await service.ConfirmDepositAsync(first.Booking.Id, "SIM-PAY-2"));
        Assert.True(await service.CheckinVehicleAsync(first.Booking.Id, 100, 90));
        Assert.False(await service.CheckoutVehicleAsync(first.Booking.Id, 90, 90));
        Assert.True(await service.CheckoutVehicleAsync(first.Booking.Id, 120, 80));
    }

    [Fact]
    public async Task BookingDetails_DeniesUnrelatedUser()
    {
        var owner = new User { FullName = "Owner", Email = "owner@example.test", Phone = "0900000020", PasswordHash = "hash" };
        var host = new User { FullName = "Host", Email = "host2@example.test", Phone = "0900000021", PasswordHash = "hash", Role = UserRole.Host };
        var stranger = new User { FullName = "Stranger", Email = "stranger@example.test", Phone = "0900000022", PasswordHash = "hash" };
        var vehicle = new Vehicle { Host = host, Brand = "Test", Model = "Bike", LicensePlate = "TEST-02", PricePerDay = 100000 };
        var booking = new Booking { Renter = owner, Vehicle = vehicle, BookingCode = "TEST-BOOKING", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddDays(1), TotalDays = 1 };
        _db.AddRange(owner, host, stranger, vehicle, booking);
        await _db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, stranger.Id.ToString()),
                new Claim(ClaimTypes.Role, UserRole.Renter.ToString())
            }, "Test"))
        };
        var page = new BookingDetailsPageModel(_db)
        {
            PageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()))
        };

        var result = await page.OnGetAsync(booking.Id);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/AccessDenied", redirect.Url);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
