using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
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

    [Fact]
    public void VnPayService_CreatesSignedUrl_AndValidatesCallbackSuccessfully()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"VnPay:TmnCode", "TESTTMN"},
            {"VnPay:HashSecret", "TESTSECRET1234567890"},
            {"VnPay:BaseUrl", "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"},
            {"VnPay:Version", "2.1.0"},
            {"VnPay:Command", "pay"},
            {"VnPay:CurrCode", "VND"},
            {"VnPay:Locale", "vn"}
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var vnPay = new VnPayService(config);
        var booking = new Booking
        {
            Id = 99,
            BookingCode = "EB-TEST-99",
            DepositAmount = 150000
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("ecobike.vn");

        var url = vnPay.CreatePaymentUrl(booking, httpContext);

        Assert.Contains("vnp_Amount=15000000", url);
        Assert.Contains("vnp_TmnCode=TESTTMN", url);
        Assert.Contains("vnp_SecureHash=", url);

        // Test Callback Validation with valid signature
        var hashSecret = "TESTSECRET1234567890";
        var queryParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            {"vnp_Amount", "15000000"},
            {"vnp_BankCode", "NCB"},
            {"vnp_BankTranNo", "14589230"},
            {"vnp_CardType", "ATM"},
            {"vnp_OrderInfo", "Thanh toan coc xe EB-TEST-99"},
            {"vnp_PayDate", "20261111120000"},
            {"vnp_ResponseCode", "00"},
            {"vnp_TmnCode", "TESTTMN"},
            {"vnp_TransactionNo", "14589230"},
            {"vnp_TransactionStatus", "00"},
            {"vnp_TxnRef", "99_EB-TEST-99_12345"}
        };

        var hashData = new System.Text.StringBuilder();
        foreach (var kv in queryParams)
        {
            if (hashData.Length > 0) hashData.Append('&');
            hashData.Append(System.Net.WebUtility.UrlEncode(kv.Key));
            hashData.Append('=');
            hashData.Append(System.Net.WebUtility.UrlEncode(kv.Value));
        }

        string secureHash = vnPay.ComputeHmacSha512(hashSecret, hashData.ToString());

        var dict = queryParams.ToDictionary(
            k => k.Key,
            v => new StringValues(v.Value)
        );
        dict.Add("vnp_SecureHash", new StringValues(secureHash));

        var queryCollection = new QueryCollection(dict);
        var result = vnPay.ProcessCallback(queryCollection);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsValidSignature);
        Assert.Equal(99, result.BookingId);
        Assert.Equal("14589230", result.TransactionNo);
        Assert.Equal("NCB", result.BankCode);
    }

    [Fact]
    public void VnPayService_RejectsTamperedSignature()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"VnPay:TmnCode", "TESTTMN"},
            {"VnPay:HashSecret", "TESTSECRET1234567890"}
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var vnPay = new VnPayService(config);

        var queryCollection = new QueryCollection(new Dictionary<string, StringValues>
        {
            {"vnp_Amount", "15000000"},
            {"vnp_ResponseCode", "00"},
            {"vnp_TransactionStatus", "00"},
            {"vnp_TxnRef", "99_EB-TEST-99_12345"},
            {"vnp_SecureHash", "INVALID_HASH_VALUE_TAMPERED"}
        });

        var result = vnPay.ProcessCallback(queryCollection);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsValidSignature);
    }

    [Fact]
    public void SepayService_CreatesValidVietQrUrl_AndProcessesWebhookSuccessfully()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Sepay:BankCode", "MB"},
            {"Sepay:BankName", "MBBank"},
            {"Sepay:AccountNumber", "0901000001"},
            {"Sepay:AccountName", "ECOBIKE VIETNAM"},
            {"Sepay:ApiKey", "TEST_KEY_123"}
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var sepay = new SepayService(config);

        var qrUrl = sepay.GetVietQrUrl(180000, "EB-20261111-9999");
        Assert.Contains("img.vietqr.io/image/MB-0901000001", qrUrl);
        Assert.Contains("amount=180000", qrUrl);
        Assert.Contains("addInfo=EB-20261111-9999", qrUrl);

        var booking = new Booking
        {
            Id = 55,
            BookingCode = "EB-20261111-9999",
            DepositAmount = 180000,
            Status = BookingStatus.AwaitingPayment
        };

        var payload = new SepayWebhookPayload
        {
            Id = 98765,
            Gateway = "MBBank",
            TransferType = "in",
            TransferAmount = 180000,
            Content = "Thanh toan coc xe EB-20261111-9999 tu app MBBank",
            ReferenceCode = "FT2611110098765"
        };

        var result = sepay.ProcessWebhook(payload, "Apikey TEST_KEY_123", new[] { booking });

        Assert.True(result.IsSuccess);
        Assert.True(result.IsValidApiKey);
        Assert.Equal(55, result.BookingId);
        Assert.Equal("FT2611110098765", result.ReferenceCode);
        Assert.Equal(180000, result.Amount);
    }

    [Fact]
    public void SepayService_RejectsUnderpaidOrOutgoingTransaction()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Sepay:ApiKey", "TEST_KEY_123"}
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var sepay = new SepayService(config);
        var booking = new Booking
        {
            Id = 56,
            BookingCode = "EB-20261111-8888",
            DepositAmount = 200000
        };

        // Case 1: Tiền vào không đủ cọc (chuyển thiếu)
        var underpaidPayload = new SepayWebhookPayload
        {
            Id = 111,
            TransferType = "in",
            TransferAmount = 50000, // Cọc 200k nhưng chỉ chuyển 50k
            Content = "EB-20261111-8888"
        };
        var underpaidResult = sepay.ProcessWebhook(underpaidPayload, "Apikey TEST_KEY_123", new[] { booking });
        Assert.False(underpaidResult.IsSuccess);

        // Case 2: Tiền ra (chuyển đi)
        var outPayload = new SepayWebhookPayload
        {
            Id = 222,
            TransferType = "out",
            TransferAmount = 200000,
            Content = "EB-20261111-8888"
        };
        var outResult = sepay.ProcessWebhook(outPayload, "Apikey TEST_KEY_123", new[] { booking });
        Assert.False(outResult.IsSuccess);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
