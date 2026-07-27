using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;

namespace KuaforumAPI.Persistence.Repositories
{
    public class SiteVisitRepository : GenericRepository<SiteVisit>, ISiteVisitRepository
    {
        public SiteVisitRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
