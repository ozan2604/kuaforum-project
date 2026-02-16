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
        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailability(Guid employeeId, DateTime date)
        {
            // The date comes from query string, likely as 'yyyy-MM-dd'. Model binder handles it.
            // We might need to ensure it's treated as the correct date part.
            var result = await _appointmentService.GetEmployeeAvailabilityAsync(employeeId, date);
            return Ok(result);
        }
        [HttpGet("reviewable")]
        [Authorize]
        public async Task<IActionResult> GetReviewable(Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appointment = await _appointmentService.GetReviewableAppointmentAsync(userId, shopId);
            // Return 200 OK with null if no appointment, or 204 No Content. 
            // Client expects JSON usually. returning null is fine.
            return Ok(appointment);
        }
    }
}
