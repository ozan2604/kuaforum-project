using KuaforumAPI.Application.DTOs.AdminSalon;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IAdminSalonService
    {
        Task CreateSalonAsync(AdminCreateSalonDto request);
    }
}
