using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.AdminSalon;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class AdminSalonController : ControllerBase
    {
        private readonly IAdminSalonService _adminSalonService;

        public AdminSalonController(IAdminSalonService adminSalonService)
        {
            _adminSalonService = adminSalonService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateSalon([FromBody] AdminCreateSalonDto request)
        {
            await _adminSalonService.CreateSalonAsync(request);
            return Ok(new { message = "Salon başarıyla oluşturuldu." });
        }
    }
}
