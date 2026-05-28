using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
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

        [HttpGet("my-shops")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> GetMyShops()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shops = await _shopService.GetMyShopsAsync(userId);
            return Ok(shops);
        }

        [HttpPut("{shopId}")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> UpdateShop(Guid shopId, [FromBody] CreateShopDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _shopService.UpdateShopAsync(shopId, userId, request);
                return Ok(new { Message = "Shop updated successfully." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("{shopId}/dashboard-stats")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> GetDashboardStats(Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var stats = await _shopService.GetDashboardStatsAsync(shopId, userId);
                return Ok(stats);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
        [HttpGet("admin/all")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.Admin)]
        public async Task<IActionResult> GetAllShops(
            [FromQuery] string search = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (totalCount, shops) = await _shopService.GetAllShopsAdminAsync(search, page, pageSize);
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
        public async Task<IActionResult> GetPublicShops(
            [FromQuery] string? city = null,
            [FromQuery] string? district = null,
            [FromQuery] string? neighborhood = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (pageSize > 50) pageSize = 50;
            var result = await _shopService.GetPublicShopsPagedAsync(city, district, neighborhood, pageNumber, pageSize);
            return Ok(result);
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
                var targetShop = await _shopService.GetShopByIdAsync(id);
                if (targetShop == null || targetShop.OwnerId != userId)
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

        [HttpDelete("{id}/cover-image")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> DeleteCoverImage(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            try
            {
                await _shopService.DeleteCoverImageAsync(id, userId);
                return NoContent();
            }
            catch (System.UnauthorizedAccessException)
            {
                return Forbid();
            }
        }



        [HttpPost("{id}/videos")]
        [Authorize(Roles = "SalonOwner")]
        [RequestSizeLimit(200_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
        public async Task<IActionResult> UploadShopVideo(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            try
            {
                var result = await _shopService.UploadShopVideoAsync(id, userId, file);
                return Ok(result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpDelete("videos/{videoId}")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<IActionResult> DeleteShopVideo(Guid videoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            try
            {
                await _shopService.DeleteShopVideoAsync(videoId, userId);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
                var targetShop = await _shopService.GetShopByIdAsync(id);
                if (targetShop == null || targetShop.OwnerId != userId)
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

        [HttpPost("gallery-images/{imageId}/tags")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> AddImageTag(Guid imageId, [FromBody] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Etiket adı boş olamaz." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var tag = await _shopService.AddImageTagAsync(userId, imageId, name);
                return Ok(tag);
            }
            catch (Application.Exceptions.NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPut("gallery-images/tags/{tagId}")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> UpdateImageTag(Guid tagId, [FromBody] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Etiket adı boş olamaz." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _shopService.UpdateImageTagAsync(userId, tagId, name);
                return NoContent();
            }
            catch (Application.Exceptions.NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpDelete("gallery-images/tags/{tagId}")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> DeleteImageTag(Guid tagId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _shopService.DeleteImageTagAsync(userId, tagId);
                return NoContent();
            }
            catch (Application.Exceptions.NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
