using KuaforumAPI.Application.DTOs.Appointment;
using KuaforumAPI.Application.DTOs.Common;
using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IAppointmentService
    {
        Task CreateAsync(string userId, CreateAppointmentDto request);
        Task CreateManualAsync(string staffUserId, CreateManualAppointmentDto request);
        Task<PagedResult<AppointmentDto>> GetMyAppointmentsAsync(string userId, int page = 1, int pageSize = 20);
        Task<PagedResult<AppointmentDto>> GetShopAppointmentsAsync(string? ownerId, Guid shopId, AppointmentStatus? status = null, int page = 1, int pageSize = 10, string? searchTerm = null, DateTime? date = null, Guid? employeeId = null, Guid? serviceId = null);
        Task<NoShowResultDto?> UpdateStatusAsync(string? ownerId, Guid appointmentId, UpdateAppointmentStatusDto request);
        Task<EmployeeAvailabilityDto> GetEmployeeAvailabilityAsync(Guid employeeId, DateTime date);
        Task<AppointmentDto> GetReviewableAppointmentAsync(string userId, Guid shopId);
        Task<List<AppointmentDto>> GetAssignedAppointmentsAsync(string employeeUserId, DateTime? from = null, DateTime? to = null);
        Task<PagedResult<AppointmentDto>> GetAssignedAppointmentsPagedAsync(string employeeUserId, AppointmentStatus? status = null, int page = 1, int pageSize = 10, string? searchTerm = null, DateTime? date = null, Guid? serviceId = null);
        Task<NoShowResultDto?> UpdateStatusByEmployeeAsync(string employeeUserId, Guid appointmentId, UpdateAppointmentStatusDto request);
        Task<NoShowResultDto?> UpdateGroupStatusByEmployeeAsync(string employeeUserId, Guid groupId, UpdateAppointmentStatusDto request);
        Task CancelByCustomerAsync(string userId, Guid appointmentId, string? reason = null);
        Task CancelGroupAsync(string userId, Guid groupId, string? reason = null);
        Task<NoShowResultDto?> UpdateGroupStatusAsync(string? ownerId, Guid groupId, UpdateAppointmentStatusDto request);
    }
}
