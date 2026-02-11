using KuaforumAPI.Application.DTOs.SalonApplication;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace KuaforumAPI.Infrastructure.Services
{
    public class SalonApplicationService : ISalonApplicationService
    {
        private readonly ISalonOwnerApplicationRepository _repository;
        private readonly IShopRepository _shopRepository; // Added
        private readonly UserManager<ApplicationUser> _userManager;

        public SalonApplicationService(ISalonOwnerApplicationRepository repository, IShopRepository shopRepository, UserManager<ApplicationUser> userManager)
        {
            _repository = repository;
            _shopRepository = shopRepository; // Added
            _userManager = userManager;
        }

        public async Task ApplyAsync(string userId, CreateSalonApplicationDto request)
        {
            var application = new SalonOwnerApplication
            {
                UserId = userId,
                ShopName = request.ShopName,
                Description = request.Description,
                Address = request.Address,
                City = request.City,
                District = request.District,
                PhoneNumber = request.PhoneNumber,
                TaxNumber = request.TaxNumber,
                Status = ApplicationStatus.Pending,
                CreatedAt = DateTime.UtcNow // Ensure creation time
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
                TaxNumber = a.TaxNumber,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task<SalonOwnerApplication> GetApplicationByUserIdAsync(string userId)
        {
            var applications = await _repository.GetByUserIdAsync(userId);
            // Assuming one active application per user for now, or get the latest
            return applications.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
        }

        public async Task ApproveApplicationAsync(Guid applicationId)
        {
            var application = await _repository.GetByIdAsync(applicationId);
            if (application == null) throw new Exception("Application not found");

            // Check if user already has a shop to prevent duplicates (optional but good safety)
            var existingShop = await _shopRepository.GetByOwnerIdAsync(application.UserId);
            if (existingShop != null)
            {
                throw new Exception("User already has a shop.");
            }

            // Create Shop
            var shop = new Shop
            {
                OwnerId = application.UserId,
                Name = application.ShopName,
                Description = application.Description,
                Address = application.Address,
                City = application.City,
                District = application.District,
                PhoneNumber = application.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _shopRepository.AddAsync(shop);

            // Update Application Status
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
