using KuaforumAPI.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface ICoreExampleService
    {
        Task<IEnumerable<CoreExampleDto>> GetAllAsync();
        Task<CoreExampleDto> GetByIdAsync(Guid id);
        Task<CoreExampleDto> CreateAsync(CreateCoreExampleDto createDto);
        Task UpdateAsync(Guid id, CreateCoreExampleDto updateDto);
        Task DeleteAsync(Guid id);
    }
}
