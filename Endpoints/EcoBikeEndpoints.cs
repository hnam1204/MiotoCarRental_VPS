using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.Services;
using MiotoCarRental.ViewModels;

namespace MiotoCarRental.Endpoints
{
    public static class EcoBikeEndpoints
    {
        public static void MapEcoBikeEndpoints(this WebApplication app)
        {
            // Redirect legacy /Home routes to Razor Pages root
            app.MapGet("/Home", () => Results.Redirect("/"));
            app.MapGet("/Home/Index", () => Results.Redirect("/"));
            app.MapGet("/Stations", () => Results.Redirect("/Home/Stations"));
            app.MapGet("/Vehicles/Index", (HttpContext ctx) => Results.Redirect("/Vehicles" + ctx.Request.QueryString));
            app.MapGet("/Host/Index", () => Results.Redirect("/Host"));
            app.MapGet("/Admin/Index", () => Results.Redirect("/Admin"));

            // =========================================================================
            // 1. RESTful JSON APIs (.NET Core 9 Web API Layer cho ReactJS Frontend)
            // =========================================================================
            app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html"));
            app.MapGet("/api-docs", () => Results.Redirect("/swagger/index.html"));
            app.MapGet("/react", () => Results.Redirect("/react/index.html"));

            var api = app.MapGroup("/api");

            api.MapGet("/vehicles", async (MiotoDbContext db, VehicleCategory? category, string? city, string? brand, bool? requiresLicense, decimal? maxPrice, string? search) =>
            {
                var query = db.Vehicles.Where(v => v.Status == VehicleStatus.Active).AsQueryable();
                if (category.HasValue) query = query.Where(v => v.Category == category.Value);
                if (!string.IsNullOrWhiteSpace(city)) query = query.Where(v => v.City.Contains(city));
                if (!string.IsNullOrWhiteSpace(brand)) query = query.Where(v => v.Brand.Contains(brand));
                if (requiresLicense.HasValue) query = query.Where(v => v.RequiresLicenseA1 == requiresLicense.Value);
                if (maxPrice.HasValue && maxPrice.Value > 0) query = query.Where(v => v.PricePerDay <= maxPrice.Value);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var kw = search.Trim().ToLower();
                    query = query.Where(v => v.Brand.ToLower().Contains(kw) || v.Model.ToLower().Contains(kw) || v.City.ToLower().Contains(kw));
                }

                var list = await query.Select(v => new
                {
                    v.Id,
                    v.Brand,
                    v.Model,
                    v.LicensePlate,
                    v.YearManufacture,
                    Category = v.Category.ToString(),
                    CategoryId = (int)v.Category,
                    v.BatteryRangeKm,
                    v.MaxSpeedKmH,
                    v.BatteryType,
                    v.RequiresLicenseA1,
                    v.PricePerDay,
                    v.City,
                    v.District,
                    v.Address,
                    v.InstantBooking,
                    v.ThumbnailUrl,
                    v.LimitKmPerDay
                }).ToListAsync();

                return Results.Ok(list.OrderBy(v => v.PricePerDay));
            });

            api.MapGet("/vehicles/{id:int}", async (int id, MiotoDbContext db) =>
            {
                var v = await db.Vehicles.Include(x => x.Host).FirstOrDefaultAsync(x => x.Id == id);
                if (v is null) return Results.NotFound(new { message = "Không tìm thấy phương tiện" });
                return Results.Ok(new
                {
                    v.Id,
                    v.Brand,
                    v.Model,
                    v.LicensePlate,
                    v.YearManufacture,
                    Category = v.Category.ToString(),
                    v.BatteryRangeKm,
                    v.MaxSpeedKmH,
                    v.BatteryType,
                    v.RequiresLicenseA1,
                    v.PricePerDay,
                    v.City,
                    v.District,
                    v.Address,
                    v.InstantBooking,
                    v.ThumbnailUrl,
                    v.Description,
                    HostName = v.Host?.FullName ?? "Đối tác EcoBike"
                });
            });

