using FluentValidation;
using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class ShopService : IShopService
    {
        private readonly IShopRepository _shopRepository;
        private readonly IShopImageRepository _shopImageRepository;
        private readonly IValidator<CreateShopDto> _validator;

        public ShopService(IShopRepository shopRepository, IShopImageRepository shopImageRepository, IValidator<CreateShopDto> validator)
        {
            _shopRepository = shopRepository;
            _shopImageRepository = shopImageRepository;
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
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsActive = true
            };

            await _shopRepository.AddAsync(shop);
        }

        public async Task<ShopDto> GetShopByOwnerIdAsync(string userId)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(userId);
            if (shop == null) return null;
            
            var images = await _shopImageRepository.GetByShopIdAsync(shop.Id);

            return new ShopDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                Address = shop.Address,
                City = shop.City,
                District = shop.District,
                PhoneNumber = shop.PhoneNumber,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                IsActive = shop.IsActive,
                CoverImagePath = shop.CoverImagePath,
                ImageUrls = images.Select(i => i.Url).ToList(),
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
            shop.Latitude = request.Latitude;
            shop.Longitude = request.Longitude;
            
            await _shopRepository.UpdateAsync(shop);
        }

        public async Task<IEnumerable<ShopDto>> GetAllShopsAsync()
        {
            var shops = await _shopRepository.GetAllWithDetailsAsync();
            // Note: GetAllWithDetailsAsync should ideally include Images, but for now we might lazy load or separate queries.
            // Assuming GetAllWithDetailsAsync includes Owner.
            // If Images are not included in the repository method, we might need to fetch them.
            // But doing N+1 queries here is bad.
            // Let's assume for 'GetAll' (list view), we only need CoverImage which is on Shop entity.
            // We won't load gallery images for the list view to performance.

            return shops.Select(shop => new ShopDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                Address = shop.Address,
                City = shop.City,
                District = shop.District,
                PhoneNumber = shop.PhoneNumber,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                IsActive = shop.IsActive,
                CoverImagePath = shop.CoverImagePath,
                // ImageUrls = ... (Skip for list view)
                OwnerName = shop.Owner != null ? $"{shop.Owner.FirstName} {shop.Owner.LastName}" : "Unknown",
                OwnerEmail = shop.Owner?.Email,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            });
        }

        public async Task<ShopDto> GetShopByIdAsync(Guid id)
        {
            var shop = await _shopRepository.GetByIdAsync(id);
            if (shop == null) return null;

            var images = await _shopImageRepository.GetByShopIdAsync(shop.Id);

            return new ShopDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                Address = shop.Address,
                City = shop.City,
                District = shop.District,
                PhoneNumber = shop.PhoneNumber,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                IsActive = shop.IsActive,
                CoverImagePath = shop.CoverImagePath,
                ImageUrls = images.Select(i => i.Url).ToList(),
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            };
        }

        public async Task<string> UploadCoverImageAsync(Guid shopId, IFormFile file, string webRootPath)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null) throw new NotFoundException("Shop not found");

            // Delete old cover image if exists
            if (!string.IsNullOrEmpty(shop.CoverImagePath))
            {
                var oldPath = Path.Combine(webRootPath, shop.CoverImagePath.TrimStart('/'));
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                }
            }

            var uploadsFolder = Path.Combine(webRootPath, "uploads", "shops", "covers");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var relativePath = "/uploads/shops/covers/" + uniqueFileName;
            shop.CoverImagePath = relativePath;
            await _shopRepository.UpdateAsync(shop);

            return relativePath;
        }

        public async Task<IEnumerable<string>> UploadGalleryImagesAsync(Guid shopId, IFormFileCollection files, string webRootPath)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null) throw new NotFoundException("Shop not found");

            var uploadsFolder = Path.Combine(webRootPath, "uploads", "shops", "gallery");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uploadedUrls = new List<string>();

            foreach (var file in files)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var relativePath = "/uploads/shops/gallery/" + uniqueFileName;
                
                var shopImage = new ShopImage
                {
                    ShopId = shopId,
                    Url = relativePath
                };

                await _shopImageRepository.AddAsync(shopImage);
                uploadedUrls.Add(relativePath);
            }

            return uploadedUrls;
        }

        public async Task DeleteGalleryImageAsync(Guid imageId, string webRootPath)
        {
            var image = await _shopImageRepository.GetByIdAsync(imageId);
            if (image == null) throw new NotFoundException("Image not found");

            var filePath = Path.Combine(webRootPath, image.Url.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            await _shopImageRepository.DeleteAsync(image);
        }
    }
}
