using System.Collections.Generic;
using System.Threading.Tasks;
using KuaforumAPI.Application.DTOs.Admin;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IAdminPasswordService
    {
        Task<List<AdminPasswordStatusDto>> GetAllStatusesAsync();
        Task<bool> SetPasswordAsync(SetAdminPasswordRequest request);
        Task<bool> DeletePasswordAsync(string key);
    }
}
