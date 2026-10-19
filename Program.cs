using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Endpoints;
using MiotoCarRental.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình kiến trúc Razor Pages (MVVM PageModel) thay cho MVC truyền thống
builder.Services.AddRazorPages();

// Cấu hình Cookie Authentication & Role-Based Authorization (RBAC)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Tầng Data Access Layer: Entity Framework Core với SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=mioto.db";
builder.Services.AddDbContext<MiotoDbContext>(options =>
    options.UseSqlite(connectionString));

// Tầng Business Logic Layer (Services)
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

// Tự động khởi tạo Database và nạp 16 xe điện mẫu khi chạy ứng dụng
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MiotoDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi nạp dữ liệu mẫu ban đầu.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Thứ tự Middleware: Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

// Ánh xạ Frontend Razor Pages (MVVM) & Backend Minimal Web APIs (Endpoints)
app.MapRazorPages();
app.MapEcoBikeEndpoints();

app.Run();
