using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace KuaforumAPI.Persistence.Repositories
{
    public class ShopRepository : GenericRepository<Shop>, IShopRepository
    {
        private readonly ApplicationDbContext _context;

        public ShopRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Shop> GetByOwnerIdAsync(string ownerId)
        {
            return await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == ownerId);
        }

        public async Task<IEnumerable<Shop>> GetAllWithDetailsAsync()
        {
              return await _context.Shops
                .Include(s => s.Owner)
                .ToListAsync();
        }
    }
}
