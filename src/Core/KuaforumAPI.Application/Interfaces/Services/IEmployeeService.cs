using KuaforumAPI.Application.DTOs.Employee;
using KuaforumAPI.Application.DTOs.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<AddEmployeeResult> AddEmployeeAsync(Guid shopId, string ownerId, CreateEmployeeDto request);
        Task<List<EmployeeListDto>> GetEmployeesAsync(Guid shopId, string ownerId); // For Owner
        Task<List<EmployeeListDto>> GetEmployeesByShopIdAsync(Guid shopId); // For Public
        Task AssignServicesAsync(Guid shopId, string ownerId, Guid shopEmployeeId, List<Guid> serviceIds);
        Task<List<ShopServiceDto>> GetEmployeeServicesAsync(Guid shopId, string ownerId, Guid shopEmployeeId);

        Task UpdateEmployeeAsync(Guid shopId, string ownerId, Guid shopEmployeeId, UpdateEmployeeOwnerDto request);
        Task DeleteEmployeeAsync(Guid shopId, string ownerId, Guid shopEmployeeId);
        Task RestoreEmployeeAsync(Guid shopId, string ownerId, Guid shopEmployeeId);

        Task UpdateScheduleAsync(Guid shopId, string ownerId, Guid shopEmployeeId, UpdateScheduleDto request);
        Task<List<ScheduleDto>> GetScheduleAsync(Guid shopId, string ownerId, Guid shopEmployeeId);

        Task<List<PublicEmployeeScheduleDto>> GetPublicShopSchedulesAsync(Guid shopId);

        Task<EmployeeProfileDto> GetMyProfileAsync(string userId);
        Task UpdateMyProfileAsync(string userId, UpdateEmployeeProfileDto request);

        Task<List<ScheduleDto>> GetMyScheduleAsync(string userId);
        Task UpdateMyScheduleAsync(string userId, UpdateScheduleDto request);

        // Leave dates (owner-managed)
        Task<List<EmployeeLeaveDateDto>> GetLeaveDatesAsync(Guid shopId, string ownerId, Guid shopEmployeeId);
        Task AddLeaveDateAsync(Guid shopId, string ownerId, Guid shopEmployeeId, string leaveDate, string? reason);
        Task RemoveLeaveDateAsync(Guid shopId, string ownerId, Guid leaveDateId);
        Task<List<EmployeeLeaveDateDto>> GetPublicEmployeeLeaveDatesAsync(Guid shopEmployeeId);

        // Leave dates (self-managed by employee)
        Task<List<EmployeeLeaveDateDto>> GetMyLeaveDatesAsync(string userId);
        Task AddMyLeaveDateAsync(string userId, string leaveDate, string? reason);
        Task RemoveMyLeaveDateAsync(string userId, Guid leaveDateId);
    }
}
