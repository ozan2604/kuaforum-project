using FluentValidation;
using KuaforumAPI.Application.DTOs.Service;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace KuaforumAPI.Infrastructure.Services
{
    public class ServiceManagementService : IServiceManagementService
    {
        private readonly IShopRepository _shopRepository;
        private readonly IGenericRepository<ServiceCategory> _categoryRepository;
        private readonly IGenericRepository<Domain.Entities.ShopService> _serviceRepository;
        private readonly IValidator<CreateServiceCategoryDto> _categoryValidator;
        private readonly IValidator<CreateShopServiceDto> _serviceValidator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceManagementService> _logger;

        public ServiceManagementService(
            IShopRepository shopRepository,
            IGenericRepository<ServiceCategory> categoryRepository,
            IGenericRepository<KuaforumAPI.Domain.Entities.ShopService> serviceRepository,
            IValidator<CreateServiceCategoryDto> categoryValidator,
            IValidator<CreateShopServiceDto> serviceValidator,
            ApplicationDbContext context,
            ILogger<ServiceManagementService> logger)
        {
            _shopRepository = shopRepository;
            _categoryRepository = categoryRepository;
            _serviceRepository = serviceRepository;
            _categoryValidator = categoryValidator;
            _serviceValidator = serviceValidator;
            _context = context;
            _logger = logger;
        }

        public async Task CreateCategoryAsync(Guid shopId, string ownerId, CreateServiceCategoryDto request)
        {
            var validationResult = await _categoryValidator.ValidateAsync(request);
            if (!validationResult.IsValid) throw new FluentValidation.ValidationException(validationResult.Errors);

            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || shop.OwnerId != ownerId)
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var category = new ServiceCategory
            {
                ShopId = shopId,
                Name = request.Name,
                Description = request.Description
            };

            await _categoryRepository.AddAsync(category);
        }

        public async Task CreateServiceAsync(Guid shopId, string ownerId, CreateShopServiceDto request)
        {
            var validationResult = await _serviceValidator.ValidateAsync(request);
            if (!validationResult.IsValid) throw new FluentValidation.ValidationException(validationResult.Errors);

            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || shop.OwnerId != ownerId)
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null || category.ShopId != shopId)
                throw new FluentValidation.ValidationException("Geçersiz kategori.");

            var service = new KuaforumAPI.Domain.Entities.ShopService
            {
                ShopId = shopId,
                CategoryId = request.CategoryId,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Duration = request.Duration,
                IsActive = true
            };

            await _serviceRepository.AddAsync(service);
        }

        public async Task UpdateCategoryAsync(Guid shopId, string ownerId, Guid categoryId, UpdateServiceCategoryDto request)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || shop.OwnerId != ownerId)
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null || category.ShopId != shopId)
                throw new FluentValidation.ValidationException("Kategori bulunamadı.");

            category.Name = request.Name;
            category.Description = request.Description;
            category.IsActive = request.IsActive;

            await _categoryRepository.UpdateAsync(category);
        }

        public async Task DeleteCategoryAsync(Guid shopId, string ownerId, Guid categoryId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || shop.OwnerId != ownerId)
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null || category.ShopId != shopId)
                throw new FluentValidation.ValidationException("Kategori bulunamadı.");

            var hasActiveServices = await _context.ShopServices.AnyAsync(s => s.CategoryId == categoryId && !s.IsDeleted);
            if (hasActiveServices)
                throw new FluentValidation.ValidationException("Hizmetleri olan bir kategori silinemez. Önce hizmetleri silin veya taşıyın.");

            category.IsDeleted = true;
            category.IsActive = false;
            await _categoryRepository.UpdateAsync(category);
            _logger.LogInformation("Kategori silindi. KategoriId: {CategoryId}, Salon: {ShopId}", categoryId, shopId);
        }

        public async Task UpdateServiceAsync(Guid shopId, string ownerId, Guid serviceId, UpdateShopServiceDto request)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || shop.OwnerId != ownerId)
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var service = await _serviceRepository.GetByIdAsync(serviceId);
            if (service == null || service.ShopId != shopId)
                throw new FluentValidation.ValidationException("Hizmet bulunamadı.");

            service.Name = request.Name;
            service.Description = request.Description;
            service.Price = request.Price;
            service.Duration = request.Duration;
            service.IsActive = request.IsActive;

            await _serviceRepository.UpdateAsync(service);
        }

        public async Task DeleteServiceAsync(Guid shopId, string ownerId, Guid serviceId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || shop.OwnerId != ownerId)
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var service = await _serviceRepository.GetByIdAsync(serviceId);
            if (service == null || service.ShopId != shopId)
                throw new FluentValidation.ValidationException("Hizmet bulunamadı.");

            service.IsDeleted = true;
            service.IsActive = false;
            await _serviceRepository.UpdateAsync(service);

            var employeeAssignments = await _context.ShopEmployeeServices
                .Where(ses => ses.ShopServiceId == serviceId)
                .ToListAsync();

            if (employeeAssignments.Any())
            {
                _context.ShopEmployeeServices.RemoveRange(employeeAssignments);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Hizmet silindi. HizmetId: {ServiceId}, Salon: {ShopId}, Temizlenen atama: {Count}", serviceId, shopId, employeeAssignments.Count);
        }

        public async Task<List<ServiceCategoryDto>> GetShopServicesAsync(Guid shopId, string ownerId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || shop.OwnerId != ownerId) return new List<ServiceCategoryDto>();

            var categories = await _context.ServiceCategories
                .Where(c => c.ShopId == shopId)
                .ToListAsync();

            var services = await _context.ShopServices
                .Where(s => s.ShopId == shopId)
                .ToListAsync();

            var employeeServices = await _context.ShopEmployeeServices
                .Where(ses => ses.ShopEmployee.ShopId == shopId && !ses.ShopEmployee.IsDeleted)
                .Include(ses => ses.ShopEmployee)
                .ThenInclude(se => se.User)
                .ToListAsync();

            return categories.Select(c => new ServiceCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                IsDeleted = c.IsDeleted,
                Services = services
                    .Where(s => s.CategoryId == c.Id)
                    .Select(s => new ShopServiceDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Price = s.Price,
                        Duration = s.Duration,
                        IsActive = s.IsActive,
                        IsDeleted = s.IsDeleted,
                        Employees = employeeServices
                            .Where(es => es.ShopServiceId == s.Id)
                            .Select(es => new ServiceEmployeeDto
                            {
                                Id = es.ShopEmployee.Id,
                                FirstName = es.ShopEmployee.User.FirstName,
                                LastName = es.ShopEmployee.User.LastName,
                                Title = es.ShopEmployee.Title,
                                ImageUrl = es.ShopEmployee.User.ProfileImageUrl,
                                AverageRating = es.ShopEmployee.AverageRating,
                                ReviewCount = es.ShopEmployee.ReviewCount
                            }).ToList()
                    }).ToList()
            }).ToList();
        }

        public async Task<List<ServiceCategoryDto>> GetServicesByShopIdAsync(Guid shopId)
        {
            var categories = await _context.ServiceCategories
                .Where(c => c.ShopId == shopId && c.IsActive)
                .ToListAsync();

            var services = await _context.ShopServices
                .Where(s => s.ShopId == shopId && s.IsActive && !s.IsDeleted)
                .ToListAsync();

            var employeeServices = await _context.ShopEmployeeServices
                .Where(ses => ses.ShopEmployee.ShopId == shopId && !ses.ShopEmployee.IsDeleted)
                .Include(ses => ses.ShopEmployee)
                .ThenInclude(se => se.User)
                .ToListAsync();

            return categories.Select(c => new ServiceCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                IsDeleted = c.IsDeleted,
                Services = services
                    .Where(s => s.CategoryId == c.Id)
                    .Select(s => new ShopServiceDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Price = s.Price,
                        Duration = s.Duration,
                        IsActive = s.IsActive,
                        IsDeleted = s.IsDeleted,
                        Employees = employeeServices
                            .Where(es => es.ShopServiceId == s.Id)
                            .Select(es => new ServiceEmployeeDto
                            {
                                Id = es.ShopEmployee.Id,
                                FirstName = es.ShopEmployee.User.FirstName,
                                LastName = es.ShopEmployee.User.LastName,
                                Title = es.ShopEmployee.Title,
                                ImageUrl = es.ShopEmployee.User.ProfileImageUrl,
                                AverageRating = es.ShopEmployee.AverageRating,
                                ReviewCount = es.ShopEmployee.ReviewCount
                            }).ToList()
                    }).ToList()
            }).ToList();
        }
    }
}
