using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Endpoints;
using MiotoCarRental.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình kiến trúc Razor Pages (MVVM PageModel) thay cho MVC truyền thống
builder.Services.AddRazorPages();

// Cấu hình Health Checks phục vụ kiểm tra trạng thái ứng dụng khi triển khai VPS
builder.Services.AddHealthChecks();

// Cấu hình Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "EcoBike Platform Web APIs",
        Version = "v1",
        Description = "Tài liệu API hệ thống cho thuê xe máy điện EcoBike (ASP.NET Core 9 Minimal APIs)"
    });
});

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

// Đảm bảo thư mục lưu trữ cơ sở dữ liệu SQLite tồn tại (cho môi trường VPS / Linux)
try
{
    var builderConn = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
    if (!string.IsNullOrEmpty(builderConn.DataSource) && builderConn.DataSource != ":memory:" && !builderConn.DataSource.StartsWith(":"))
    {
        var dir = Path.GetDirectoryName(builderConn.DataSource);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
catch { }

builder.Services.AddDbContext<MiotoDbContext>(options =>
    options.UseSqlite(connectionString));

// Tầng Business Logic Layer (Services)
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<ISepayService, SepayService>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();

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

// Cấu hình Forwarded Headers cho môi trường Nginx Reverse Proxy trên Linux VPS
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// Health check endpoint phục vụ kiểm tra tự động khi deploy (Zero-downtime health-check)
app.MapHealthChecks("/health");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "EcoBike", timestamp = DateTime.UtcNow })).AllowAnonymous();
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "EcoBike", timestamp = DateTime.UtcNow })).AllowAnonymous();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Chỉ chuyển hướng HTTPS nếu được bật trong cấu hình (tránh xung đột với Nginx SSL Termination và Kestrel HTTP 5081)
var httpsRedirectionEnabled = builder.Configuration.GetValue<bool>("HttpsRedirection:Enabled", true);
if (httpsRedirectionEnabled && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// Kích hoạt Swagger UI cho tài liệu API
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoBike API v1");
    c.RoutePrefix = "swagger";
});

// Thứ tự Middleware: Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

// Ánh xạ Frontend Razor Pages (MVVM) & Backend Minimal Web APIs (Endpoints)
app.MapRazorPages();
app.MapEcoBikeEndpoints();

app.Run();
