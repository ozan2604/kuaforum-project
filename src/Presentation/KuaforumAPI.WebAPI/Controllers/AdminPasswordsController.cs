using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KuaforumAPI.Application.DTOs.Admin;
using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminPasswordsController : ControllerBase
    {
        private readonly IAdminPasswordService _adminPasswordService;

        public AdminPasswordsController(IAdminPasswordService adminPasswordService)
        {
            _adminPasswordService = adminPasswordService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _adminPasswordService.GetAllStatusesAsync();
            return Ok(result);
        }

        [HttpPost("set")]
        public async Task<IActionResult> SetPassword([FromBody] SetAdminPasswordRequest request)
        {
            var result = await _adminPasswordService.SetPasswordAsync(request);
            if (!result) return BadRequest(new { message = "Geçersiz istek veya şifre belirlenemedi." });
            return Ok(new { message = "Şifre başarıyla kaydedildi." });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPassword([FromBody] SetAdminPasswordRequest request)
        {
            var result = await _adminPasswordService.VerifyPasswordAsync(request.Key, request.Password);
            if (!result) return BadRequest(new { message = "Şifre yanlış veya belirlenmemiş." });
            return Ok(new { message = "Şifre doğru." });
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> DeletePassword(string key)
        {
            var result = await _adminPasswordService.DeletePasswordAsync(key);
            if (!result) return NotFound(new { message = "Şifre bulunamadı." });
            return Ok(new { message = "Şifre başarıyla silindi." });
        }
    }
}
