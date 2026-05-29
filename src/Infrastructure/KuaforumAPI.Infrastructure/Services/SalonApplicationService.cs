using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.SalonApplication;
using Microsoft.Extensions.Logging;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace KuaforumAPI.Infrastructure.Services
{
    public class SalonApplicationService : ISalonApplicationService
    {
        private readonly ISalonOwnerApplicationRepository _repository;
        private readonly IShopRepository _shopRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDateTimeService _dateTimeService;
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;
        private readonly ILogger<SalonApplicationService> _logger;
        private readonly IShopCodeGeneratorService _codeGenerator;

        public SalonApplicationService(ISalonOwnerApplicationRepository repository, IShopRepository shopRepository, UserManager<ApplicationUser> userManager, IDateTimeService dateTimeService, ApplicationDbContext context, ISmsService smsService, ILogger<SalonApplicationService> logger, IShopCodeGeneratorService codeGenerator)
        {
            _repository = repository;
            _shopRepository = shopRepository;
            _userManager = userManager;
            _dateTimeService = dateTimeService;
            _context = context;
            _smsService = smsService;
            _logger = logger;
            _codeGenerator = codeGenerator;
        }

        public async Task ApplyAsync(string userId, CreateSalonApplicationDto request)
        {
            // Aynı kullanıcının halihazırda bekleyen bir başvurusu varsa engelleyelim (onaylanmış/reddedilmiş olanlar sorun değil)
            var hasPendingApplication = await _context.SalonOwnerApplications
                .AnyAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Pending);
            if (hasPendingApplication)
                throw new ValidationException("Bekleyen bir başvurunuz zaten var. Sonuçlanmadan yeni başvuru yapamazsınız.");

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
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Status = ApplicationStatus.Pending,
                CreatedAt = _dateTimeService.Now
            };

            await _repository.AddAsync(application);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user?.PhoneNumber != null)
                    await _smsService.SendSmsAsync(user.PhoneNumber, SmsTemplates.SalonApplicationSubmitted());

                // Adminlere bildirim gönder
                var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
                foreach (var admin in admins)
                {
                    if (!string.IsNullOrEmpty(admin.PhoneNumber))
                    {
                        await _smsService.SendSmsAsync(admin.PhoneNumber, SmsTemplates.NewSalonApplicationToAdmin(request.ShopName));
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Başvuru SMS gönderilemedi."); }
        }

        public async Task<ContactEmailCheckResultDto> CheckContactEmailAsync(string email, string userId)
        {
            var normalizedEmail = email.Trim().ToUpperInvariant();

            // Başvuran kişinin kendi emailini salon iletişim emaili olarak kullanmasına izin ver
            var requestingUser = await _userManager.FindByIdAsync(userId);
            var isOwnEmail = requestingUser?.NormalizedEmail == normalizedEmail;

            var isUsedByShop = await _context.Shops
                .AnyAsync(s => s.ContactEmail != null && s.ContactEmail.ToUpper() == normalizedEmail);

            var isUsedByApplication = await _context.SalonOwnerApplications
                .AnyAsync(a => a.ContactEmail != null
                             && a.ContactEmail.ToUpper() == normalizedEmail
                             && a.UserId != userId
                             && (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.Approved));

            // Başka bir kullanıcının emailiyle çakışıyor mu (kendi emaili hariç)
            var conflictingUser = _userManager.Users.FirstOrDefault(u =>
                u.NormalizedEmail == normalizedEmail && u.Id != userId);
            var isRegisteredUser = conflictingUser != null;

            return new ContactEmailCheckResultDto
            {
                IsUsedByShop = isUsedByShop,
                IsUsedByApplication = isUsedByApplication,
                IsRegisteredUser = isRegisteredUser,
                IsAvailable = !isUsedByShop && !isUsedByApplication && !isRegisteredUser
            };
        }

        public async Task<List<SalonApplicationListDto>> GetPendingApplicationsAsync()
        {
            var applications = await _repository.GetPendingApplicationsWithUserAsync();

            return applications.Select(a => new SalonApplicationListDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User.UserName,
                UserFirstName = a.User.FirstName,
                UserLastName = a.User.LastName,
                ShopName = a.ShopName,
                Description = a.Description,
                ContactEmail = a.ContactEmail,
                PhoneNumber = a.PhoneNumber,
                City = a.City,
                District = a.District,
                Neighborhood = a.Neighborhood,
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
                UserFirstName = a.User.FirstName,
                UserLastName = a.User.LastName,
                ShopName = a.ShopName,
                Description = a.Description,
                ContactEmail = a.ContactEmail,
                PhoneNumber = a.PhoneNumber,
                City = a.City,
                District = a.District,
                Neighborhood = a.Neighborhood,
                Categories = a.Categories.Select(c => c.CategoryValue).ToList(),
                GenderPreference = a.GenderPreference,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task<SalonOwnerApplication> GetApplicationByUserIdAsync(string userId)
        {
            var applications = await _repository.GetByUserIdAsync(userId);
            if (!applications.Any()) return null;

            // Pending varsa önce onu göster
            var pending = applications.OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault(a => a.Status == ApplicationStatus.Pending);
            if (pending != null) return pending;

            // Approved varsa yalnızca kullanıcının hâlâ aktif salonu varsa göster
            var approved = applications.OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault(a => a.Status == ApplicationStatus.Approved);
            if (approved != null)
            {
                var hasShops = await _context.Shops.AnyAsync(s => s.OwnerId == userId);
                if (hasShops) return approved;
                // Salon yok → yeni başvuru yapılabilsin, aşağıya düş
            }

            // En son reddedilmiş başvuruyu göster (varsa)
            return applications.OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault(a => a.Status == ApplicationStatus.Rejected);
        }

        public async Task ApproveApplicationAsync(Guid applicationId)
        {
            var application = await _repository.GetByIdAsync(applicationId);
            if (application == null) throw new NotFoundException("Başvuru bulunamadı.");

            var fullAddress = !string.IsNullOrWhiteSpace(application.Address)
                ? application.Address
                : $"{application.Neighborhood} Mah., {application.Street} Sok., No: {application.BuildingNumber}, {application.District}/{application.City}";

            // Salon kodu üret
            var code = await _codeGenerator.GenerateAsync(application.City ?? "");

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
                Latitude = application.Latitude,
                Longitude = application.Longitude,
                Code = code,
                CoverImagePath = string.Empty,
                PromoVideoUrl = string.Empty,
                CreatedAt = _dateTimeService.Now,
                UpdatedAt = _dateTimeService.Now
            };

            await _shopRepository.AddAsync(shop);

            // Update Application Status
            application.Status = ApplicationStatus.Approved;

            // Rol ataması: kullanıcı zaten SalonOwner değilse ekle, Customer rolünü kaldır
            var user = await _userManager.FindByIdAsync(application.UserId);
            if (user != null)
            {
                var isSalonOwner = await _userManager.IsInRoleAsync(user, KuaforumAPI.Application.Constants.Roles.SalonOwner);
                if (!isSalonOwner)
                {
                    await _userManager.AddToRoleAsync(user, KuaforumAPI.Application.Constants.Roles.SalonOwner);
                    await _userManager.RemoveFromRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Customer);
                }
            }

            await _repository.UpdateAsync(application);

            try
            {
                if (user?.PhoneNumber != null)
                    await _smsService.SendSmsAsync(user.PhoneNumber, SmsTemplates.SalonApplicationApproved(application.ShopName ?? ""));
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }
        }

        public async Task RejectApplicationAsync(Guid applicationId)
        {
            var application = await _repository.GetByIdAsync(applicationId);
            if (application == null) throw new NotFoundException("Başvuru bulunamadı.");

            application.Status = ApplicationStatus.Rejected;
            await _repository.UpdateAsync(application);

            try
            {
                var user = await _userManager.FindByIdAsync(application.UserId);
                if (user?.PhoneNumber != null)
                    await _smsService.SendSmsAsync(user.PhoneNumber, SmsTemplates.SalonApplicationRejected());
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }
        }
    }
}
