using KuaforumAPI.Application.DTOs.Employee;
using KuaforumAPI.Application.DTOs.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task AddEmployeeAsync(string ownerId, CreateEmployeeDto request);
        Task AssignServicesAsync(string ownerId, Guid shopEmployeeId, List<Guid> serviceIds);
        Task<List<ShopServiceDto>> GetEmployeeServicesAsync(string ownerId, Guid shopEmployeeId);
        
        Task UpdateScheduleAsync(string ownerId, Guid shopEmployeeId, UpdateScheduleDto request);
        Task<List<ScheduleDto>> GetScheduleAsync(string ownerId, Guid shopEmployeeId);
    }
}
