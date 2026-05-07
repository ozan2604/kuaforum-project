using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeocodingController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GeocodingController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET /api/Geocoding/search?q=...
        [HttpGet("search")]
        [EnableRateLimiting("geocoding")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Adres boş olamaz." });

            var client = _httpClientFactory.CreateClient("nominatim");
            var encoded = Uri.EscapeDataString(q.Trim());
            var url = $"https://nominatim.openstreetmap.org/search?q={encoded}&format=json&limit=1&countrycodes=tr&addressdetails=0";

            try
            {
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch
            {
                return StatusCode(502, new { message = "Geocoding servisine ulaşılamadı." });
            }
        }
    }
}
