using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.Employee;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpPost]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> AddEmployee([FromQuery] Guid shopId, [FromBody] CreateEmployeeDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.AddEmployeeAsync(shopId, userId, request);

            string message = result.IsNewUser
                ? $"{result.FirstName} {result.LastName} sisteme başarıyla kaydedildi ve çalışan olarak eklendi. Giriş bilgileri — Telefon: {result.PhoneNumber} | Geçici Şifre: {result.TemporaryPassword}"
                : $"{result.FirstName} {result.LastName} zaten sistemde kayıtlıydı, çalışan rolü başarıyla eklendi.";

            return Ok(new
            {
                message = message,
                temporaryPassword = result.TemporaryPassword,
                isNewUser = result.IsNewUser
            });
        }

        [HttpGet]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetEmployees([FromQuery] Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetEmployeesAsync(shopId, userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> UpdateEmployee(Guid id, [FromQuery] Guid shopId, [FromBody] UpdateEmployeeOwnerDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.UpdateEmployeeAsync(shopId, userId, id, request);
            return Ok(new { Message = "Çalışan güncellendi." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> DeleteEmployee(Guid id, [FromQuery] Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.DeleteEmployeeAsync(shopId, userId, id);
            return Ok(new { Message = "Çalışan silindi." });
        }

        [HttpPatch("{id}/restore")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> RestoreEmployee(Guid id, [FromQuery] Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.RestoreEmployeeAsync(shopId, userId, id);
            return Ok(new { Message = "Çalışan başarıyla geri yüklendi." });
        }

        [HttpPost("{id}/services")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> AssignServices(Guid id, [FromQuery] Guid shopId, [FromBody] AssignServicesDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.AssignServicesAsync(shopId, userId, id, request.ServiceIds);
            return Ok(new { Message = "Hizmetler atandı." });
        }

        [HttpGet("{id}/services")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetServices(Guid id, [FromQuery] Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetEmployeeServicesAsync(shopId, userId, id);
            return Ok(result);
        }

        [HttpPut("{id}/schedule")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> UpdateSchedule(Guid id, [FromQuery] Guid shopId, [FromBody] UpdateScheduleDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.UpdateScheduleAsync(shopId, userId, id, request);
            return Ok(new { Message = "Çalışma saatleri güncellendi." });
        }

        [HttpGet("{id}/schedule")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetSchedule(Guid id, [FromQuery] Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetScheduleAsync(shopId, userId, id);
            return Ok(result);
        }

        [HttpGet("public/shop/{shopId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetShopEmployees(Guid shopId)
        {
            var result = await _employeeService.GetEmployeesByShopIdAsync(shopId);
            return Ok(result);
        }

        [HttpGet("public/shop/{shopId}/schedules")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicShopSchedules(Guid shopId)
        {
            var result = await _employeeService.GetPublicShopSchedulesAsync(shopId);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetMyProfileAsync(userId);
            return Ok(result);
        }

        [HttpPut("me")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateEmployeeProfileDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.UpdateMyProfileAsync(userId, request);
            return Ok(new { Message = "Profil güncellendi." });
        }

        [HttpGet("me/schedule")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> GetMySchedule()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetMyScheduleAsync(userId);
            return Ok(result);
        }

        [HttpPut("me/schedule")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> UpdateMySchedule([FromBody] UpdateScheduleDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.UpdateMyScheduleAsync(userId, request);
            return Ok(new { Message = "Çalışma saatleri başarıyla güncellendi." });
        }

        // ─── Leave Dates ──────────────────────────────────────────────────────────

        [HttpGet("{id}/leave-dates")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetLeaveDates(Guid id, [FromQuery] Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetLeaveDatesAsync(shopId, userId, id);
            return Ok(result);
        }

        [HttpPost("{id}/leave-dates")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> AddLeaveDate(Guid id, [FromQuery] Guid shopId, [FromBody] AddLeaveDateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.AddLeaveDateAsync(shopId, userId, id, request.LeaveDate, request.Reason);
            return Ok(new { Message = "İzin günü eklendi." });
        }

        [HttpDelete("leave-dates/{leaveDateId}")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> RemoveLeaveDate(Guid leaveDateId, [FromQuery] Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.RemoveLeaveDateAsync(shopId, userId, leaveDateId);
            return Ok(new { Message = "İzin günü silindi." });
        }

        [HttpGet("{id}/leave-dates/public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicLeaveDates(Guid id)
        {
            var result = await _employeeService.GetPublicEmployeeLeaveDatesAsync(id);
            return Ok(result);
        }

        // ─── Employee self-managed leave dates ───────────────────────────────

        [HttpGet("me/leave-dates")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> GetMyLeaveDates()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetMyLeaveDatesAsync(userId);
            return Ok(result);
        }

        [HttpPost("me/leave-dates")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> AddMyLeaveDate([FromBody] AddLeaveDateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.AddMyLeaveDateAsync(userId, request.LeaveDate, request.Reason);
            return Ok(new { Message = "İzin günü eklendi." });
        }

        [HttpDelete("me/leave-dates/{leaveDateId}")]
        [Authorize(Roles = Roles.Employee)]
        public async Task<IActionResult> RemoveMyLeaveDate(Guid leaveDateId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.RemoveMyLeaveDateAsync(userId, leaveDateId);
            return Ok(new { Message = "İzin günü silindi." });
        }
    }

    public class AddLeaveDateRequest
    {
        public string LeaveDate { get; set; }
        public string? Reason { get; set; }
    }
}