            api.MapGet("/stats", async (MiotoDbContext db) =>
            {
                var vehicles = await db.Vehicles.ToListAsync();
                var bookings = await db.Bookings.ToListAsync();
                var totalGmv = bookings.Where(b => b.Status != BookingStatus.Cancelled).Sum(b => b.TotalAmount);
                return Results.Ok(new
                {
                    Architecture = "ReactJS Frontend (ClientApp) + ASP.NET Core 9.0 Web API + 3-Layer Services",
                    TotalVehicles = vehicles.Count,
                    TotalUsers = await db.Users.CountAsync(),
                    TotalBookings = bookings.Count,
                    TotalGMV = totalGmv,
                    PlatformCommission = Math.Round(totalGmv * 0.15m, 0),
                    Categories = new
                    {
                        Economy = vehicles.Count(v => v.Category == VehicleCategory.Economy),
                        MidRange = vehicles.Count(v => v.Category == VehicleCategory.MidRange),
                        Touring = vehicles.Count(v => v.Category == VehicleCategory.Touring),
                        SportPremium = vehicles.Count(v => v.Category == VehicleCategory.SportPremium)
                    }
                });
            });

            api.MapGet("/coupons", () =>
            {
                var coupons = new[]
                {
                    new { Id = 1, Code = "ECOBIKE50", Description = "Giảm 50% tối đa 50.000đ cho chuyến đầu tiên", DiscountPercent = 50, MaxDiscountAmount = 50000m },
                    new { Id = 2, Code = "SINHVIEN20", Description = "Ưu đãi 20% tối đa 40.000đ dành cho Học sinh - Sinh viên", DiscountPercent = 20, MaxDiscountAmount = 40000m },
                    new { Id = 3, Code = "GREEN10", Description = "Giảm 10% tối đa 100.000đ cho hành trình xanh", DiscountPercent = 10, MaxDiscountAmount = 100000m }
                };
                return Results.Ok(coupons);
            });

            api.MapGet("/bookings", async (MiotoDbContext db) =>
            {
                var list = await db.Bookings
                    .Include(b => b.Vehicle)
                    .Include(b => b.Renter)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(20)
                    .Select(b => new
                    {
                        b.Id,
                        b.BookingCode,
                        VehicleName = b.Vehicle != null ? b.Vehicle.Brand + " " + b.Vehicle.Model : "Xe máy điện",
                        RenterName = b.Renter != null ? b.Renter.FullName : "Khách thuê",
                        PickupDate = b.StartTime,
                        ReturnDate = b.EndTime,
                        b.TotalDays,
                        b.RentalFee,
                        b.InsuranceFee,
                        b.DepositAmount,
                        b.TotalAmount,
                        Status = b.Status.ToString()
                    })
                    .ToListAsync();
                return Results.Ok(list);
            });

            api.MapPost("/bookings/calculate", async (MiotoDbContext db, int vehicleId, int days, string? couponCode) =>
            {
                var vehicle = await db.Vehicles.FindAsync(vehicleId);
                if (vehicle == null) return Results.NotFound(new { message = "Không tìm thấy xe" });

                int validDays = Math.Max(1, days);
                decimal rentalFee = vehicle.PricePerDay * validDays;
                decimal insuranceFee = 15000m * validDays;
                decimal discount = 0m;

                if (!string.IsNullOrWhiteSpace(couponCode))
                {
                    var code = couponCode.Trim().ToUpper();
                    if (code == "ECOBIKE50") discount = Math.Min(Math.Round(rentalFee * 0.50m, 0), 50000m);
                    else if (code == "SINHVIEN20") discount = Math.Min(Math.Round(rentalFee * 0.20m, 0), 40000m);
                    else if (code == "GREEN10") discount = Math.Min(Math.Round(rentalFee * 0.10m, 0), 100000m);
                }

                decimal totalAmount = rentalFee + insuranceFee - discount;
                decimal deposit = Math.Round(totalAmount * 0.3m, 0);

                return Results.Ok(new
                {
                    VehicleId = vehicle.Id,
                    VehicleName = $"{vehicle.Brand} {vehicle.Model}",
                    PricePerDay = vehicle.PricePerDay,
                    Days = validDays,
                    RentalFee = rentalFee,
                    InsuranceFee = insuranceFee,
                    CouponCode = couponCode?.ToUpper(),
                    DiscountAmount = discount,
                    TotalAmount = totalAmount,
                    DepositRequired = deposit
                });
            }).DisableAntiforgery();

