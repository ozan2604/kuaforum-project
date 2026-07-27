using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KuaforumAPI.Application.DTOs.Analytics;
using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly ISiteAnalyticsService _analyticsService;

        public AnalyticsController(ISiteAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpPost("log-visit")]
        [AllowAnonymous]
        public async Task<IActionResult> LogVisit([FromBody] LogVisitDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _analyticsService.LogVisitAsync(dto, ip);
            return Ok(new { success = result });
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _analyticsService.GetSiteStatsAsync();
            return Ok(new { success = true, data = result });
        }

        [HttpGet("shop-stats/{shopId}")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<IActionResult> GetShopStats(System.Guid shopId)
        {
            var result = await _analyticsService.GetShopStatsAsync(shopId);
            return Ok(new { success = true, data = result });
        }
    }
}
