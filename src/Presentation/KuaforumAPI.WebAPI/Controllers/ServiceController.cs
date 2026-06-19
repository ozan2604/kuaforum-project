using KuaforumAPI.Application.DTOs.Service;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/services")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceManagementService _serviceManagementService;

        public ServiceController(IServiceManagementService serviceManagementService)
        {
            _serviceManagementService = serviceManagementService;
        }

        [HttpPost("categories")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> CreateCategory([FromQuery] Guid shopId, [FromBody] CreateServiceCategoryDto request)
        {
            var userId = User.IsInRole("Admin") ? null : User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManagementService.CreateCategoryAsync(shopId, userId, request);
            return Ok(new { Message = "Kategori oluşturuldu." });
        }

        [HttpPost("operations")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> CreateService([FromQuery] Guid shopId, [FromBody] CreateShopServiceDto request)
        {
            var userId = User.IsInRole("Admin") ? null : User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManagementService.CreateServiceAsync(shopId, userId, request);
            return Ok(new { Message = "Hizmet oluşturuldu." });
        }

        [HttpPut("categories/{id}")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromQuery] Guid shopId, [FromBody] UpdateServiceCategoryDto request)
        {
            var userId = User.IsInRole("Admin") ? null : User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManagementService.UpdateCategoryAsync(shopId, userId, id, request);
            return Ok(new { Message = "Kategori güncellendi." });
        }

        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> DeleteCategory(Guid id, [FromQuery] Guid shopId)
        {
            var userId = User.IsInRole("Admin") ? null : User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManagementService.DeleteCategoryAsync(shopId, userId, id);
            return Ok(new { Message = "Kategori silindi." });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> UpdateService(Guid id, [FromQuery] Guid shopId, [FromBody] UpdateShopServiceDto request)
        {
            var userId = User.IsInRole("Admin") ? null : User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManagementService.UpdateServiceAsync(shopId, userId, id, request);
            return Ok(new { Message = "Hizmet güncellendi." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> DeleteService(Guid id, [FromQuery] Guid shopId)
        {
            var userId = User.IsInRole("Admin") ? null : User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManagementService.DeleteServiceAsync(shopId, userId, id);
            return Ok(new { Message = "Hizmet silindi." });
        }

        [HttpGet("my-shop")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<IActionResult> GetMyShopServices([FromQuery] Guid shopId)
        {
            var userId = User.IsInRole("Admin") ? null : User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _serviceManagementService.GetShopServicesAsync(shopId, userId);
            return Ok(result);
        }

        [HttpGet("public/{shopId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicShopServices(Guid shopId)
        {
            var result = await _serviceManagementService.GetServicesByShopIdAsync(shopId);
            return Ok(result);
        }
    }
}
