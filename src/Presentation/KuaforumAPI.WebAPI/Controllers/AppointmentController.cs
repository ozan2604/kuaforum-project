using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.Appointment;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _appointmentService.CreateAsync(userId, request);
            return Ok(new { Message = "Appointment created successfully." });
        }

        [HttpGet("my-appointments")]
        [Authorize]
        public async Task<IActionResult> GetMyAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _appointmentService.GetMyAppointmentsAsync(userId);
            return Ok(result);
        }

        [HttpGet("shop/{shopId}")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetShopAppointments(Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _appointmentService.GetShopAppointmentsAsync(userId, shopId);
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _appointmentService.UpdateStatusAsync(userId, id, request);
            return Ok(new { Message = "Appointment status updated." });
        }
    }
}
