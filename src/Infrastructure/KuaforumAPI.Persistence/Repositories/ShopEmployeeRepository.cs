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
    public class ShopEmployeeRepository : GenericRepository<ShopEmployee>, IShopEmployeeRepository
    {
        private readonly ApplicationDbContext _context;

        public ShopEmployeeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ShopEmployee>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.ShopEmployees
                .Where(e => e.ShopId == shopId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ShopEmployee>> GetByShopIdWithSchedulesAsync(Guid shopId)
        {
            return await _context.ShopEmployees
                .Where(e => e.ShopId == shopId)
                .Include(e => e.Schedules)
                .ToListAsync();
        }
    }
}
