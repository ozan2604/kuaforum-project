using Microsoft.AspNetCore.Mvc;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static string? _provincesCache;

        public LocationController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // İller nadiren değişir — in-memory cache ile tutulur
        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            if (_provincesCache != null)
                return Content(_provincesCache, "application/json");

            var client = _httpClientFactory.CreateClient("turkiyeapi");
            try
            {
                var response = await client.GetAsync("v1/provinces");
                var content = await response.Content.ReadAsStringAsync();
                _provincesCache = content;
                return Content(content, "application/json");
            }
            catch
            {
                return StatusCode(502, new { message = "Konum servisi geçici olarak kullanılamıyor." });
            }
        }

        [HttpGet("neighborhoods")]
        public async Task<IActionResult> GetNeighborhoods([FromQuery] int districtId)
        {
            var client = _httpClientFactory.CreateClient("turkiyeapi");
            try
            {
                var response = await client.GetAsync($"v1/neighborhoods?districtId={districtId}");
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch
            {
                return StatusCode(502, new { message = "Konum servisi geçici olarak kullanılamıyor." });
            }
        }
    }
}
