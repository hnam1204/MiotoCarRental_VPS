using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.ViewModels;

namespace MiotoCarRental.Controllers
{
    public class AccountController : Controller
    {
        private readonly MiotoDbContext _context;

        public AccountController(MiotoDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower().Trim());

            // Trong môi trường đồ án, hỗ trợ so khớp mật khẩu đơn giản hoặc hash
            if (user == null || (user.PasswordHash != model.Password && user.PasswordHash != $"{model.Password}_hash_123" && user.PasswordHash != $"{model.Password.ToLower()}123"))
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);

            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName}.";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            // Điều hướng thông minh theo vai trò
            if (user.Role == UserRole.Admin)
            {
                return RedirectToAction("Index", "Admin");
            }
            if (user.Role == UserRole.Host)
            {
                return RedirectToAction("Index", "Host");
            }

            return RedirectToAction("Index", "Home");
        }

        // POST / GET: /Account/QuickLogin (Dành riêng cho giảng viên & demo bảo vệ đồ án)
        [HttpPost]
        public async Task<IActionResult> QuickLogin(string email, string? returnUrl = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                await SignInUserAsync(user, true);
                TempData["SuccessMessage"] = $"Đã đăng nhập nhanh với vai trò [{user.Role}]: {user.FullName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Điều hướng thông minh theo vai trò
                if (user.Role == UserRole.Admin)
                {
                    return RedirectToAction("Index", "Admin");
                }
                if (user.Role == UserRole.Host)
                {
                    return RedirectToAction("Index", "Host");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra trùng Email
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower().Trim()))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng trên hệ thống.");
                return View(model);
            }

            // Kiểm tra trùng SĐT
            if (await _context.Users.AnyAsync(u => u.Phone == model.Phone.Trim()))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được đăng ký.");
                return View(model);
            }

            var newUser = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.ToLower().Trim(),
                Phone = model.Phone.Trim(),
                PasswordHash = model.Password,
                Role = model.Role,
                AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100",
                DriverLicenseStatus = DriverLicenseStatus.Approved, // Mặc định duyệt cho khách mới để tiện trải nghiệm đồ án
                DriverLicenseNo = "GPLX-" + Random.Shared.Next(10000000, 99999999).ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            await SignInUserAsync(newUser, true);

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn đã được tự động đăng nhập.";
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống an toàn.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var vm = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                DriverLicenseNo = user.DriverLicenseNo,
                DriverLicenseImage = user.DriverLicenseImage,
                DriverLicenseStatus = user.DriverLicenseStatus,
                RejectionReason = user.RejectionReason
            };

            return View(vm);
        }

        // POST: /Account/UpdateLicense
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLicense(string licenseNo, string? licenseImageUrl)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.DriverLicenseNo = licenseNo.Trim();
                    user.DriverLicenseImage = licenseImageUrl ?? "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?w=600";
                    user.DriverLicenseStatus = DriverLicenseStatus.Approved;
                    await _context.SaveChangesAsync();

                    // Cập nhật lại Cookie Claims
                    await SignInUserAsync(user, true);

                    TempData["SuccessMessage"] = "Cập nhật Giấy phép lái xe thành công! Hồ sơ đã được phê duyệt.";
                }
            }

            return RedirectToAction(nameof(Profile));
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/UpgradeToHost
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpgradeToHost()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Role == UserRole.Renter)
            {
                user.Role = UserRole.Host;
                await _context.SaveChangesAsync();
                await SignInUserAsync(user, true);
                TempData["SuccessMessage"] = "Chúc mừng bạn đã nâng cấp thành công thành Đối Tác Chủ Xe (Host)! Bạn có thể bắt đầu đăng xe và nhận đơn thuê.";
            }

            return RedirectToAction("Index", "Host");
        }

        // Helper: Ký Cookie đăng nhập
        private async Task SignInUserAsync(User user, bool isPersistent)
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

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        }
    }
}
