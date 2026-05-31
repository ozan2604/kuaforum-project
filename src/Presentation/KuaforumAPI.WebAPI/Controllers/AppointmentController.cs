using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.Appointment;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        [Authorize(Roles = $"{Roles.Customer},{Roles.SalonOwner},{Roles.Employee}")]
        [EnableRateLimiting("appointments")]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _appointmentService.CreateAsync(userId, request);
            return Ok(new { Message = "Appointment created successfully." });
        }

        [HttpPost("manual")]
        [Authorize(Roles = $"{Roles.SalonOwner},{Roles.Employee}")]
        public async Task<IActionResult> CreateManual([FromBody] CreateManualAppointmentDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _appointmentService.CreateManualAsync(userId, request);
            return Ok(new { Message = "Manuel randevu oluşturuldu." });
        }

        [HttpGet("my-appointments")]
        [Authorize]
        public async Task<IActionResult> GetMyAppointments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _appointmentService.GetMyAppointmentsAsync(userId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("employee/my-appointments")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> GetAssignedAppointments([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _appointmentService.GetAssignedAppointmentsAsync(userId, from, to);
            return Ok(result);
        }

        [HttpGet("employee/my-appointments/paged")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> GetAssignedAppointmentsPaged([FromQuery] AppointmentStatus? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, [FromQuery] DateTime? date = null, [FromQuery] Guid? serviceId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _appointmentService.GetAssignedAppointmentsPagedAsync(userId, status, page, pageSize, searchTerm, date, serviceId);
            return Ok(result);
        }

        [HttpPut("employee/{id}/status")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> UpdateStatusByEmployee(Guid id, [FromBody] UpdateAppointmentStatusDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var noShowResult = await _appointmentService.UpdateStatusByEmployeeAsync(userId, id, request);
            return Ok(new { Message = "Appointment status updated.", noShowResult });
        }

        [HttpPut("employee/group/{groupId}/status")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> UpdateGroupStatusByEmployee(Guid groupId, [FromBody] UpdateAppointmentStatusDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var noShowResult = await _appointmentService.UpdateGroupStatusByEmployeeAsync(userId, groupId, request);
            return Ok(new { Message = "Group appointment status updated.", noShowResult });
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
            var noShowResult = await _appointmentService.UpdateStatusAsync(userId, id, request);
            return Ok(new { Message = "Appointment status updated.", noShowResult });
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
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailability([FromQuery] Guid employeeId, [FromQuery] string date)
        {
            // Kültür bağımsız parse: "yyyy-MM-dd" formatı zorunlu
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedDate))
            {
                return BadRequest(new { Message = "Geçersiz tarih formatı. Beklenen: yyyy-MM-dd" });
            }
            var result = await _appointmentService.GetEmployeeAvailabilityAsync(employeeId, parsedDate);
            return Ok(result);
        }
        [HttpDelete("group/{groupId}")]
        [Authorize]
        public async Task<IActionResult> CancelGroup(Guid groupId, [FromQuery] string? reason = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _appointmentService.CancelGroupAsync(userId, groupId, reason);
            return Ok(new { Message = "Grup randevusu iptal edildi." });
        }

        [HttpPut("group/{groupId}/status")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> UpdateGroupStatus(Guid groupId, [FromBody] UpdateAppointmentStatusDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var noShowResult = await _appointmentService.UpdateGroupStatusAsync(userId, groupId, request);
            return Ok(new { Message = "Grup randevu durumu güncellendi.", noShowResult });
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
