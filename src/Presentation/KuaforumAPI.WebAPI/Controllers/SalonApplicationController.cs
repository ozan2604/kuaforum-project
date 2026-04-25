using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.SalonApplication;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalonApplicationController : ControllerBase
    {
        private readonly ISalonApplicationService _salonApplicationService;

        public SalonApplicationController(ISalonApplicationService salonApplicationService)
        {
            _salonApplicationService = salonApplicationService;
        }

        [HttpPost("apply")]
        [Authorize]
        public async Task<IActionResult> Apply([FromBody] CreateSalonApplicationDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _salonApplicationService.ApplyAsync(userId, request);
            return Ok(new { Message = "Application submitted successfully." });
        }

        [HttpGet("check-contact-email")]
        [Authorize]
        public async Task<IActionResult> CheckContactEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "E-posta adresi boş olamaz." });

            var result = await _salonApplicationService.CheckContactEmailAsync(email);
            return Ok(result);
        }

        [HttpGet("my-application")]
        [Authorize]
        public async Task<IActionResult> GetMyApplication()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var application = await _salonApplicationService.GetApplicationByUserIdAsync(userId);
            return Ok(application);
        }

        [HttpGet("pending")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetPendingApplications()
        {
            var applications = await _salonApplicationService.GetPendingApplicationsAsync();
            return Ok(applications);
        }

        [HttpGet("rejected")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetRejectedApplications()
        {
            var applications = await _salonApplicationService.GetRejectedApplicationsAsync();
            return Ok(applications);
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> ApproveApplication(Guid id)
        {
            await _salonApplicationService.ApproveApplicationAsync(id);
            return Ok(new { Message = "Application approved. User is now a Salon Owner." });
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> RejectApplication(Guid id)
        {
            await _salonApplicationService.RejectApplicationAsync(id);
            return Ok(new { Message = "Application rejected." });
        }
    }
}
