using KuaforumAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Repositories
{
    public interface IShopImageRepository : IGenericRepository<ShopImage>
    {
        Task<IEnumerable<ShopImage>> GetByShopIdAsync(Guid shopId);
    }
}
