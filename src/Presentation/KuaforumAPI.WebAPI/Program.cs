using KuaforumAPI.Persistence.Contexts;
using KuaforumAPI.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using KuaforumAPI.Application;
using KuaforumAPI.WebAPI.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using KuaforumAPI.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

// Uygulama henüz başlamadan önce gelen hataları yakalamak için bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logConfig) =>
{
    logConfig
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "Logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}"
        );

    var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        // Warning ve üzeri loglar (Error, Fatal) SQL Server'daki Logs tablosuna da kaydedilir
        logConfig.WriteTo.MSSqlServer(
            connectionString: connectionString,
            sinkOptions: new MSSqlServerSinkOptions
            {
                TableName = "Logs",
                AutoCreateSqlTable = true  // Tablo yoksa otomatik oluşturur
            },
            restrictedToMinimumLevel: LogEventLevel.Warning
        );
    }
});


builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    );
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("nominatim", c =>
{
    c.DefaultRequestHeaders.Add("User-Agent", "KuaforumApp/1.0 (contact@kuaforum.com)");
    c.DefaultRequestHeaders.Add("Accept-Language", "tr");
});

builder.Services.AddHttpClient("netgsm", c =>
{
    c.BaseAddress = new Uri("https://api.netgsm.com.tr");
    c.Timeout = TimeSpan.FromSeconds(15);
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Rate Limiting — tüm policy'ler IP başına ayrı sayaç tutar (per-IP partition)
builder.Services.AddRateLimiter(options =>
{
    static string GetIp(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Auth & şifre işlemleri: her IP'ye 1 dakikada 10 istek
    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: GetIp(ctx),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    // Randevu oluşturma: her IP'ye 1 dakikada 20 istek
    options.AddPolicy("appointments", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: GetIp(ctx),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    // Yorum oluşturma/güncelleme: her IP'ye 1 dakikada 10 istek
    options.AddPolicy("reviews", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: GetIp(ctx),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    // Dosya yükleme (Cloudinary): her IP'ye 1 dakikada 5 istek
    options.AddPolicy("upload", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: GetIp(ctx),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    // Misafir randevu oluşturma: her IP'ye 1 dakikada 3, saatte 10 istek
    options.AddPolicy("guest-appointments", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey: GetIp(ctx),
        factory: _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 3,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    // Salon başvurusu: her IP'ye 1 saatte 3 istek
    options.AddPolicy("application", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: GetIp(ctx),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromHours(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    // Geocoding (Nominatim harici API): her IP'ye 1 dakikada 30 istek
    options.AddPolicy("geocoding", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: GetIp(ctx),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Persistence
builder.Services.AddPersistenceServices(builder.Configuration);

// Identity & JWT
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();



var trCulture = new System.Globalization.CultureInfo("tr-TR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = trCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = trCulture;

// Cloudinary
builder.Services.Configure<KuaforumAPI.Application.Settings.CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<KuaforumAPI.Application.Interfaces.Services.IImageService, KuaforumAPI.Infrastructure.Services.CloudinaryImageService>();

var app = builder.Build();

// Seed Roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await KuaforumAPI.Persistence.Seeds.RoleSeeder.SeedRolesAsync(services);
    await KuaforumAPI.Persistence.Seeds.AdminUserSeeder.SeedAdminAsync(services);
}

// Proxy (Nginx, cloud load balancer) arkasında gerçek IP ve protokolü doğru oku
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production: tarayıcıya 1 yıl boyunca bu siteyi sadece HTTPS ile aç talimatı
    app.UseHsts();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