            api.MapGet("/stations", () =>
            {
                var stations = new[]
                {
                    new { Id = 1, Name = "Trạm Sạc Siêu Tốc V-GREEN Vinhomes Central Park", City = "TP. Hồ Chí Minh", District = "Bình Thạnh", Type = "FastCharge", Ports = 24, Status = "Hoạt động 24/7" },
                    new { Id = 2, Name = "Trạm Đổi Pin Tự Động Selex Nguyễn Huệ", City = "TP. Hồ Chí Minh", District = "Quận 1", Type = "BatterySwap", Ports = 16, Status = "Hoạt động 24/7" },
                    new { Id = 3, Name = "Dat Bike Supercharge Station Võ Văn Tần", City = "TP. Hồ Chí Minh", District = "Quận 3", Type = "SuperCharge", Ports = 12, Status = "Hoạt động 24/7" },
                    new { Id = 4, Name = "Trạm Sạc V-GREEN Royal City Nguyễn Trãi", City = "Hà Nội", District = "Thanh Xuân", Type = "FastCharge", Ports = 30, Status = "Hoạt động 24/7" },
                    new { Id = 5, Name = "Trạm Đổi Pin & Sạc Nhanh Cầu Rồng", City = "Đà Nẵng", District = "Hải Châu", Type = "BatterySwap", Ports = 18, Status = "Hoạt động 24/7" }
                };
                return Results.Ok(stations);
            });

            // =========================================================================
            // 2. ACCOUNT & AUTHENTICATION ENDPOINTS
            // =========================================================================
            app.MapPost("/Account/Login", async (HttpContext http, MiotoDbContext db, IPasswordService passwordService, [FromForm] string? email, [FromForm] string? password, [FromForm] bool rememberMe, [FromForm] string? returnUrl) =>
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SetTempMessage(http, "ErrorMessage", "Vui lòng nhập đầy đủ Email và Mật khẩu!");
                    return Results.Redirect($"/Account/Login?returnUrl={returnUrl}");
                }

                var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower().Trim());
                bool isValid = false;

