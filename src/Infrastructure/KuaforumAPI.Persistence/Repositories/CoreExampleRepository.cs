using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Persistence.Repositories
{
    public class CoreExampleRepository : GenericRepository<CoreExample>, ICoreExampleRepository
    {
        public CoreExampleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<CoreExample> GetByNameAsync(string name)
        {
            return await _context.CoreExamples
                .FirstOrDefaultAsync(x => x.Name == name);
        }
    }
}
