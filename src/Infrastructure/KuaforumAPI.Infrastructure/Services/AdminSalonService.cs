using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.AdminSalon;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class AdminSalonService : IAdminSalonService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IShopRepository _shopRepository;
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IShopCodeGeneratorService _codeGenerator;
        private readonly ILogger<AdminSalonService> _logger;

        public AdminSalonService(
            UserManager<ApplicationUser> userManager,
            IShopRepository shopRepository,
            ApplicationDbContext context,
            ISmsService smsService,
            IDateTimeService dateTimeService,
            IShopCodeGeneratorService codeGenerator,
            ILogger<AdminSalonService> logger)
        {
            _userManager = userManager;
            _shopRepository = shopRepository;
            _context = context;
            _smsService = smsService;
            _dateTimeService = dateTimeService;
            _codeGenerator = codeGenerator;
            _logger = logger;
        }

        public async Task CreateSalonAsync(AdminCreateSalonDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
                bool isNewUser = false;
                string? tempPassword = null;

                if (user == null)
                {
                    isNewUser = true;
                    tempPassword = GenerateTempPassword();

                    user = new ApplicationUser
                    {
                        UserName = request.PhoneNumber,
                        Email = $"{request.PhoneNumber}@salonbir.dummy",
                        PhoneNumber = request.PhoneNumber,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(user, tempPassword);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        throw new ValidationException($"Kullanıcı oluşturulamadı: {errors}");
                    }

                    await _userManager.AddToRoleAsync(user, Roles.SalonOwner);
                }
                else
                {
                    var isSalonOwner = await _userManager.IsInRoleAsync(user, Roles.SalonOwner);
                    if (!isSalonOwner)
                    {
                        await _userManager.AddToRoleAsync(user, Roles.SalonOwner);
                        await _userManager.RemoveFromRoleAsync(user, Roles.Customer);
                    }
                }

                var code = await _codeGenerator.GenerateAsync(request.City ?? "");

                var shop = new Shop
                {
                    OwnerId = user.Id,
                    Name = request.ShopName,
                    Description = string.Empty,
                    PhoneNumber = request.PhoneNumber,
                    ContactEmail = string.Empty,
                    City = request.City ?? "",
                    District = request.District ?? "",
                    Neighborhood = request.Neighborhood ?? "",
                    Street = request.Street ?? "",
                    BuildingNumber = request.BuildingNumber ?? "",
                    Address = request.Address ?? "",
                    Categories = request.CategoryIds.Select(id => new ShopCategoryAssignment { CategoryValue = id }).ToList(),
                    GenderPreference = request.GenderPreference,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Code = code,
                    CoverImagePath = string.Empty,
                    PromoVideoUrl = string.Empty,
                    CreatedAt = _dateTimeService.Now,
                    UpdatedAt = _dateTimeService.Now
                };

                await _shopRepository.AddAsync(shop);
                await transaction.CommitAsync();

                try
                {
                    if (isNewUser && tempPassword != null)
                        await _smsService.SendSmsAsync(request.PhoneNumber, SmsTemplates.AdminCreatedNewSalonOwner(request.ShopName, tempPassword));
                    else
                        await _smsService.SendSmsAsync(request.PhoneNumber, SmsTemplates.AdminAssignedExistingSalonOwner(request.ShopName));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Admin salon oluşturma SMS gönderilemedi (ana işlem etkilenmedi).");
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string GenerateTempPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            var rng = Random.Shared;
            return $"S{upper[rng.Next(upper.Length)]}{lower[rng.Next(lower.Length)]}{digits[rng.Next(digits.Length)]}{digits[rng.Next(digits.Length)]}{lower[rng.Next(lower.Length)]}{upper[rng.Next(upper.Length)]}!";
        }
    }
}
