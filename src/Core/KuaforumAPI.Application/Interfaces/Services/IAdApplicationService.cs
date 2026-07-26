using KuaforumAPI.Application.DTOs.AdApplication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IAdApplicationService
    {
        Task<AdApplicationDto> CreateAdApplicationAsync(string userId, CreateAdApplicationDto dto);
        Task<IEnumerable<AdApplicationDto>> GetUserAdApplicationsAsync(string userId);
        Task<IEnumerable<AdApplicationDto>> GetAllAdApplicationsAsync();
        Task<AdApplicationDto> UpdateAdApplicationStatusAsync(Guid id, UpdateAdApplicationStatusDto dto);
        Task<IEnumerable<AdApplicationDto>> GetActiveAdsAsync();
        Task DeleteAdApplicationAsync(Guid id, string userId);
        Task<AdApplicationDto> UpdateUserAdApplicationAsync(Guid id, string userId, UpdateUserAdApplicationDto dto);
    }
}
