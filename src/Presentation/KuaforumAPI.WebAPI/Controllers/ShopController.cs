using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

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
        public async Task<IActionResult> GetAllShops()
        {
            var shops = await _shopService.GetAllShopsAsync();
            return Ok(shops);
        }

        [HttpGet("public/all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicShops()
        {
            var shops = await _shopService.GetAllShopsAsync();
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
    }
}
