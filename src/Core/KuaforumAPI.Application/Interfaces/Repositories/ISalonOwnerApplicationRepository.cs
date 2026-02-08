using KuaforumAPI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Repositories
{
    public interface ISalonOwnerApplicationRepository : IGenericRepository<SalonOwnerApplication>
    {
        Task<List<SalonOwnerApplication>> GetPendingApplicationsWithUserAsync();
    }
}
