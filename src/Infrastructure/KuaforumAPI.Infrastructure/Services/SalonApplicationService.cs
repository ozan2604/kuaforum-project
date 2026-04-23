using KuaforumAPI.Application.DTOs.SalonApplication;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace KuaforumAPI.Infrastructure.Services
{
    public class SalonApplicationService : ISalonApplicationService
    {
        private readonly ISalonOwnerApplicationRepository _repository;
        private readonly IShopRepository _shopRepository; // Added
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDateTimeService _dateTimeService;

        public SalonApplicationService(ISalonOwnerApplicationRepository repository, IShopRepository shopRepository, UserManager<ApplicationUser> userManager, IDateTimeService dateTimeService)
        {
            _repository = repository;
            _shopRepository = shopRepository; // Added
            _userManager = userManager;
            _dateTimeService = dateTimeService;
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
                Neighborhood = request.Neighborhood,
                Street = request.Street,
                BuildingNumber = request.BuildingNumber,
                PhoneNumber = request.PhoneNumber,
                ContactEmail = request.ContactEmail,
                Categories = request.CategoryIds.Select(id => new SalonApplicationCategoryItem { CategoryValue = id }).ToList(),
                GenderPreference = request.GenderPreference,
                Status = ApplicationStatus.Pending,
                CreatedAt = _dateTimeService.Now
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
                ContactEmail = a.ContactEmail,
                Categories = a.Categories.Select(c => c.CategoryValue).ToList(),
                GenderPreference = a.GenderPreference,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task<List<SalonApplicationListDto>> GetRejectedApplicationsAsync()
        {
            var applications = await _repository.GetRejectedApplicationsWithUserAsync();

            return applications.Select(a => new SalonApplicationListDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User.UserName,
                ShopName = a.ShopName,
                Description = a.Description,
                ContactEmail = a.ContactEmail,
                Categories = a.Categories.Select(c => c.CategoryValue).ToList(),
                GenderPreference = a.GenderPreference,
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

            var fullAddress = !string.IsNullOrWhiteSpace(application.Address) 
                ? application.Address 
                : $"{application.Neighborhood} Mah., {application.Street} Sok., No: {application.BuildingNumber}, {application.District}/{application.City}";

            // Create Shop
            var shop = new Shop
            {
                OwnerId = application.UserId,
                Name = application.ShopName ?? "Unnamed Shop",
                Description = application.Description,
                Address = fullAddress,
                City = application.City ?? "Bilinmiyor",
                District = application.District ?? "Bilinmiyor",
                Neighborhood = application.Neighborhood,
                Street = application.Street,
                BuildingNumber = application.BuildingNumber,
                PhoneNumber = application.PhoneNumber ?? "0000000000",
                ContactEmail = application.ContactEmail,
                Categories = application.Categories.Select(c => new ShopCategoryAssignment { CategoryValue = c.CategoryValue }).ToList(),
                GenderPreference = application.GenderPreference,
                CoverImagePath = string.Empty,
                CreatedAt = _dateTimeService.Now,
                UpdatedAt = _dateTimeService.Now
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
