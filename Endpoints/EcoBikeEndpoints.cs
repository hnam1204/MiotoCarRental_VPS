using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.Models;
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

                var list = await query.Select(v => new PublicVehicleDto(
                    v.Id,
                    v.Brand,
                    v.Model,
                    v.YearManufacture,
                    v.Category.ToString(),
                    (int)v.Category,
                    v.BatteryRangeKm,
                    v.MaxSpeedKmH,
                    v.BatteryType,
                    v.RequiresLicenseA1,
                    v.PricePerDay,
                    v.City,
                    v.District,
                    v.InstantBooking,
                    v.ThumbnailUrl,
                    v.LimitKmPerDay)).ToListAsync();

                return Results.Ok(list.OrderBy(v => v.PricePerDay));
            });

            api.MapGet("/vehicles/{id:int}", async (int id, MiotoDbContext db) =>
            {
                var v = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.Status == VehicleStatus.Active);
                if (v is null) return Results.NotFound(new { message = "Không tìm thấy phương tiện" });
                return Results.Ok(new PublicVehicleDetailsDto(
                    v.Id,
                    v.Brand,
                    v.Model,
                    v.YearManufacture,
                    v.Category.ToString(),
                    v.BatteryRangeKm,
                    v.MaxSpeedKmH,
                    v.BatteryType,
                    v.RequiresLicenseA1,
                    v.PricePerDay,
                    v.City,
                    v.District,
                    v.InstantBooking,
                    v.ThumbnailUrl,
                    v.Description,
                    "Đối tác EcoBike"));
            });

            api.MapGet("/stats", async (MiotoDbContext db) =>
            {
                var vehicles = await db.Vehicles
                    .Where(v => v.Status == VehicleStatus.Active)
                    .ToListAsync();
                return Results.Ok(new PublicStatsDto(
                    "ReactJS Frontend (ClientApp) + ASP.NET Core 9.0 Web API + 3-Layer Services",
                    vehicles.Count,
                    await db.Users.CountAsync(),
                    await db.Bookings.CountAsync(b => b.Status != BookingStatus.Cancelled),
                    new PublicCategoryStatsDto(
                        vehicles.Count(v => v.Category == VehicleCategory.Economy),
                        vehicles.Count(v => v.Category == VehicleCategory.MidRange),
                        vehicles.Count(v => v.Category == VehicleCategory.Touring),
                        vehicles.Count(v => v.Category == VehicleCategory.SportPremium))));
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

            api.MapGet("/bookings", [Authorize(Roles = "Admin,Host")] async (HttpContext http, MiotoDbContext db) =>
            {
                var query = db.Bookings.AsQueryable();
                if (!http.User.IsInRole("Admin"))
                {
                    var hostId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!int.TryParse(hostId, out var currentHostId))
                    {
                        return Results.Forbid();
                    }

                    query = query.Where(b => b.Vehicle != null && b.Vehicle.HostId == currentHostId);
                }

                var list = await query
                    .Include(b => b.Vehicle)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(20)
                    .Select(b => new BookingSummaryDto(
                        b.Vehicle != null ? b.Vehicle.Brand + " " + b.Vehicle.Model : "Xe máy điện",
                        b.StartTime,
                        b.EndTime,
                        b.TotalDays,
                        b.Status.ToString()))
                    .ToListAsync();
                return Results.Ok(list);
            });

            api.MapPost("/bookings/calculate", async (MiotoDbContext db, int vehicleId, int days, string? couponCode) =>
            {
                var vehicle = await db.Vehicles.FindAsync(vehicleId);
                if (vehicle == null || vehicle.Status != VehicleStatus.Active)
                {
                    return Results.NotFound(new { message = "Không tìm thấy xe" });
                }

                if (days is < 1 or > 30)
                {
                    return Results.BadRequest(new { message = "Số ngày thuê phải từ 1 đến 30." });
                }

                int validDays = days;
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
            });

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
            app.MapPost("/Account/QuickLogin", async (HttpContext http, MiotoDbContext db, IPasswordService passwordService, [FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl) =>
            {
                if (!app.Environment.IsDevelopment() || !app.Configuration.GetValue<bool>("Demo:EnableQuickLogin"))
                {
                    return Results.NotFound();
                }

                var allowedDemoEmails = new[] { "admin@mioto.vn", "chuxe@gmail.com", "khachthue@gmail.com" };
                if (!allowedDemoEmails.Contains(email.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { message = "Tài khoản demo không hợp lệ." });
                }

                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower());
                if (user != null && passwordService.VerifyAndUpgrade(user, password) != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                {
                    await db.SaveChangesAsync();
                    await SignInUserAsync(http, user, true);
                    SetTempMessage(http, "SuccessMessage", $"Đã đăng nhập nhanh với vai trò [{user.Role}]: {user.FullName}!");

                    if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
                    {
                        return Results.Redirect(returnUrl);
                    }

                    if (user.Role == UserRole.Admin) return Results.Redirect("/Admin");
                    if (user.Role == UserRole.Host) return Results.Redirect("/Host");
                }
                return Results.Redirect("/");
            });

            app.MapPost("/Account/Logout", async (HttpContext http) =>
            {
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                SetTempMessage(http, "SuccessMessage", "Bạn đã đăng xuất khỏi hệ thống an toàn.");
                return Results.Redirect("/");
            });

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
            });

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
            });

            // =========================================================================
            // 3. BOOKING WORKFLOW ENDPOINTS
            // =========================================================================
            app.MapPost("/Bookings/Create", [Authorize] async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] CreateBookingViewModel model) =>
            {
                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int renterId)) return Results.Forbid();

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
            });

            app.MapPost("/Bookings/ConfirmPayment", [Authorize] async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] int id) =>
            {
                var booking = await db.Bookings.FindAsync(id);
                if (booking == null) return Results.NotFound();

                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdStr, out int currentUserId);
                bool isAdmin = http.User.IsInRole("Admin");
                bool isRenter = booking.RenterId == currentUserId;

                if (!isAdmin && !isRenter) return Results.Redirect("/Account/AccessDenied");

                var success = await bookingService.ConfirmDepositAsync(id, $"SIM-PAY-{Guid.NewGuid().ToString()[..8].ToUpper()}");
                if (success)
                {
                    SetTempMessage(http, "SuccessMessage", "Thanh toán mô phỏng cọc 30% thành công! Chuyến xe của bạn đã được xác nhận.");
                }
                return Results.Redirect($"/Bookings/Details/{id}");
            });

            app.MapPost("/Bookings/Checkin", [Authorize] async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] int id, [FromForm] int odo, [FromForm] int fuelLevel) =>
            {
                var booking = await db.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
                if (booking == null) return Results.NotFound();

                var currentUserId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(currentUserId, out var userId)
                    || (!http.User.IsInRole("Admin") && booking.RenterId != userId && booking.Vehicle?.HostId != userId))
                {
                    return Results.Forbid();
                }

                if (!await bookingService.CheckinVehicleAsync(id, odo, fuelLevel))
                {
                    SetTempMessage(http, "ErrorMessage", "Không thể bàn giao xe ở trạng thái hiện tại hoặc thông số không hợp lệ.");
                    return Results.Redirect($"/Bookings/Details/{id}");
                }
                SetTempMessage(http, "SuccessMessage", "Bàn giao xe thành công! Chúc bạn có một hành trình an toàn.");
                return Results.Redirect($"/Bookings/Details/{id}");
            });

            app.MapPost("/Bookings/Checkout", [Authorize] async (HttpContext http, MiotoDbContext db, IBookingService bookingService, [FromForm] int id, [FromForm] int odo, [FromForm] int fuelLevel) =>
            {
                var booking = await db.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
                if (booking == null) return Results.NotFound();

                var currentUserId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(currentUserId, out var userId)
                    || (!http.User.IsInRole("Admin") && booking.RenterId != userId && booking.Vehicle?.HostId != userId))
                {
                    return Results.Forbid();
                }

                if (!await bookingService.CheckoutVehicleAsync(id, odo, fuelLevel))
                {
                    SetTempMessage(http, "ErrorMessage", "Không thể nghiệm thu trả xe ở trạng thái hiện tại hoặc thông số không hợp lệ.");
                    return Results.Redirect($"/Bookings/Details/{id}");
                }
                SetTempMessage(http, "SuccessMessage", "Nghiệm thu trả xe thành công! Cảm ơn bạn đã sử dụng dịch vụ.");
                return Results.Redirect($"/Bookings/Details/{id}");
            });

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
            });

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
            });

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
            });

            app.MapPost("/Host/ApproveBooking", [Authorize(Roles = "Host,Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int id) =>
            {
                var booking = await db.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
                var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdStr, out var hostId);
                var canManage = http.User.IsInRole("Admin") || booking?.Vehicle?.HostId == hostId;
                if (canManage && booking != null && booking.Status == BookingStatus.Pending)
                {
                    booking.Status = BookingStatus.AwaitingPayment;
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã chấp nhận đơn đặt xe #{booking.BookingCode}.");
                }
                return Results.Redirect("/Host");
            });

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
            });

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
            });

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
            });

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
            });

            app.MapPost("/Admin/UpdateUserRole", [Authorize(Roles = "Admin")] async (HttpContext http, MiotoDbContext db, [FromForm] int userId, [FromForm] UserRole newRole) =>
            {
                if (!Enum.IsDefined(newRole))
                {
                    SetTempMessage(http, "ErrorMessage", "Vai trò không hợp lệ.");
                    return Results.Redirect("/Admin");
                }

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
            });
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
