using KuaforumAPI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Repositories
{
    public interface IUserAddressRepository : IGenericRepository<UserAddress>
    {
        Task<IEnumerable<UserAddress>> GetByUserIdAsync(string userId);
    }
}
