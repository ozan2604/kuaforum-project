using FluentValidation;
using KuaforumAPI.Application.DTOs.Service;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
   

namespace KuaforumAPI.Infrastructure.Services
{
    public class ServiceManagementService : IServiceManagementService
    {
        private readonly IShopRepository _shopRepository;
        private readonly IGenericRepository<ServiceCategory> _categoryRepository;
        private readonly IGenericRepository<Domain.Entities.ShopService> _serviceRepository;
        private readonly IValidator<CreateServiceCategoryDto> _categoryValidator;
        private readonly IValidator<CreateShopServiceDto> _serviceValidator;
        private readonly ApplicationDbContext _context; // Direct access for efficient querying (Include)

        public ServiceManagementService(
            IShopRepository shopRepository,
            IGenericRepository<ServiceCategory> categoryRepository,
            IGenericRepository<KuaforumAPI.Domain.Entities.ShopService> serviceRepository,
            IValidator<CreateServiceCategoryDto> categoryValidator,
            IValidator<CreateShopServiceDto> serviceValidator,
            ApplicationDbContext context)
        {
            _shopRepository = shopRepository;
            _categoryRepository = categoryRepository;
            _serviceRepository = serviceRepository;
            _categoryValidator = categoryValidator;
            _serviceValidator = serviceValidator;
            _context = context;
        }

        public async Task CreateCategoryAsync(string ownerId, CreateServiceCategoryDto request)
        {
            var validationResult = await _categoryValidator.ValidateAsync(request);
            if (!validationResult.IsValid) throw new FluentValidation.ValidationException(validationResult.Errors);

            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("You must have a shop to manage services.");

            var category = new ServiceCategory
            {
                ShopId = shop.Id,
                Name = request.Name,
                Description = request.Description
            };

            await _categoryRepository.AddAsync(category);
        }

        public async Task CreateServiceAsync(string ownerId, CreateShopServiceDto request)
        {
            var validationResult = await _serviceValidator.ValidateAsync(request);
            if (!validationResult.IsValid) throw new FluentValidation.ValidationException(validationResult.Errors);

            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("You must have a shop to manage services.");

            // Verify Category belongs to Shop
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null || category.ShopId != shop.Id)
            {
                throw new FluentValidation.ValidationException("Invalid category.");
            }

            var service = new KuaforumAPI.Domain.Entities.ShopService
            {
                ShopId = shop.Id,
                CategoryId = request.CategoryId,
                Name = request.Name,
                Price = request.Price,
                Duration = request.Duration,
                IsActive = true
            };

            await _serviceRepository.AddAsync(service);
        }

        public async Task<List<ServiceCategoryDto>> GetShopServicesAsync(string userId)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(userId);
            if (shop == null) return new List<ServiceCategoryDto>();

            // Eager load Categories with Services
            // Note: Since we use GenericRepo mostly, complex includes are better via Context directly or specialized repo query.
            // Here using Context for Efficiency.
            var categories = await _context.ServiceCategories
                .Where(c => c.ShopId == shop.Id)
                .Include(c => c.Shop) // Optional
                .ToListAsync();

            var services = await _context.ShopServices
                .Where(s => s.ShopId == shop.Id && s.IsActive)
                .ToListAsync();

            // Map to DTOs
            var result = categories.Select(c => new ServiceCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Services = services
                    .Where(s => s.CategoryId == c.Id)
                    .Select(s => new ShopServiceDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Price = s.Price,
                        Duration = s.Duration,
                        IsActive = s.IsActive
                    }).ToList()
            }).ToList();

            return result;
        }

        public async Task<List<ServiceCategoryDto>> GetServicesByShopIdAsync(Guid shopId)
        {
            // Similar logic but by ShopId directly
             var categories = await _context.ServiceCategories
                .Where(c => c.ShopId == shopId)
                .ToListAsync();

            var services = await _context.ShopServices
                .Where(s => s.ShopId == shopId && s.IsActive)
                .ToListAsync();

            // Map to DTOs
            var result = categories.Select(c => new ServiceCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Services = services
                    .Where(s => s.CategoryId == c.Id)
                    .Select(s => new ShopServiceDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Price = s.Price,
                        Duration = s.Duration,
                        IsActive = s.IsActive
                    }).ToList()
            }).ToList();

            return result;
        }
    }
}
