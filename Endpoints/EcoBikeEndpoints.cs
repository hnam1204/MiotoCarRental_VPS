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
            app.MapGet("/Vehicles/Index", (HttpContext ctx) => Results.Redirect("/Vehicles" + ctx.Request.QueryString));
            app.MapGet("/Host/Index", () => Results.Redirect("/Host"));
            app.MapGet("/Admin/Index", () => Results.Redirect("/Admin"));

            // =========================================================================
            // 1. RESTful JSON APIs (Backend API Layer)
            // =========================================================================
            var api = app.MapGroup("/api");

            api.MapGet("/vehicles", async (MiotoDbContext db, VehicleCategory? category, string? city) =>
            {
                var query = db.Vehicles.Where(v => v.Status == VehicleStatus.Active).AsQueryable();
                if (category.HasValue) query = query.Where(v => v.Category == category.Value);
                if (!string.IsNullOrWhiteSpace(city)) query = query.Where(v => v.City.Contains(city));
                var list = await query.ToListAsync();
                return Results.Ok(list.OrderBy(v => v.PricePerDay));
            });

            api.MapGet("/vehicles/{id:int}", async (int id, MiotoDbContext db) =>
            {
                var vehicle = await db.Vehicles.Include(v => v.Host).FirstOrDefaultAsync(v => v.Id == id);
                return vehicle is not null ? Results.Ok(vehicle) : Results.NotFound();
            });

            api.MapGet("/stats", async (MiotoDbContext db) =>
            {
                return Results.Ok(new
                {
                    Architecture = "3-Layer Architecture + Razor Pages (MVVM) + Minimal Web API",
                    TotalVehicles = await db.Vehicles.CountAsync(),
                    TotalUsers = await db.Users.CountAsync(),
                    TotalBookings = await db.Bookings.CountAsync()
                });
            });

            // =========================================================================
            // 2. ACCOUNT & AUTHENTICATION ENDPOINTS
            // =========================================================================
            app.MapPost("/Account/QuickLogin", async (HttpContext http, MiotoDbContext db, [FromForm] string email, [FromForm] string? returnUrl) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
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
                if (booking != null && booking.Status == BookingStatus.Pending)
                {
                    booking.Status = BookingStatus.AwaitingPayment;
                    await db.SaveChangesAsync();
                    SetTempMessage(http, "SuccessMessage", $"Đã chấp nhận đơn đặt xe #{booking.BookingCode}.");
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
