using KuaforumAPI.Application.DTOs.Employee;
using KuaforumAPI.Application.DTOs.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<AddEmployeeResult> AddEmployeeAsync(string ownerId, CreateEmployeeDto request);
        Task<List<EmployeeListDto>> GetEmployeesAsync(string ownerId); // For Owner
        Task<List<EmployeeListDto>> GetEmployeesByShopIdAsync(Guid shopId); // For Public
        Task AssignServicesAsync(string ownerId, Guid shopEmployeeId, List<Guid> serviceIds);
        Task<List<ShopServiceDto>> GetEmployeeServicesAsync(string ownerId, Guid shopEmployeeId);
        
        Task UpdateEmployeeAsync(string ownerId, Guid shopEmployeeId, UpdateEmployeeOwnerDto request);
        Task DeleteEmployeeAsync(string ownerId, Guid shopEmployeeId);
        Task RestoreEmployeeAsync(string ownerId, Guid shopEmployeeId);
        
        Task UpdateScheduleAsync(string ownerId, Guid shopEmployeeId, UpdateScheduleDto request);
        Task<List<ScheduleDto>> GetScheduleAsync(string ownerId, Guid shopEmployeeId);

        Task<List<PublicEmployeeScheduleDto>> GetPublicShopSchedulesAsync(Guid shopId);

        Task<EmployeeProfileDto> GetMyProfileAsync(string userId);
        Task UpdateMyProfileAsync(string userId, UpdateEmployeeProfileDto request);

        Task<List<ScheduleDto>> GetMyScheduleAsync(string userId);
        Task UpdateMyScheduleAsync(string userId, UpdateScheduleDto request);

        // Leave dates (owner-managed)
        Task<List<EmployeeLeaveDateDto>> GetLeaveDatesAsync(string ownerId, Guid shopEmployeeId);
        Task AddLeaveDateAsync(string ownerId, Guid shopEmployeeId, string leaveDate, string? reason);
        Task RemoveLeaveDateAsync(string ownerId, Guid leaveDateId);
        Task<List<EmployeeLeaveDateDto>> GetPublicEmployeeLeaveDatesAsync(Guid shopEmployeeId);

        // Leave dates (self-managed by employee)
        Task<List<EmployeeLeaveDateDto>> GetMyLeaveDatesAsync(string userId);
        Task AddMyLeaveDateAsync(string userId, string leaveDate, string? reason);
        Task RemoveMyLeaveDateAsync(string userId, Guid leaveDateId);
    }
}
