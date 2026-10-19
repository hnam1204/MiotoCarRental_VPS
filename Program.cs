using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Endpoints;
using MiotoCarRental.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình kiến trúc Razor Pages (MVVM PageModel) thay cho MVC truyền thống
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EcoBike API",
        Version = "v1",
        Description = "Read-only vehicle and demo booking calculation APIs. Simulated payment flows are documented in the coursework UI."
    });
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "ecobike.csrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    var configuredProxies = builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies")
        .Get<string[]>() ?? ["127.0.0.1"];

    foreach (var configuredProxy in configuredProxies)
    {
        if (System.Net.IPAddress.TryParse(configuredProxy, out var proxyAddress))
        {
            options.KnownProxies.Add(proxyAddress);
        }
    }
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
        options.Cookie.Name = "ecobike.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization();

// Tầng Data Access Layer: Entity Framework Core với SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=mioto.db";
builder.Services.AddDbContext<MiotoDbContext>(options =>
    options.UseSqlite(connectionString));

// Tầng Business Logic Layer (Services)
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var services = scope.ServiceProvider;
        DbInitializer.Initialize(
            services.GetRequiredService<MiotoDbContext>(),
            seedDemoData: true,
            services.GetRequiredService<IPasswordService>());
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi nạp dữ liệu mẫu ban đầu.");
    }
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.Use(async (context, next) =>
{
    var isStateChanging = HttpMethods.IsPost(context.Request.Method)
        || HttpMethods.IsPut(context.Request.Method)
        || HttpMethods.IsPatch(context.Request.Method)
        || HttpMethods.IsDelete(context.Request.Method);
    var isStatelessQuote = context.Request.Path.Equals("/api/bookings/calculate", StringComparison.OrdinalIgnoreCase);

    if (isStateChanging && !isStatelessQuote)
    {
        try
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "Yêu cầu không hợp lệ." });
            return;
        }
    }

    await next();
});

// Thứ tự Middleware: Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (MiotoDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "ok" })
        : Results.Json(new { status = "degraded" }, statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous().WithTags("Operations");

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoBike API v1");
    options.RoutePrefix = "swagger";
});

// Ánh xạ Frontend Razor Pages (MVVM) & Backend Minimal Web APIs (Endpoints)
app.MapRazorPages();
app.MapEcoBikeEndpoints();

app.Run();

public partial class Program;
