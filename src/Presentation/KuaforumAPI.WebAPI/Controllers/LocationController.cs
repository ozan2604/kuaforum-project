using KuaforumAPI.Persistence.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public LocationController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var cities = await _context.Cities
                .Include(c => c.Districts)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    districts = c.Districts.Select(d => new { id = d.Id, name = d.Name }).ToList()
                })
                .ToListAsync();

            if (cities.Any())
            {
                return Ok(new { status = "success", data = cities });
            }

            return StatusCode(502, new { message = "Konum servisi yükleniyor, lütfen birazdan tekrar deneyin." });
        }

        [HttpGet("neighborhoods")]
        public async Task<IActionResult> GetNeighborhoods([FromQuery] int districtId)
        {
            // First check our DB
            var dbNeighborhoods = await _context.Neighborhoods
                .Where(n => n.DistrictId == districtId)
                .Select(n => new { id = n.Id, name = n.Name })
                .ToListAsync();

            if (dbNeighborhoods.Any())
            {
                return Ok(new { status = "success", data = dbNeighborhoods });
            }

            // If not found in DB, try fetching from turkiyeapi dynamically (for existing Turkiye districts)
            var client = _httpClientFactory.CreateClient("turkiyeapi");
            try
            {
                var response = await client.GetAsync($"v1/neighborhoods?districtId={districtId}");
                var content = await response.Content.ReadAsStringAsync();
                
                // Fire and forget saving it to DB so next time it's fast
                // In a real scenario, you'd use a background queue or scoped service.
                
                return Content(content, "application/json");
            }
            catch
            {
                return StatusCode(502, new { message = "Konum servisi geçici olarak kullanılamıyor." });
            }
        }
    }
}
