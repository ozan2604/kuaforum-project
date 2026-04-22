using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
        [HttpGet("admin/all")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.Admin)]
        public async Task<IActionResult> GetAllShops(
            [FromServices] KuaforumAPI.Persistence.Contexts.ApplicationDbContext context, 
            [FromQuery] string search = "", 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            var query = context.Shops.Include(s => s.Owner).AsQueryable();

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
                Category = shop.Category,
                GenderPreference = shop.GenderPreference,
                IsActive = shop.IsActive,
                IsAutoProcessEnabled = shop.IsAutoProcessEnabled,
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
                return BadRequest("No file uploaded.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userShop = await _shopService.GetShopByOwnerIdAsync(userId);
            
            // Basic ownership check
            if (userShop == null || (userShop.Id != id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            try
            {
                var imagePath = await _shopService.UploadCoverImageAsync(id, file);
                return Ok(new { path = imagePath });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/gallery-images")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> UploadGalleryImages(Guid id, System.Collections.Generic.List<Microsoft.AspNetCore.Http.IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userShop = await _shopService.GetShopByOwnerIdAsync(userId);
            
            if (userShop == null || (userShop.Id != id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            try
            {
                var formCollection = new Microsoft.AspNetCore.Http.FormFileCollection();
                formCollection.AddRange(files);

                var uploadedPaths = await _shopService.UploadGalleryImagesAsync(id, formCollection);
                return Ok(uploadedPaths);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("gallery-images/{imageId}")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> DeleteGalleryImage(Guid imageId)
        {
            try
            {
                await _shopService.DeleteGalleryImageAsync(imageId);
                return Ok();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
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
    }
}
