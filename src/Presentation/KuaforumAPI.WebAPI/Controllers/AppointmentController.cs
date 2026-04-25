using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.Appointment;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Enums;
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
        [Authorize(Roles = Roles.Customer)]
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

        [HttpGet("employee/my-appointments")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> GetAssignedAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _appointmentService.GetAssignedAppointmentsAsync(userId);
            return Ok(result);
        }

        [HttpPut("employee/{id}/status")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> UpdateStatusByEmployee(Guid id, [FromBody] UpdateAppointmentStatusDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _appointmentService.UpdateStatusByEmployeeAsync(userId, id, request);
            return Ok(new { Message = "Appointment status updated." });
        }

        [HttpGet("shop/{shopId}")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetShopAppointments(Guid shopId, [FromQuery] AppointmentStatus? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, [FromQuery] DateTime? date = null, [FromQuery] Guid? employeeId = null, [FromQuery] Guid? serviceId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _appointmentService.GetShopAppointmentsAsync(userId, shopId, status, page, pageSize, searchTerm, date, employeeId, serviceId);
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
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelByCustomer(Guid id, [FromQuery] string? reason = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _appointmentService.CancelByCustomerAsync(userId, id, reason);
            return Ok(new { Message = "Randevu iptal edildi." });
        }

        [HttpGet("availability")]
        [Authorize]
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
