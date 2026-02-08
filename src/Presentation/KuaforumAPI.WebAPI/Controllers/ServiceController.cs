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
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateServiceCategoryDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManagementService.CreateCategoryAsync(userId, request);
            return Ok(new { Message = "Category created successfully." });
        }

        [HttpPost("operations")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> CreateService([FromBody] CreateShopServiceDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManagementService.CreateServiceAsync(userId, request);
            return Ok(new { Message = "Service created successfully." });
        }

        [HttpGet("my-shop")]
        [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.SalonOwner)]
        public async Task<IActionResult> GetMyShopServices()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _serviceManagementService.GetShopServicesAsync(userId);
            return Ok(result);
        }
    }
}
