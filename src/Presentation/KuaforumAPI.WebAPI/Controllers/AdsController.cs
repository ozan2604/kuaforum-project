using KuaforumAPI.Application.DTOs.AdApplication;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdsController : ControllerBase
    {
        private readonly IAdApplicationService _adService;

        public AdsController(IAdApplicationService adService)
        {
            _adService = adService;
        }

        [HttpPost]
        [Authorize]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB limit for videos
        public async Task<IActionResult> CreateAd([FromForm] CreateAdApplicationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adService.CreateAdApplicationAsync(userId, dto);
            return Ok(result);
        }

        [HttpGet("my-ads")]
        [Authorize]
        public async Task<IActionResult> GetMyAds()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adService.GetUserAdApplicationsAsync(userId);
            return Ok(result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAdsForAdmin()
        {
            var result = await _adService.GetAllAdApplicationsAsync();
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAdApplicationStatusDto dto)
        {
            var result = await _adService.UpdateAdApplicationStatusAsync(id, dto);
            return Ok(result);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAds()
        {
            var result = await _adService.GetActiveAdsAsync();
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB limit for videos
        public async Task<IActionResult> UpdateAd(Guid id, [FromForm] UpdateUserAdApplicationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adService.UpdateUserAdApplicationAsync(id, userId, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAd(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _adService.DeleteAdApplicationAsync(id, userId);
            return NoContent();
        }
    }
}
