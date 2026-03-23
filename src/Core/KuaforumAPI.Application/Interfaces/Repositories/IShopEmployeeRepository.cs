using KuaforumAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Repositories
{
    public interface IShopEmployeeRepository : IGenericRepository<ShopEmployee>
    {
        Task<IEnumerable<ShopEmployee>> GetByShopIdAsync(Guid shopId);
        Task<IEnumerable<ShopEmployee>> GetByShopIdWithSchedulesAsync(Guid shopId);

    }
}
