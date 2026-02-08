using FluentValidation;
using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class ShopService : IShopService
    {
        private readonly IShopRepository _shopRepository;
        private readonly IValidator<CreateShopDto> _validator;

        public ShopService(IShopRepository shopRepository, IValidator<CreateShopDto> validator)
        {
            _shopRepository = shopRepository;
            _validator = validator;
        }

        public async Task CreateShopAsync(string userId, CreateShopDto request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new FluentValidation.ValidationException(validationResult.Errors);
            }

            var existingShop = await _shopRepository.GetByOwnerIdAsync(userId);
            if (existingShop != null)
            {
                throw new FluentValidation.ValidationException("User already has a shop.");
            }

            var shop = new Shop
            {
                OwnerId = userId,
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                City = request.City,
                District = request.District,
                PhoneNumber = request.PhoneNumber,
                IsActive = true
            };

            await _shopRepository.AddAsync(shop);
        }

        public async Task<ShopDto> GetShopByOwnerIdAsync(string userId)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(userId);
            if (shop == null) return null;

            return new ShopDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                Address = shop.Address,
                City = shop.City,
                District = shop.District,
                PhoneNumber = shop.PhoneNumber,
                IsActive = shop.IsActive,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            };
        }

        public async Task UpdateShopAsync(string userId, CreateShopDto request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new FluentValidation.ValidationException(validationResult.Errors);
            }

            var shop = await _shopRepository.GetByOwnerIdAsync(userId);
            if (shop == null)
            {
                throw new NotFoundException("Shop not found.");
            }

            shop.Name = request.Name;
            shop.Description = request.Description;
            shop.Address = request.Address;
            shop.City = request.City;
            shop.District = request.District;
            shop.PhoneNumber = request.PhoneNumber;
            
            await _shopRepository.UpdateAsync(shop);
        }
    }
}
