using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Persistence.Repositories
{
    public class ShopImageRepository : GenericRepository<ShopImage>, IShopImageRepository
    {
        public ShopImageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ShopImage>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.ShopImages
                .Include(si => si.Tags)
                .Where(si => si.ShopId == shopId)
                .ToListAsync();
        }
    }
}
