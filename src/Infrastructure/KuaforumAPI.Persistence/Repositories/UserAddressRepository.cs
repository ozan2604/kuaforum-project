using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Persistence.Repositories
{
    public class UserAddressRepository : GenericRepository<UserAddress>, IUserAddressRepository
    {
        public UserAddressRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserAddress>> GetByUserIdAsync(string userId)
        {
            return await _dbSet.Where(ua => ua.UserId == userId).ToListAsync();
        }
    }
}
