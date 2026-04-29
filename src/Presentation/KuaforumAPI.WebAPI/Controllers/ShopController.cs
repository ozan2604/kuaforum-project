using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using KuaforumAPI.Application.Exceptions;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly IShopService _shopService;

        public ShopController(IShopService shopService)
        {
            _shopService = shopService;
        }

        [HttpPost]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> CreateShop([FromBody] CreateShopDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _shopService.CreateShopAsync(userId, request);
            return Ok(new { Message = "Shop created successfully." });
        }

        [HttpGet("my-shop")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> GetMyShop()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shop = await _shopService.GetShopByOwnerIdAsync(userId);
            if (shop == null) return NotFound(new { Message = "You don't have a shop yet." });
            return Ok(shop);
        }

        [HttpPut]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> UpdateShop([FromBody] CreateShopDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _shopService.UpdateShopAsync(userId, request);
            return Ok(new { Message = "Shop updated successfully." });
        }

        [HttpGet("my-shop/dashboard-stats")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> GetDashboardStats(
            [FromServices] KuaforumAPI.Persistence.Contexts.ApplicationDbContext context,
            [FromServices] KuaforumAPI.Application.Interfaces.Services.IDateTimeService dateTimeService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shop = await _shopService.GetShopByOwnerIdAsync(userId);
            if (shop == null) return NotFound(new { Message = "You don't have a shop yet." });

            var now = dateTimeService.Now;
            var today = now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-7);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var appointments = await context.Appointments
                .Where(a => a.ShopId == shop.Id)
                .Select(a => new { a.StartTime, a.Status, Price = a.ShopService.Price })
                .ToListAsync();

            var todayApps = appointments.Where(a => a.StartTime.Date == today).ToList();
            var weekApps = appointments.Where(a => a.StartTime.Date >= startOfWeek).ToList();
            var monthApps = appointments.Where(a => a.StartTime.Date >= startOfMonth).ToList();
            var yearApps = appointments.Where(a => a.StartTime.Date >= startOfYear).ToList();

            var services = await context.ShopServices.Where(s => s.ShopId == shop.Id && !s.IsDeleted).ToListAsync();
            var employees = await context.ShopEmployees.Where(e => e.ShopId == shop.Id && !e.IsDeleted).ToListAsync();

            var unconfirmedApps = appointments.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Pending);
            var missingInfo = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(shop.Description)) missingInfo.Add("Açıklama");
            if (string.IsNullOrWhiteSpace(shop.CoverImagePath)) missingInfo.Add("Kapak Fotoğrafı");
            if (shop.Categories == null || !shop.Categories.Any()) missingInfo.Add("Kategori");

            var notifications = new System.Collections.Generic.List<string>();
            if (unconfirmedApps > 0) notifications.Add($"{unconfirmedApps} adet onay/yanıt bekleyen randevunuz var.");
            if (missingInfo.Any()) notifications.Add($"Dükkan profilinizde eksikler var: {string.Join(", ", missingInfo)}.");

            return Ok(new
            {
                ShopId = shop.Id,
                Notifications = notifications,
                Appointments = new
                {
                    Today = new
                    {
                        Total = todayApps.Count(),
                        Completed = todayApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed),
                        Cancelled = todayApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Cancelled),
                        Rejected = todayApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Rejected),
                        Revenue = todayApps.Where(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed).Sum(a => (decimal)a.Price)
                    },
                    ThisWeek = new
                    {
                        Total = weekApps.Count(),
                        Completed = weekApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed),
                        Cancelled = weekApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Cancelled),
                        Rejected = weekApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Rejected),
                        Revenue = weekApps.Where(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed).Sum(a => (decimal)a.Price)
                    },
                    ThisMonth = new
                    {
                        Total = monthApps.Count(),
                        Completed = monthApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed),
                        Cancelled = monthApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Cancelled),
                        Rejected = monthApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Rejected),
                        Revenue = monthApps.Where(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed).Sum(a => (decimal)a.Price)
                    },
                    ThisYear = new
                    {
                        Total = yearApps.Count(),
                        Completed = yearApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed),
                        Cancelled = yearApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Cancelled),
                        Rejected = yearApps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Rejected),
                        Revenue = yearApps.Where(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed).Sum(a => (decimal)a.Price)
                    }
                },
                Services = new
                {
                    Total = services.Count,
                    Active = services.Count(s => s.IsActive),
                    Passive = services.Count(s => !s.IsActive)
                },
                Employees = new
                {
                    Total = employees.Count,
                    Active = employees.Count(e => e.IsActive),
                    Passive = employees.Count(e => !e.IsActive)
                }
            });
        }
        [HttpGet("admin/all")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.Admin)]
        public async Task<IActionResult> GetAllShops(
            [FromServices] KuaforumAPI.Persistence.Contexts.ApplicationDbContext context, 
            [FromQuery] string search = "", 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            var query = context.Shops.Include(s => s.Owner).Include(s => s.Categories).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => 
                    s.Name.ToLower().Contains(lowerSearch) || 
                    (s.Owner != null && (s.Owner.FirstName.ToLower().Contains(lowerSearch) || s.Owner.LastName.ToLower().Contains(lowerSearch) || s.Owner.Email.ToLower().Contains(lowerSearch))) ||
                    (s.City != null && s.City.ToLower().Contains(lowerSearch)) ||
                    (s.PhoneNumber != null && s.PhoneNumber.Contains(search))
                );
            }

            var totalCount = await EntityFrameworkQueryableExtensions.CountAsync(query);

            var pagedShops = await EntityFrameworkQueryableExtensions.ToListAsync(
                query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
            );

            var shops = pagedShops.Select(shop => new KuaforumAPI.Application.DTOs.Shop.ShopDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                Address = shop.Address,
                City = shop.City,
                District = shop.District,
                PhoneNumber = shop.PhoneNumber,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                Categories = shop.Categories.Select(c => c.CategoryValue).ToList(),
                GenderPreference = shop.GenderPreference,
                IsActive = shop.IsActive,
                IsAutoProcessEnabled = shop.IsAutoProcessEnabled,
                BookingDaysAhead = shop.BookingDaysAhead,
                CoverImagePath = shop.CoverImagePath,
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
                OwnerName = shop.Owner != null ? $"{shop.Owner.FirstName} {shop.Owner.LastName}" : "Unknown",
                OwnerEmail = shop.Owner != null ? shop.Owner.Email : null,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            }).ToList();

            return Ok(new { TotalCount = totalCount, Shops = shops });
        }

        [HttpDelete("admin/{id}")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.Admin)]
        public async Task<IActionResult> DeleteShopByAdmin(Guid id)
        {
            await _shopService.DeleteShopAsync(id);
            return Ok(new { Message = "Shop deleted successfully." });
        }

        [HttpGet("public/all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicShops([FromQuery] string? city = null, [FromQuery] string? district = null, [FromQuery] string? neighborhood = null)
        {
            var shops = await _shopService.GetAllShopsAsync(city, district, neighborhood);
            // Filter only active shops for public view if needed, but for now return all
            // Ideally should filter in service, but this is fine for MVP
            return Ok(shops);
        }

        [HttpGet("public/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetShopById(Guid id)
        {
            var shop = await _shopService.GetShopByIdAsync(id);
            if (shop == null) return NotFound(new { Message = "Shop not found." });
            return Ok(shop);
        }

        [HttpPost("{id}/cover-image")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> UploadCoverImage(Guid id, Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userShop = await _shopService.GetShopByOwnerIdAsync(userId);
                if (userShop == null || userShop.Id != id)
                    return Forbid();
            }

            try
            {
                var imagePath = await _shopService.UploadCoverImageAsync(id, file);
                return Ok(new { path = imagePath });
            }
            catch (System.Exception)
            {
                return StatusCode(500, new { message = "Kapak fotoğrafı yüklenirken bir hata oluştu." });
            }
        }

        [HttpPost("{id}/gallery-images")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> UploadGalleryImages(Guid id, System.Collections.Generic.List<Microsoft.AspNetCore.Http.IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userShop = await _shopService.GetShopByOwnerIdAsync(userId);
                if (userShop == null || userShop.Id != id)
                    return Forbid();
            }

            try
            {
                var formCollection = new Microsoft.AspNetCore.Http.FormFileCollection();
                formCollection.AddRange(files);

                var uploadedPaths = await _shopService.UploadGalleryImagesAsync(id, formCollection);
                return Ok(uploadedPaths);
            }
            catch (System.Exception)
            {
                return StatusCode(500, new { message = "Galeri fotoğrafları yüklenirken bir hata oluştu." });
            }
        }

        [HttpDelete("gallery-images/{imageId}")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> DeleteGalleryImage(Guid imageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdmin = User.IsInRole("Admin");
            try
            {
                await _shopService.DeleteGalleryImageAsync(imageId, userId, isAdmin);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (NotFoundException)
            {
                return NotFound(new { message = "Fotoğraf bulunamadı." });
            }
            catch (System.Exception)
            {
                return StatusCode(500, new { message = "Fotoğraf silinirken bir hata oluştu." });
            }
        }

        [HttpPatch("{id}/auto-process")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> UpdateAutoProcess(Guid id, [FromBody] bool isEnabled)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _shopService.UpdateAutoProcessAsync(userId, id, isEnabled);
            return Ok(new { Message = "Auto process setting updated." });
        }

        [HttpGet("{shopId}/closure-dates")]
        [Authorize]
        public async Task<IActionResult> GetClosureDates(Guid shopId)
        {
            var result = await _shopService.GetClosureDatesAsync(shopId);
            return Ok(result);
        }

        [HttpPost("{shopId}/closure-dates")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> AddClosureDate(Guid shopId, [FromBody] AddClosureDateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _shopService.AddClosureDateAsync(userId, shopId, request.Date, request.Reason);
            return Ok(new { Message = "Kapalı gün eklendi." });
        }

        [HttpDelete("closure-dates/{id}")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> RemoveClosureDate(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _shopService.RemoveClosureDateAsync(userId, id);
            return Ok(new { Message = "Kapalı gün silindi." });
        }
    }

    public record AddClosureDateRequest(DateTime Date, string? Reason);
}
