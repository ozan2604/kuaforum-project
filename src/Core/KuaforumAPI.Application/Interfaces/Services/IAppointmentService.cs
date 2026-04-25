using KuaforumAPI.Application.DTOs.Appointment;
using KuaforumAPI.Application.DTOs.Common;
using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IAppointmentService
    {
        Task CreateAsync(string userId, CreateAppointmentDto request);
        Task<List<AppointmentDto>> GetMyAppointmentsAsync(string userId);
        Task<PagedResult<AppointmentDto>> GetShopAppointmentsAsync(string ownerId, Guid shopId, AppointmentStatus? status = null, int page = 1, int pageSize = 10, string? searchTerm = null, DateTime? date = null, Guid? employeeId = null, Guid? serviceId = null);
        Task UpdateStatusAsync(string ownerId, Guid appointmentId, UpdateAppointmentStatusDto request);
        Task<EmployeeAvailabilityDto> GetEmployeeAvailabilityAsync(Guid employeeId, DateTime date);
        Task<AppointmentDto> GetReviewableAppointmentAsync(string userId, Guid shopId);
        Task<List<AppointmentDto>> GetAssignedAppointmentsAsync(string employeeUserId);
        Task UpdateStatusByEmployeeAsync(string employeeUserId, Guid appointmentId, UpdateAppointmentStatusDto request);
        Task CancelByCustomerAsync(string userId, Guid appointmentId, string? reason = null);
    }
}