                if (user != null)
                {
                    var verify = passwordService.VerifyAndUpgrade(user, password.Trim());
                    if (verify != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                    {
                        isValid = true;
                        if (verify == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded)
                        {
                            await db.SaveChangesAsync();
                        }
                    }
                }

                if (!isValid || user == null)
                {
                    SetTempMessage(http, "ErrorMessage", "Email hoặc mật khẩu không chính xác!");
                    return Results.Redirect($"/Account/Login?returnUrl={returnUrl}");
                }

                await SignInUserAsync(http, user, rememberMe);
                SetTempMessage(http, "SuccessMessage", $"Đăng nhập thành công! Chào mừng {user.FullName}.");

                if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
                {
                    return Results.Redirect(returnUrl);
                }

                if (user.Role == UserRole.Admin) return Results.Redirect("/Admin");
                if (user.Role == UserRole.Host) return Results.Redirect("/Host");
                return Results.Redirect("/");
            }).DisableAntiforgery();

            app.MapPost("/Account/QuickLogin", (HttpContext http) =>
            {
                SetTempMessage(http, "ErrorMessage", "Tính năng đăng nhập nhanh đã bị vô hiệu hóa. Vui lòng nhập Email và Mật khẩu để xác thực tài khoản!");
                return Results.Redirect("/Account/Login");
            }).DisableAntiforgery();

            app.MapPost("/Account/Logout", async (HttpContext http) =>
            {
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                SetTempMessage(http, "SuccessMessage", "Bạn đã đăng xuất khỏi hệ thống an toàn.");
                return Results.Redirect("/");
            }).DisableAntiforgery();

            app.MapPost("/Account/UpdateLicense", [Authorize] async (HttpContext http, MiotoDbContext db, [FromForm] string licenseNo, [FromForm] string? licenseImageUrl) =>
            {
                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var user = await db.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.DriverLicenseNo = licenseNo.Trim();
                        user.DriverLicenseImage = licenseImageUrl ?? "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?w=600";
                        user.DriverLicenseStatus = DriverLicenseStatus.Approved;
                        await db.SaveChangesAsync();
                        await SignInUserAsync(http, user, true);
                        SetTempMessage(http, "SuccessMessage", "Cập nhật Giấy phép lái xe thành công! Hồ sơ đã được phê duyệt.");
                    }
                }
                return Results.Redirect("/Account/Profile");
            }).DisableAntiforgery();

            app.MapPost("/Account/UpgradeToHost", [Authorize] async (HttpContext http, MiotoDbContext db) =>
            {
                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId)) return Results.Redirect("/Account/Login");

                var user = await db.Users.FindAsync(userId);
                if (user == null) return Results.NotFound();

                if (user.Role == UserRole.Renter)
                {
                    user.Role = UserRole.Host;
                    await db.SaveChangesAsync();
                    await SignInUserAsync(http, user, true);
                    SetTempMessage(http, "SuccessMessage", "Chúc mừng bạn đã nâng cấp thành công thành Đối Tác Chủ Xe (Host)!");
                }
                return Results.Redirect("/Host");
            }).DisableAntiforgery();

            // =========================================================================
            // 3. BOOKING WORKFLOW ENDPOINTS
            // =========================================================================
            app.MapPost("/Bookings/Create", async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] CreateBookingViewModel model) =>
            {
                if (http.User.Identity?.IsAuthenticated != true)
                {
                    SetTempMessage(http, "ErrorMessage", "Vui lòng đăng nhập trước khi tiến hành đặt xe.");
                    return Results.Redirect($"/Account/Login?returnUrl=/Vehicles/Details/{model.VehicleId}");
                }

                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int renterId)) return Results.Redirect("/Account/Login");

                var user = await db.Users.FindAsync(renterId);
                if (user != null && user.DriverLicenseStatus != DriverLicenseStatus.Approved)
                {
                    SetTempMessage(http, "ErrorMessage", "Giấy phép lái xe của bạn chưa được phê duyệt. Vui lòng cập nhật hồ sơ để đặt xe.");
                    return Results.Redirect("/Account/Profile");
                }

                var result = await bookingService.CreateBookingAsync(renterId, model);
                if (!result.Success || result.Booking == null)
                {
                    SetTempMessage(http, "ErrorMessage", result.Message);
                    return Results.Redirect($"/Vehicles/Details/{model.VehicleId}");
                }

                SetTempMessage(http, "SuccessMessage", "Tạo đơn đặt xe thành công! Vui lòng thanh toán cọc giữ chỗ.");
                return Results.Redirect($"/Bookings/Details/{result.Booking.Id}");
            }).DisableAntiforgery();

            app.MapPost("/Bookings/ConfirmPayment", [Authorize] async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] int id) =>
            {
                var booking = await db.Bookings.FindAsync(id);
                if (booking == null) return Results.NotFound();

                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdStr, out int currentUserId);
                bool isAdmin = http.User.IsInRole("Admin");
                bool isRenter = booking.RenterId == currentUserId;

                if (!isAdmin && !isRenter) return Results.Redirect("/Account/AccessDenied");

                var success = await bookingService.ConfirmDepositAsync(id, $"PAY-{Guid.NewGuid().ToString()[..8].ToUpper()}");
                if (success)
                {
                    SetTempMessage(http, "SuccessMessage", "Thanh toán cọc 30% thành công! Chuyến xe của bạn đã được xác nhận.");
                }
                return Results.Redirect($"/Bookings/Details/{id}");
            }).DisableAntiforgery();

            // API khởi tạo thanh toán qua Cổng VNPay Sandbox
            app.MapPost("/Payment/CreateVnPayUrl", [Authorize] async (HttpContext http, MiotoDbContext db, IVnPayService vnPayService, [FromForm] int id) =>
            {
                var booking = await db.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
                if (booking == null) return Results.NotFound();

                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdStr, out int currentUserId);
                bool isAdmin = http.User.IsInRole("Admin");
                bool isRenter = booking.RenterId == currentUserId;

                if (!isAdmin && !isRenter) return Results.Redirect("/Account/AccessDenied");

                if (booking.Status == BookingStatus.Pending)
                {
                    booking.Status = BookingStatus.AwaitingPayment;
                    await db.SaveChangesAsync();
                }

                if (booking.Status != BookingStatus.AwaitingPayment)
                {
                    SetTempMessage(http, "ErrorMessage", "Đơn hàng này không ở trạng thái chờ thanh toán cọc.");
                    return Results.Redirect($"/Bookings/Details/{id}");
                }

                var paymentUrl = vnPayService.CreatePaymentUrl(booking, http);
                return Results.Redirect(paymentUrl);
            }).DisableAntiforgery();

            // API tiếp nhận kết quả thanh toán từ Cổng VNPay (Return URL)
            app.MapGet("/Payment/VnPayCallback", async (HttpContext http, MiotoDbContext db, IBookingService bookingService, IVnPayService vnPayService) =>
            {
                var result = vnPayService.ProcessCallback(http.Request.Query);

                if (result.BookingId <= 0 && !string.IsNullOrEmpty(result.BookingCode))
                {
                    var found = await db.Bookings.FirstOrDefaultAsync(b => b.BookingCode == result.BookingCode);
                    if (found != null) result.BookingId = found.Id;
                }

                if (result.BookingId <= 0)
                {
                    SetTempMessage(http, "ErrorMessage", "Không tìm thấy thông tin đơn đặt xe liên quan đến giao dịch VNPay.");
                    return Results.Redirect("/Vehicles");
                }

                if (result.IsSuccess)
                {
                    var transId = !string.IsNullOrEmpty(result.TransactionNo) ? result.TransactionNo : $"VNP-{result.BookingCode}";
                    await bookingService.ConfirmDepositAsync(result.BookingId, transId, "VNPAY", result.BankCode);
                    SetTempMessage(http, "SuccessMessage", $"Thanh toán cọc 30% qua Cổng VNPay thành công! Mã giao dịch: {result.TransactionNo} ({result.BankCode}). Đơn hàng đã được xác nhận giữ xe.");
                }
                else
                {
                    SetTempMessage(http, "ErrorMessage", $"Thanh toán cọc qua Cổng VNPay không thành công: {result.Message}");
                }

                return Results.Redirect($"/Bookings/Details/{result.BookingId}");
            });

            // API Webhook đối soát ngầm Server-to-Server từ VNPay (IPN)
            app.MapGet("/Payment/VnPayIPN", async (HttpContext http, MiotoDbContext db, IBookingService bookingService, IVnPayService vnPayService) =>
            {
                var result = vnPayService.ProcessCallback(http.Request.Query);

                if (!result.IsValidSignature)
                {
                    return Results.Json(new { RspCode = "97", Message = "Invalid Signature" });
                }

                var booking = await db.Bookings.FindAsync(result.BookingId);
                if (booking == null && !string.IsNullOrEmpty(result.BookingCode))
                {
                    booking = await db.Bookings.FirstOrDefaultAsync(b => b.BookingCode == result.BookingCode);
                }

                if (booking == null)
                {
                    return Results.Json(new { RspCode = "01", Message = "Order not found" });
                }

                if (booking.Status == BookingStatus.Confirmed)
                {
                    return Results.Json(new { RspCode = "02", Message = "Order already confirmed" });
                }

                if (result.IsSuccess)
                {
                    var transId = !string.IsNullOrEmpty(result.TransactionNo) ? result.TransactionNo : $"VNP-{result.BookingCode}";
                    await bookingService.ConfirmDepositAsync(booking.Id, transId, "VNPAY", result.BankCode);
                }

                return Results.Json(new { RspCode = "00", Message = "Confirm Success" });
            });

            // API kiểm tra trạng thái thanh toán đơn hàng thời gian thực (Real-time Polling)
            app.MapGet("/api/bookings/{id:int}/status", async (int id, MiotoDbContext db) =>
            {
                var booking = await db.Bookings.FindAsync(id);
                if (booking == null) return Results.NotFound();

                bool isPaid = booking.Status == BookingStatus.Confirmed || 
                              booking.Status == BookingStatus.InProgress || 
                              booking.Status == BookingStatus.Completed;

                return Results.Ok(new
                {
                    id = booking.Id,
                    bookingCode = booking.BookingCode,
                    status = booking.Status.ToString(),
                    isPaid = isPaid,
                    paymentMethod = booking.PaymentMethod,
                    transactionId = booking.PaymentTransactionId,
                    bankCode = booking.PaymentBankCode,
                    depositPaidAt = booking.DepositPaidAt?.ToString("yyyy-MM-ddTHH:mm:ss")
                });
            });

            // API Webhook tiếp nhận biến động số dư ngân hàng thực tế từ SePay / Casso (MBBank)
            app.MapPost("/api/sepay/webhook", async (HttpContext http, MiotoDbContext db, IBookingService bookingService, ISepayService sepayService, [FromBody] SepayWebhookPayload payload) =>
            {
                string? authHeader = http.Request.Headers["Authorization"].FirstOrDefault();

                var pendingBookings = await db.Bookings
                    .Where(b => b.Status == BookingStatus.AwaitingPayment || b.Status == BookingStatus.Pending)
                    .ToListAsync();

                var result = sepayService.ProcessWebhook(payload, authHeader, pendingBookings);

                if (!result.IsValidApiKey)
                {
                    return Results.Json(new { success = false, message = "Invalid API Key" }, statusCode: 401);
                }

                if (result.IsSuccess)
                {
                    await bookingService.ConfirmDepositAsync(result.BookingId, result.ReferenceCode, $"SEPAY_{result.Gateway.ToUpper()}", result.Gateway);
                    return Results.Ok(new { success = true, message = "Xác nhận cọc thành công", bookingId = result.BookingId, gateway = result.Gateway });
                }

                return Results.Ok(new { success = false, message = result.Message });
            });

            // API kiểm thử kích hoạt mô phỏng Webhook SePay (Dành cho Demo / Thuyết trình)
            app.MapPost("/api/sepay/test-webhook/{bookingId:int}", async (int bookingId, HttpContext http, MiotoDbContext db, IBookingService bookingService) =>
            {
                var booking = await db.Bookings.FindAsync(bookingId);
                if (booking == null) return Results.NotFound();

                if (booking.Status == BookingStatus.Pending)
                {
                    booking.Status = BookingStatus.AwaitingPayment;
                    await db.SaveChangesAsync();
                }

                string gateway = http.Request.Query["gateway"].ToString();
                if (string.IsNullOrEmpty(gateway)) gateway = "MBBank";

                var refCode = $"{gateway.ToUpper()}-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
                await bookingService.ConfirmDepositAsync(booking.Id, refCode, $"SEPAY_{gateway.ToUpper()}", gateway);
                return Results.Ok(new { success = true, message = $"Đã nhận diện thanh toán từ {gateway} thành công!", referenceCode = refCode, gateway = gateway });
            });

            // API mô phỏng Webhook thanh toán (dành cho Demo)
            app.MapPost("/Bookings/SimulatePayment", [Authorize] async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] int id) =>
            {
                var booking = await db.Bookings.FindAsync(id);
                if (booking == null) return Results.NotFound();

                // Giả lập xử lý thanh toán thành công
                await bookingService.ConfirmDepositAsync(id, $"VCB-SIM-{Guid.NewGuid().ToString()[..8].ToUpper()}");
                
                SetTempMessage(http, "SuccessMessage", "Mô phỏng thanh toán thành công! Đơn hàng đã được xác nhận.");
                return Results.Redirect($"/Bookings/Details/{id}");
            }).DisableAntiforgery();

            app.MapPost("/Bookings/Checkin", [Authorize] async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] int id, [FromForm] int odo, [FromForm] int fuelLevel) =>
            {
                var booking = await db.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
                if (booking == null) return Results.NotFound();

                await bookingService.CheckinVehicleAsync(id, odo, fuelLevel);
                SetTempMessage(http, "SuccessMessage", "Bàn giao xe thành công! Chúc bạn có một hành trình an toàn.");
                return Results.Redirect($"/Bookings/Details/{id}");
            }).DisableAntiforgery();

            app.MapPost("/Bookings/Checkout", [Authorize] async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] int id, [FromForm] int odo, [FromForm] int fuelLevel) =>
            {
                var booking = await db.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
                if (booking == null) return Results.NotFound();

                await bookingService.CheckoutVehicleAsync(id, odo, fuelLevel);
                SetTempMessage(http, "SuccessMessage", "Nghiệm thu trả xe thành công! Cảm ơn bạn đã sử dụng dịch vụ.");
                return Results.Redirect($"/Bookings/Details/{id}");
            }).DisableAntiforgery();

            app.MapPost("/Bookings/AddReview", [Authorize] async (HttpContext http, MiotoDbContext db, [FromForm] int bookingId, [FromForm] int rating, [FromForm] string? comment) =>
            {
                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int renterId)) return Results.Redirect("/Account/Login");

                var booking = await db.Bookings.Include(b => b.Reviews).FirstOrDefaultAsync(b => b.Id == bookingId && b.RenterId == renterId);
                if (booking != null && booking.Status == BookingStatus.Completed && !booking.Reviews.Any())
                {
                    db.Reviews.Add(new Review
                    {
                        BookingId = bookingId,
                        ReviewerId = renterId,
                        Rating = Math.Clamp(rating, 1, 5),
                        Comment = comment?.Trim(),
                        CreatedAt = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", "Cảm ơn bạn đã gửi đánh giá và nhận xét!");
                }
                return Results.Redirect($"/Bookings/Details/{bookingId}");
            }).DisableAntiforgery();

            // =========================================================================
            // 4. HOST MANAGEMENT ENDPOINTS
            // =========================================================================
            app.MapPost("/Host/BecomeHostConfirmed", [Authorize] async (HttpContext http, MiotoDbContext db) =>
            {
                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId)) return Results.Redirect("/Account/Login");

                var user = await db.Users.FindAsync(userId);
                if (user == null) return Results.Redirect("/Account/Login");

                if (user.Role == UserRole.Renter)
                {
                    user.Role = UserRole.Host;
                    await db.SaveChangesAsync();
                    await SignInUserAsync(http, user, true);
                    SetTempMessage(http, "SuccessMessage", "Chúc mừng bạn đã kích hoạt tài khoản Đối Tác Chủ Xe (Host) thành công!");
                }
                return Results.Redirect("/Host");
            }).DisableAntiforgery();

            app.MapPost("/Host/ToggleStatus", [Authorize(Roles = "Host,Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int id) =>
            {
                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdStr, out int hostId);
                var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && (v.HostId == hostId || http.User.IsInRole("Admin")));
                if (vehicle != null)
                {
                    vehicle.Status = vehicle.Status == VehicleStatus.Active ? VehicleStatus.Inactive : VehicleStatus.Active;
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã cập nhật trạng thái xe [{vehicle.Model}].");
                }
                return Results.Redirect("/Host");
            }).DisableAntiforgery();

            app.MapPost("/Host/ApproveBooking", [Authorize(Roles = "Host,Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int id) =>
            {
                var booking = await db.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
                if (booking == null) return Results.NotFound();

                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdStr, out int currentUserId);
                bool isAdmin = http.User.IsInRole("Admin");
                bool isHost = booking.Vehicle != null && booking.Vehicle.HostId == currentUserId;

                if (!isAdmin && !isHost)
                {
                    return Results.Redirect("/Account/AccessDenied");
                }

                if (booking.Status == BookingStatus.Pending)
                {
                    booking.Status = BookingStatus.AwaitingPayment;
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã chấp nhận đơn đặt xe #{booking.BookingCode}. Khách hàng có thể tiến hành đặt cọc.");
                }
                return Results.Redirect("/Host");
            }).DisableAntiforgery();

            app.MapPost("/Host/RejectBooking", [Authorize(Roles = "Host,Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int bookingId, [FromForm] string? reason) =>
            {
                var booking = await db.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking == null) return Results.NotFound();

                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdStr, out int currentUserId);
                bool isAdmin = http.User.IsInRole("Admin");
                bool isHost = booking.Vehicle != null && booking.Vehicle.HostId == currentUserId;

                if (!isAdmin && !isHost)
                {
                    return Results.Redirect("/Account/AccessDenied");
                }

                if (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.AwaitingPayment)
                {
                    booking.Status = BookingStatus.Cancelled;
                    booking.CancellationReason = string.IsNullOrWhiteSpace(reason) 
                        ? "Chủ xe bận / từ chối nhận chuyến thuê." 
                        : reason.Trim();

                    // Tự động giải phóng lịch khóa xe
                    var blockedDates = await db.CalendarBlockeds.Where(c => c.BookingId == bookingId).ToListAsync();
                    if (blockedDates.Any())
                    {
                        db.CalendarBlockeds.RemoveRange(blockedDates);
                    }

                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã từ chối đơn đặt xe #{booking.BookingCode} và giải phóng lịch xe thành công.");
                }
                return Results.Redirect("/Host");
            }).DisableAntiforgery();

            // =========================================================================
            // 5. ADMIN PORTAL ENDPOINTS
            // =========================================================================
            app.MapPost("/Admin/ApproveLicense", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int userId) =>
            {
                var user = await db.Users.FindAsync(userId);
                if (user != null)
                {
                    user.DriverLicenseStatus = DriverLicenseStatus.Approved;
                    user.RejectionReason = null;
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã phê duyệt Giấy phép lái xe A1 cho khách hàng {user.FullName}.");
                }
                return Results.Redirect("/Admin");
            }).DisableAntiforgery();

            app.MapPost("/Admin/RejectLicense", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int userId, [FromForm] string? reason) =>
            {
                var user = await db.Users.FindAsync(userId);
                if (user != null)
                {
                    user.DriverLicenseStatus = DriverLicenseStatus.Rejected;
                    user.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Hình ảnh GPLX bị mờ hoặc không trùng khớp." : reason.Trim();
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã từ chối hồ sơ GPLX của khách hàng {user.FullName}.");
                }
                return Results.Redirect("/Admin");
            }).DisableAntiforgery();

            app.MapPost("/Admin/DeleteVehicle", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int vehicleId) =>
            {
                var vehicle = await db.Vehicles.Include(v => v.Bookings).FirstOrDefaultAsync(v => v.Id == vehicleId);
                if (vehicle != null)
                {
                    if (vehicle.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress))
                    {
                        SetTempMessage(http, "ErrorMessage", $"Không thể xóa xe {vehicle.Model} vì đang có chuyến thuê diễn ra!");
                        return Results.Redirect("/Admin");
                    }

                    var blocks = await db.CalendarBlockeds.Where(c => c.VehicleId == vehicleId).ToListAsync();
                    if (blocks.Any()) db.CalendarBlockeds.RemoveRange(blocks);

                    db.Vehicles.Remove(vehicle);
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã xóa thành công phương tiện {vehicle.Model} khỏi hệ thống.");
                }
                return Results.Redirect("/Admin");
            }).DisableAntiforgery();

            app.MapPost("/Admin/UpdateVehiclePrice", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int vehicleId, [FromForm] decimal pricePerDay) =>
            {
                var vehicle = await db.Vehicles.FindAsync(vehicleId);
                if (vehicle != null && pricePerDay > 0)
                {
                    vehicle.PricePerDay = pricePerDay;
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã cập nhật giá thuê của xe {vehicle.Model} thành {pricePerDay:N0}đ/ngày.");
                }
                return Results.Redirect("/Admin");
            }).DisableAntiforgery();

            app.MapPost("/Admin/DeleteUser", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int userId) =>
            {
                var currentAdminIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(currentAdminIdStr, out int currentAdminId);

                if (userId == currentAdminId)
                {
                    SetTempMessage(http, "ErrorMessage", "Bạn không thể tự xóa tài khoản Quản trị viên đang đăng nhập!");
                    return Results.Redirect("/Admin");
                }

                var user = await db.Users.Include(u => u.OwnedVehicles).Include(u => u.Bookings).FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    if (user.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress))
                    {
                        SetTempMessage(http, "ErrorMessage", $"Không thể xóa người dùng {user.FullName} vì đang có đơn thuê xe hoạt động!");
                        return Results.Redirect("/Admin");
                    }

                    db.Users.Remove(user);
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã xóa tài khoản {user.FullName} ({user.Email}) thành công.");
                }
                return Results.Redirect("/Admin");
            }).DisableAntiforgery();

            app.MapPost("/Admin/ToggleVehicleStatus", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int vehicleId) =>
            {
                var vehicle = await db.Vehicles.FindAsync(vehicleId);
                if (vehicle != null)
                {
                    vehicle.Status = vehicle.Status == VehicleStatus.Active ? VehicleStatus.Inactive : VehicleStatus.Active;
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã chuyển trạng thái xe {vehicle.Model} ({vehicle.LicensePlate}).");
                }
                return Results.Redirect("/Admin");
            }).DisableAntiforgery();

            app.MapPost("/Admin/CancelBooking", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int bookingId, [FromForm] string? reason) =>
            {
                var booking = await db.Bookings.FindAsync(bookingId);
                if (booking != null)
                {
                    booking.Status = BookingStatus.Cancelled;
                    booking.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Admin hủy đơn theo yêu cầu kiểm duyệt." : reason.Trim();
                    var blockedDates = await db.CalendarBlockeds.Where(c => c.BookingId == bookingId).ToListAsync();
                    if (blockedDates.Any()) db.CalendarBlockeds.RemoveRange(blockedDates);
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã hủy chuyến thuê #{booking.BookingCode} và giải phóng lịch xe.");
                }
                return Results.Redirect("/Admin");
            }).DisableAntiforgery();

            app.MapPost("/Admin/UpdateUserRole", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int userId, [FromForm] UserRole newRole) =>
            {
                var user = await db.Users.FindAsync(userId);
                if (user != null)
                {
                    var currentAdminIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    int.TryParse(currentAdminIdStr, out int currentAdminId);
                    if (user.Id == currentAdminId && newRole != UserRole.Admin)
                    {
                        SetTempMessage(http, "ErrorMessage", "Bạn không thể tự hạ quyền Admin của chính tài khoản đang đăng nhập!");
                        return Results.Redirect("/Admin");
                    }
                    var oldRole = user.Role;
                    user.Role = newRole;
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Phân quyền thành công! [{user.FullName}] đã chuyển từ [{oldRole}] sang [{newRole}].");
                }
                return Results.Redirect("/Admin");
            }).DisableAntiforgery();
        }

        public static async Task SignInUserAsync(HttpContext http, User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("DriverLicenseStatus", user.DriverLicenseStatus.ToString()),
                new Claim("AvatarUrl", user.AvatarUrl ?? "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        }

        private static void SetTempMessage(HttpContext http, string key, string message)
        {
            var factory = http.RequestServices.GetService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>();
            if (factory != null)
            {
                var tempData = factory.GetTempData(http);
                tempData[key] = message;
                tempData.Save();
            }
        }
    }
}
