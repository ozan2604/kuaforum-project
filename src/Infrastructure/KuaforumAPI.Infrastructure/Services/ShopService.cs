using FluentValidation;
using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using KuaforumAPI.Persistence.Contexts;
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
        private readonly IShopEmployeeRepository _shopEmployeeRepository;
        private readonly IImageService _imageService;
        private readonly IValidator<CreateShopDto> _validator;
        private readonly ApplicationDbContext _context;

        public ShopService(IShopRepository shopRepository, IShopImageRepository shopImageRepository, IShopEmployeeRepository shopEmployeeRepository, IImageService imageService, IValidator<CreateShopDto> validator, ApplicationDbContext context)
        {
            _shopRepository = shopRepository;
            _shopImageRepository = shopImageRepository;
            _shopEmployeeRepository = shopEmployeeRepository;
            _imageService = imageService;
            _validator = validator;
            _context = context;
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
                Neighborhood = request.Neighborhood,
                Street = request.Street,
                BuildingNumber = request.BuildingNumber,
                PhoneNumber = request.PhoneNumber,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Categories = request.CategoryIds.Select(id => new ShopCategoryAssignment { CategoryValue = id }).ToList(),
                GenderPreference = request.GenderPreference,
                OpenTime = ParseTime(request.OpenTime),
                CloseTime = ParseTime(request.CloseTime),
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
                Neighborhood = shop.Neighborhood,
                Street = shop.Street,
                BuildingNumber = shop.BuildingNumber,
                PhoneNumber = shop.PhoneNumber,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                Categories = shop.Categories.Select(c => c.CategoryValue).ToList(),
                GenderPreference = shop.GenderPreference,
                IsActive = shop.IsActive,
                IsAutoProcessEnabled = shop.IsAutoProcessEnabled,
                BookingDaysAhead = shop.BookingDaysAhead,
                OpenTime = FormatTime(shop.OpenTime),
                CloseTime = FormatTime(shop.CloseTime),
                ClosureDates = shop.ClosureDates.Select(c => new ShopClosureDateDto { Id = c.Id, ClosureDate = c.ClosureDate, Reason = c.Reason }).ToList(),
                CoverImagePath = shop.CoverImagePath,
                Images = images.Select(i => new ShopImageDto { Id = i.Id, Url = i.Url }).ToList(),
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
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
            shop.Neighborhood = request.Neighborhood;
            shop.Street = request.Street;
            shop.BuildingNumber = request.BuildingNumber;
            shop.PhoneNumber = request.PhoneNumber;
            shop.Latitude = request.Latitude;
            shop.Longitude = request.Longitude;
            shop.GenderPreference = request.GenderPreference;
            shop.OpenTime = ParseTime(request.OpenTime);
            shop.CloseTime = ParseTime(request.CloseTime);
            shop.BookingDaysAhead = request.BookingDaysAhead > 0 ? request.BookingDaysAhead : 30;

            await _shopRepository.UpdateAsync(shop);
            await _shopRepository.UpdateShopCategoriesAsync(shop.Id, request.CategoryIds);
        }

        public async Task<IEnumerable<ShopDto>> GetAllShopsAsync(string? city = null, string? district = null, string? neighborhood = null)
        {
            var shops = await _shopRepository.GetAllWithDetailsAsync(city, district, neighborhood);
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
                Neighborhood = shop.Neighborhood,
                Street = shop.Street,
                BuildingNumber = shop.BuildingNumber,
                PhoneNumber = shop.PhoneNumber,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                Categories = shop.Categories.Select(c => c.CategoryValue).ToList(),
                GenderPreference = shop.GenderPreference,
                IsActive = shop.IsActive,
                IsAutoProcessEnabled = shop.IsAutoProcessEnabled,
                BookingDaysAhead = shop.BookingDaysAhead,
                CoverImagePath = shop.CoverImagePath,
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
                OpenTime = FormatTime(shop.OpenTime),
                CloseTime = FormatTime(shop.CloseTime),
                OwnerName = shop.Owner != null ? $"{shop.Owner.FirstName} {shop.Owner.LastName}" : "Unknown",
                OwnerEmail = null,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            });
        }

        public async Task DeleteShopAsync(Guid id)
        {
            var shop = await _shopRepository.GetByIdAsync(id);
            if (shop == null)
            {
                throw new NotFoundException("Shop not found.");
            }

            var imagesToDelete = await _shopRepository.DeleteShopWithDependenciesAsync(id);

            foreach (var imageUrl in imagesToDelete)
            {
                try
                {
                    await _imageService.DeleteImageAsync(imageUrl);
                }
                catch (Exception ex)
                {
                    // Log exception but continue deleting other images/shop
                    Console.WriteLine($"Error deleting image {imageUrl}: {ex.Message}");
                }
            }
        }

        public async Task<ShopDto> GetShopByIdAsync(Guid id)
        {
            var shop = await _shopRepository.GetByIdAsync(id);
            if (shop == null) return null;

            var images = await _shopImageRepository.GetByShopIdAsync(shop.Id);
            
            // Calculate Weekly Schedule
            var employees = await _shopEmployeeRepository.GetByShopIdWithSchedulesAsync(shop.Id);
            var weeklySchedule = new List<ShopScheduleDto>();
            string saturdayClosingTime = "Closed";

            if (employees.Any())
            {
                var allSchedules = employees.SelectMany(e => e.Schedules).ToList();
                
                for (int i = 0; i < 7; i++)
                {
                    var day = (DayOfWeek)i;
                    var daySchedules = allSchedules.Where(s => s.DayOfWeek == day && s.IsWorking).ToList();

                    if (daySchedules.Any())
                    {
                        var minStart = daySchedules.Min(s => s.StartTime);
                        var maxEnd = daySchedules.Max(s => s.EndTime);

                        weeklySchedule.Add(new ShopScheduleDto
                        {
                            Day = day.ToString(),
                            DayOfWeek = i,
                            OpeningTime = minStart.ToString(@"hh\:mm"),
                            ClosingTime = maxEnd.ToString(@"hh\:mm"),
                            IsClosed = false
                        });

                        // Keep existing logic for backward compatibility or specific display if needed
                        if (day == DayOfWeek.Saturday)
                        {
                            saturdayClosingTime = maxEnd.ToString(@"hh\:mm");
                        }
                    }
                    else
                    {
                        weeklySchedule.Add(new ShopScheduleDto
                        {
                            Day = day.ToString(),
                            DayOfWeek = i,
                            OpeningTime = null,
                            ClosingTime = null,
                            IsClosed = true
                        });
                    }
                }
            }

            var closureDates = await _context.ShopClosureDates
                .Where(c => c.ShopId == shop.Id)
                .OrderBy(c => c.ClosureDate)
                .ToListAsync();

            return new ShopDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                Address = shop.Address,
                City = shop.City,
                District = shop.District,
                Neighborhood = shop.Neighborhood,
                Street = shop.Street,
                BuildingNumber = shop.BuildingNumber,
                PhoneNumber = shop.PhoneNumber,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                Categories = shop.Categories.Select(c => c.CategoryValue).ToList(),
                GenderPreference = shop.GenderPreference,
                IsActive = shop.IsActive,
                IsAutoProcessEnabled = shop.IsAutoProcessEnabled,
                BookingDaysAhead = shop.BookingDaysAhead,
                OpenTime = FormatTime(shop.OpenTime),
                CloseTime = FormatTime(shop.CloseTime),
                ClosureDates = closureDates.Select(c => new ShopClosureDateDto { Id = c.Id, ClosureDate = c.ClosureDate, Reason = c.Reason }).ToList(),
                CoverImagePath = shop.CoverImagePath,
                Images = images.Select(i => new ShopImageDto { Id = i.Id, Url = i.Url }).ToList(),
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
                SaturdayClosingTime = saturdayClosingTime,
                WeeklySchedule = weeklySchedule.OrderBy(s => s.DayOfWeek == 0 ? 7 : s.DayOfWeek).ToList(),
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            };
        }

        public async Task<string> UploadCoverImageAsync(Guid shopId, IFormFile file)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null) throw new NotFoundException("Shop not found");

            // Delete old cover image if exists
            if (!string.IsNullOrEmpty(shop.CoverImagePath))
            {
                await _imageService.DeleteImageAsync(shop.CoverImagePath);
            }

            var imageUrl = await _imageService.UploadImageAsync(file, "shops/covers", 1200, 400);
            
            shop.CoverImagePath = imageUrl;
            await _shopRepository.UpdateAsync(shop);

            return imageUrl;
        }

        public async Task<IEnumerable<string>> UploadGalleryImagesAsync(Guid shopId, IFormFileCollection files)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null) throw new NotFoundException("Shop not found");

            var uploadedUrls = new List<string>();

            foreach (var file in files)
            {
                var imageUrl = await _imageService.UploadImageAsync(file, "shops/gallery");
                if (imageUrl == null) continue;

                var shopImage = new ShopImage
                {
                    ShopId = shopId,
                    Url = imageUrl
                };

                await _shopImageRepository.AddAsync(shopImage);
                uploadedUrls.Add(imageUrl);
            }

            return uploadedUrls;
        }

        public async Task DeleteGalleryImageAsync(Guid imageId, string userId, bool isAdmin)
        {
            var image = await _shopImageRepository.GetByIdAsync(imageId);
            if (image == null) throw new NotFoundException("Image not found");

            if (!isAdmin)
            {
                var shop = await _shopRepository.GetByOwnerIdAsync(userId);
                if (shop == null || shop.Id != image.ShopId)
                    throw new UnauthorizedAccessException("Bu görseli silme yetkiniz yok.");
            }

            await _imageService.DeleteImageAsync(image.Url);
            await _shopImageRepository.DeleteAsync(image);
        }

        public async Task UpdateAutoProcessAsync(string ownerId, Guid shopId, bool isEnabled)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null || shop.Id != shopId) throw new FluentValidation.ValidationException("Shop not found or unauthorized.");

            shop.IsAutoProcessEnabled = isEnabled;
            await _shopRepository.UpdateAsync(shop);
        }

        public async Task<List<ShopClosureDateDto>> GetClosureDatesAsync(Guid shopId)
        {
            return await _context.ShopClosureDates
                .Where(c => c.ShopId == shopId)
                .OrderBy(c => c.ClosureDate)
                .Select(c => new ShopClosureDateDto { Id = c.Id, ClosureDate = c.ClosureDate, Reason = c.Reason })
                .ToListAsync();
        }

        public async Task AddClosureDateAsync(string ownerId, Guid shopId, DateTime date, string? reason)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null || shop.Id != shopId) throw new FluentValidation.ValidationException("Shop not found or unauthorized.");

            var alreadyExists = await _context.ShopClosureDates
                .AnyAsync(c => c.ShopId == shopId && c.ClosureDate.Date == date.Date);
            if (alreadyExists) throw new FluentValidation.ValidationException("Bu tarih zaten kapalı olarak işaretlenmiş.");

            _context.ShopClosureDates.Add(new ShopClosureDate
            {
                ShopId = shopId,
                ClosureDate = date.Date,
                Reason = reason
            });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveClosureDateAsync(string ownerId, Guid closureDateId)
        {
            var closure = await _context.ShopClosureDates
                .Include(c => c.Shop)
                .FirstOrDefaultAsync(c => c.Id == closureDateId);
            if (closure == null) throw new NotFoundException("Kapalı gün bulunamadı.");

            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null || shop.Id != closure.ShopId) throw new FluentValidation.ValidationException("Unauthorized.");

            _context.ShopClosureDates.Remove(closure);
            await _context.SaveChangesAsync();
        }

        private static TimeSpan? ParseTime(string? timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return null;
            if (TimeSpan.TryParseExact(timeStr, @"hh\:mm", null, out var ts)) return ts;
            if (TimeSpan.TryParse(timeStr, out var ts2)) return ts2;
            return null;
        }

        private static string? FormatTime(TimeSpan? ts) =>
            ts.HasValue ? ts.Value.ToString(@"hh\:mm") : null;
    }
}
