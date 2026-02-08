using KuaforumAPI.Domain.Entities;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Repositories
{
    public interface ICoreExampleRepository : IGenericRepository<CoreExample>
    {
        // Example custom method:
        Task<CoreExample> GetByNameAsync(string name);
    }
}
