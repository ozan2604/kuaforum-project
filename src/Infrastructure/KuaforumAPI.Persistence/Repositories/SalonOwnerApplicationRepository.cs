using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Persistence.Repositories
{
    public class SalonOwnerApplicationRepository : GenericRepository<SalonOwnerApplication>, ISalonOwnerApplicationRepository
    {
        private readonly ApplicationDbContext _context;

        public SalonOwnerApplicationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<SalonOwnerApplication>> GetPendingApplicationsWithUserAsync()
        {
            return await _context.SalonOwnerApplications
                .Include(a => a.User)
                .Where(a => a.Status == ApplicationStatus.Pending)
                .ToListAsync();
        }

        public async Task<List<SalonOwnerApplication>> GetRejectedApplicationsWithUserAsync()
        {
            return await _context.SalonOwnerApplications
                .Include(a => a.User)
                .Where(a => a.Status == ApplicationStatus.Rejected)
                .ToListAsync();
        }

        public async Task<List<SalonOwnerApplication>> GetByUserIdAsync(string userId)
        {
            return await _context.SalonOwnerApplications
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }
    }
}
