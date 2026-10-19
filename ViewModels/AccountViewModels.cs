using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiotoCarRental.Data;
using MiotoCarRental.Endpoints;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.ViewModels
{
    [IgnoreAntiforgeryToken]
    public class LoginViewModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public LoginViewModel() { }

        [ActivatorUtilitiesConstructor]
        public LoginViewModel(MiotoDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; } = true;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public IActionResult OnGet(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return Redirect("/");
            }
            ReturnUrl = returnUrl;
            ViewData["ReturnUrl"] = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || _context == null)
            {
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == Email.ToLower().Trim());
            if (user == null || (user.PasswordHash != Password && user.PasswordHash != $"{Password}_hash_123" && user.PasswordHash != $"{Password.ToLower()}123"))
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return Page();
            }

            await EcoBikeEndpoints.SignInUserAsync(HttpContext, user, RememberMe);
            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName}.";

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            if (user.Role == UserRole.Admin) return Redirect("/Admin");
            if (user.Role == UserRole.Host) return Redirect("/Host");
            return Redirect("/");
        }
    }

    [IgnoreAntiforgeryToken]
    public class RegisterViewModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public RegisterViewModel() { }

        [ActivatorUtilitiesConstructor]
        public RegisterViewModel(MiotoDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Họ và tên")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và có 10 chữ số")]
        public string Phone { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public UserRole Role { get; set; } = UserRole.Renter;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng xác nhận Mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không trùng khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return Redirect("/");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || _context == null)
            {
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == Email.ToLower().Trim()))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng trên hệ thống.");
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.Phone == Phone.Trim()))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được đăng ký.");
                return Page();
            }

            var newUser = new User
            {
                FullName = FullName.Trim(),
                Email = Email.ToLower().Trim(),
                Phone = Phone.Trim(),
                PasswordHash = Password,
                Role = Role,
                AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100",
                DriverLicenseStatus = DriverLicenseStatus.Approved,
                DriverLicenseNo = "GPLX-" + Random.Shared.Next(10000000, 99999999).ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            await EcoBikeEndpoints.SignInUserAsync(HttpContext, newUser, true);

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn đã được tự động đăng nhập.";
            return Redirect("/");
        }
    }

    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ProfileViewModel : PageModel
    {
        private readonly MiotoDbContext? _context;

        public ProfileViewModel() { }

        [ActivatorUtilitiesConstructor]
        public ProfileViewModel(MiotoDbContext context)
        {
            _context = context;
        }

        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? DriverLicenseNo { get; set; }
        public string? DriverLicenseImage { get; set; }
        public DriverLicenseStatus DriverLicenseStatus { get; set; }
        public string? RejectionReason { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (_context == null) return Redirect("/Account/Login");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Redirect("/Account/Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            Id = user.Id;
            FullName = user.FullName;
            Email = user.Email;
            Phone = user.Phone;
            Role = user.Role;
            DriverLicenseNo = user.DriverLicenseNo;
            DriverLicenseImage = user.DriverLicenseImage;
            DriverLicenseStatus = user.DriverLicenseStatus;
            RejectionReason = user.RejectionReason;

            return Page();
        }
    }
}
