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
    }
}
