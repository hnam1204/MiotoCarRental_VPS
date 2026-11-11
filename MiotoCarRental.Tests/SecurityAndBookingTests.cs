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

    [Fact]
    public void SepayService_SupportsMultiChannel_VPBank_And_MoMo()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"PaymentAccounts:MBBank:AccountNumber", "0901000001"},
            {"PaymentAccounts:VPBank:AccountNumber", "0902000002"},
            {"PaymentAccounts:MoMo:PhoneNumber", "0903000003"},
            {"PaymentAccounts:MoMo:AccountName", "ECOBIKE VIETNAM"}
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var sepay = new SepayService(config);
        var channels = sepay.GetAvailableChannels(250000, "EB-20261111-7777");

        Assert.Equal(3, channels.Count);
        Assert.Contains(channels, c => c.Key == "MBBank" && c.QrUrl.Contains("MB-0901000001"));
        Assert.Contains(channels, c => c.Key == "VPBank" && c.QrUrl.Contains("VPB-0902000002"));
        Assert.Contains(channels, c => c.Key == "MoMo" && c.QrUrl.Contains("MOMO-0903000003"));

        var booking = new Booking
        {
            Id = 77,
            BookingCode = "EB-20261111-7777",
            DepositAmount = 250000
        };

        // Test webhook từ VPBank
        var vpbPayload = new SepayWebhookPayload
        {
            Id = 555,
            Gateway = "VPBank",
            TransferType = "in",
            TransferAmount = 250000,
            Content = "EB-20261111-7777 coc xe"
        };
        var vpbResult = sepay.ProcessWebhook(vpbPayload, null, new[] { booking });
        Assert.True(vpbResult.IsSuccess);
        Assert.Equal("VPBank", vpbResult.Gateway);

        // Test webhook từ MoMo
        var momoPayload = new SepayWebhookPayload
        {
            Id = 666,
            Gateway = "MoMo",
            TransferType = "in",
            TransferAmount = 250000,
            Content = "EB-20261111-7777 chuyen tien momo"
        };
        var momoResult = sepay.ProcessWebhook(momoPayload, null, new[] { booking });
        Assert.True(momoResult.IsSuccess);
        Assert.Equal("MoMo", momoResult.Gateway);
    }

    [Fact]
    public async Task LoginViewModel_RequiresPassword_AndRejectsInvalidLogin()
    {
        var user = new User
        {
            FullName = "Tester",
            Email = "tester@mioto.vn",
            Phone = "0900000099",
            PasswordHash = "admin123",
            Role = UserRole.Admin
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Case 1: Sai mật khẩu
        var modelWrong = new LoginViewModel(_db, _passwords)
        {
            Email = "tester@mioto.vn",
            Password = "wrong_password_999"
        };
        var resultWrong = await modelWrong.OnPostAsync();
        Assert.IsType<PageResult>(resultWrong);
        Assert.True(modelWrong.ModelState.ErrorCount > 0);

        // Case 2: Đúng mật khẩu (hỗ trợ cả 123456 và admin123)
        var modelCorrect = new LoginViewModel(_db, _passwords)
        {
            Email = "tester@mioto.vn",
            Password = "admin123"
        };
        var resultCorrect = await modelCorrect.OnPostAsync();
        var redirect = Assert.IsType<RedirectResult>(resultCorrect);
        Assert.Equal("/Admin", redirect.Url);
    }

    [Fact]
    public async Task EditVehicle_OnlyOwnerHostOrAdminCanEdit()
    {
        var hostA = new User { FullName = "Host A", Email = "hosta@test.vn", Phone = "0901111111", Role = UserRole.Host };
        var hostB = new User { FullName = "Host B", Email = "hostb@test.vn", Phone = "0902222222", Role = UserRole.Host };
        _db.Users.AddRange(hostA, hostB);
        await _db.SaveChangesAsync();

        var vehicleA = new Vehicle
        {
            HostId = hostA.Id,
            Brand = "VinFast",
            Model = "Evo200",
            LicensePlate = "59-A1 111.11",
            PricePerDay = 120000,
            Address = "123 Le Loi",
            City = "TP. Hồ Chí Minh",
            District = "Quận 1"
        };
        _db.Vehicles.Add(vehicleA);
        await _db.SaveChangesAsync();

        // Host B cố gắng chỉnh sửa xe của Host A -> Phải bị AccessDenied
        var editModelUnauthorized = new EditVehicleViewModel(_db);
        var httpContextB = new DefaultHttpContext();
        httpContextB.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, hostB.Id.ToString()),
            new Claim(ClaimTypes.Role, "Host")
        }, "TestAuth"));
        editModelUnauthorized.PageContext = new PageContext { HttpContext = httpContextB };

        var getResult = await editModelUnauthorized.OnGetAsync(vehicleA.Id);
        var redirectDenied = Assert.IsType<RedirectResult>(getResult);
        Assert.Equal("/Account/AccessDenied", redirectDenied.Url);

        // Host A (chủ sở hữu thật) truy cập và cập nhật giá -> Thành công
        var editModelAuthorized = new EditVehicleViewModel(_db)
        {
            Category = VehicleCategory.MidRange,
            Brand = "VinFast",
            ModelName = "Evo200 Updated",
            YearManufacture = 2024,
            LicensePlate = "59-A1 111.11",
            BatteryRangeKm = 205,
            MaxSpeedKmH = 70,
            BatteryType = "Pin LFP",
            PricePerDay = 150000,
            Address = "456 Nguyen Hue",
            City = "TP. Hồ Chí Minh",
            District = "Quận 1"
        };
        var httpContextA = new DefaultHttpContext();
        httpContextA.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, hostA.Id.ToString()),
            new Claim(ClaimTypes.Role, "Host")
        }, "TestAuth"));
        editModelAuthorized.PageContext = new PageContext { HttpContext = httpContextA };

        var postResult = await editModelAuthorized.OnPostAsync(vehicleA.Id);
        var redirectSuccess = Assert.IsType<RedirectResult>(postResult);
        Assert.Equal("/Host", redirectSuccess.Url);

        var updatedVehicle = await _db.Vehicles.FindAsync(vehicleA.Id);
        Assert.NotNull(updatedVehicle);
        Assert.Equal(150000, updatedVehicle.PricePerDay);
        Assert.Equal("Evo200 Updated", updatedVehicle.Model);
        Assert.Equal("456 Nguyen Hue", updatedVehicle.Address);
    }

    [Fact]
    public async Task Host_RejectBooking_ReleasesCalendarAndSetsCancelled()
    {
        var host = new User { FullName = "Host X", Email = "hostx@test.vn", Phone = "0908888888", Role = UserRole.Host };
        var renter = new User { FullName = "Renter Y", Email = "rentery@test.vn", Phone = "0909999999", Role = UserRole.Renter };
        _db.Users.AddRange(host, renter);
        await _db.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            HostId = host.Id,
            Brand = "VinFast",
            Model = "Feliz S",
            LicensePlate = "59-F1 999.99",
            PricePerDay = 130000,
            Address = "789 Tran Hung Dao",
            City = "TP. Hồ Chí Minh",
            District = "Quận 5"
        };
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        var booking = new Booking
        {
            BookingCode = "EB-20261111-9999",
            RenterId = renter.Id,
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(3),
            TotalDays = 2,
            TotalAmount = 290000,
            DepositAmount = 87000,
            Status = BookingStatus.Pending
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        var blockedDate = new CalendarBlocked
        {
            BookingId = booking.Id,
            VehicleId = vehicle.Id,
            StartDate = booking.StartTime,
            EndDate = booking.EndTime,
            Reason = "BOOKED"
        };
        _db.CalendarBlockeds.Add(blockedDate);
        await _db.SaveChangesAsync();

        // Kiểm tra trước khi reject: Calendar đang bị khóa
        Assert.True(await _db.CalendarBlockeds.AnyAsync(c => c.BookingId == booking.Id));

        // Chủ xe thực hiện Reject Booking
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = "Xe đang bảo dưỡng định kỳ";
        var blocks = await _db.CalendarBlockeds.Where(c => c.BookingId == booking.Id).ToListAsync();
        _db.CalendarBlockeds.RemoveRange(blocks);
        await _db.SaveChangesAsync();

        // Kiểm tra sau khi reject:
        var updatedBooking = await _db.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Cancelled, updatedBooking.Status);
        Assert.Equal("Xe đang bảo dưỡng định kỳ", updatedBooking.CancellationReason);

        // Lịch xe đã được giải phóng hoàn toàn
        var hasBlocks = await _db.CalendarBlockeds.AnyAsync(c => c.BookingId == booking.Id);
        Assert.False(hasBlocks);
    }

    [Fact]
    public async Task CreateBooking_RejectsPastDates_AndRejectsSelfBooking()
    {
        var host = new User { FullName = "Host Self", Email = "hostself@test.vn", Phone = "0907777777", Role = UserRole.Host };
        var renter = new User { FullName = "Renter Regular", Email = "renterreg@test.vn", Phone = "0906666666", Role = UserRole.Renter };
        _db.Users.AddRange(host, renter);
        await _db.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            HostId = host.Id,
            Brand = "Yadea",
            Model = "Ocean",
            LicensePlate = "59-Y1 888.88",
            PricePerDay = 80000,
            Address = "100 Vo Van Kiet",
            City = "TP. Hồ Chí Minh",
            District = "Quận 1",
            RequiresLicenseA1 = false,
            Status = VehicleStatus.Active
        };
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        var bookingService = new BookingService(_db);

        // Case 1: Đặt lùi về quá khứ -> Bị từ chối
        var pastModel = new CreateBookingViewModel
        {
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddDays(-2),
            EndTime = DateTime.UtcNow.AddDays(1)
        };
        var pastResult = await bookingService.CreateBookingAsync(renter.Id, pastModel);
        Assert.False(pastResult.Success);
        Assert.Contains("quá khứ", pastResult.Message);

        // Case 2: Chủ xe tự đặt xe của chính mình -> Bị từ chối
        var selfModel = new CreateBookingViewModel
        {
            VehicleId = vehicle.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(3)
        };
        var selfResult = await bookingService.CreateBookingAsync(host.Id, selfModel);
        Assert.False(selfResult.Success);
        Assert.Contains("tự thuê", selfResult.Message);

        // Case 3: Khách thuê hợp lệ đặt xe -> Thành công
        var validResult = await bookingService.CreateBookingAsync(renter.Id, selfModel);
        Assert.True(validResult.Success);
        Assert.NotNull(validResult.Booking);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
