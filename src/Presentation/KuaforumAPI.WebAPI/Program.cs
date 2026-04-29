using KuaforumAPI.Persistence.Contexts;
using KuaforumAPI.Persistence;
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

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("nominatim", c =>
{
    c.DefaultRequestHeaders.Add("User-Agent", "KuaforumApp/1.0 (contact@kuaforum.com)");
    c.DefaultRequestHeaders.Add("Accept-Language", "tr");
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

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
