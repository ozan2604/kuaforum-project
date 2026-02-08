using KuaforumAPI.Application.DTOs.SalonApplication;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class SalonApplicationService : ISalonApplicationService
    {
        private readonly ISalonOwnerApplicationRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;

        public SalonApplicationService(ISalonOwnerApplicationRepository repository, UserManager<ApplicationUser> userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

        public async Task ApplyAsync(string userId, CreateSalonApplicationDto request)
        {
            var application = new SalonOwnerApplication
            {
                UserId = userId,
                ShopName = request.ShopName,
                Description = request.Description,
                Status = ApplicationStatus.Pending
            };

            await _repository.AddAsync(application);
        }

        public async Task<List<SalonApplicationListDto>> GetPendingApplicationsAsync()
        {
            var applications = await _repository.GetPendingApplicationsWithUserAsync();

            return applications.Select(a => new SalonApplicationListDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User.UserName,
                ShopName = a.ShopName,
                Description = a.Description,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task ApproveApplicationAsync(Guid applicationId)
        {
            var application = await _repository.GetByIdAsync(applicationId);
            if (application == null) throw new Exception("Application not found");

            application.Status = ApplicationStatus.Approved;
            
            // Assign Role
            var user = await _userManager.FindByIdAsync(application.UserId);
            if (user != null)
            {
                await _userManager.AddToRoleAsync(user, KuaforumAPI.Application.Constants.Roles.SalonOwner);
                await _userManager.RemoveFromRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Customer);
            }

            await _repository.UpdateAsync(application);
        }

        public async Task RejectApplicationAsync(Guid applicationId)
        {
            var application = await _repository.GetByIdAsync(applicationId);
            if (application == null) throw new Exception("Application not found");

            application.Status = ApplicationStatus.Rejected;
            await _repository.UpdateAsync(application);
        }
    }
}
