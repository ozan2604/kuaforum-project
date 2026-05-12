using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.Block;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/shop-block")]
    [ApiController]
    public class ShopBlockController : ControllerBase
    {
        private readonly IShopBlockService _blockService;

        public ShopBlockController(IShopBlockService blockService)
        {
            _blockService = blockService;
        }

        [HttpPost("{shopId}/block")]
        [Authorize(Roles = $"{Roles.SalonOwner},{Roles.Employee}")]
        public async Task<IActionResult> BlockCustomer(Guid shopId, [FromBody] BlockCustomerDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _blockService.BlockCustomerAsync(userId, shopId, dto);
            return Ok(new { Message = "Müşteri engellendi." });
        }

        [HttpDelete("{shopId}/block/{customerId}")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> UnblockCustomer(Guid shopId, string customerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _blockService.UnblockCustomerAsync(userId, shopId, customerId);
            return NoContent();
        }

        [HttpGet("{shopId}/blocked-customers")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetBlockedCustomers(Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _blockService.GetBlockedCustomersAsync(userId, shopId);
            return Ok(result);
        }

        [HttpGet("{shopId}/customer-by-phone")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetCustomerByPhone(Guid shopId, [FromQuery] string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { Message = "Telefon numarası zorunludur." });
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _blockService.GetCustomerByPhoneAsync(userId, shopId, phone);
            if (result == null)
                return NotFound(new { Message = "Bu numarada kayıtlı müşteri bulunamadı." });
            return Ok(result);
        }
    }
}
