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
        public async Task<IActionResult> AddEmployee([FromBody] CreateEmployeeDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.AddEmployeeAsync(userId, request);

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

        [HttpPost("{id}/services")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> AssignServices(Guid id, [FromBody] AssignServicesDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.AssignServicesAsync(userId, id, request.ServiceIds);
            return Ok(new { Message = "Services assigned successfully." });
        }

        [HttpGet]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetEmployees()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetEmployeesAsync(userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeOwnerDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.UpdateEmployeeAsync(userId, id, request);
            return Ok(new { Message = "Employee updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> DeleteEmployee(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.DeleteEmployeeAsync(userId, id);
            return Ok(new { Message = "Employee deleted successfully." });
        }

        [HttpPatch("{id}/restore")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> RestoreEmployee(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.RestoreEmployeeAsync(userId, id);
            return Ok(new { Message = "Çalışan başarıyla geri yüklendi." });
        }

        [HttpGet("{id}/services")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetServices(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetEmployeeServicesAsync(userId, id);
            return Ok(result);
        }

        [HttpPut("{id}/schedule")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] UpdateScheduleDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _employeeService.UpdateScheduleAsync(userId, id, request);
            return Ok(new { Message = "Schedule updated successfully." });
        }

        [HttpGet("{id}/schedule")]
        [Authorize(Roles = Roles.SalonOwner)]
        public async Task<IActionResult> GetSchedule(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _employeeService.GetScheduleAsync(userId, id);
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
            return Ok(new { Message = "Profile updated successfully." });
        }
    }
}
