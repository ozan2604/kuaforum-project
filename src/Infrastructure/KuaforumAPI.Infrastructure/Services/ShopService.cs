using FluentValidation;
using KuaforumAPI.Application.DTOs.Common;
using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<ShopService> _logger;
        private readonly IShopCodeGeneratorService _codeGenerator;

        public ShopService(IShopRepository shopRepository, IShopImageRepository shopImageRepository, IShopEmployeeRepository shopEmployeeRepository, IImageService imageService, IValidator<CreateShopDto> validator, ApplicationDbContext context, IDateTimeService dateTimeService, ILogger<ShopService> logger, IShopCodeGeneratorService codeGenerator)
        {
            _shopRepository = shopRepository;
            _shopImageRepository = shopImageRepository;
            _shopEmployeeRepository = shopEmployeeRepository;
            _imageService = imageService;
            _validator = validator;
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
            _codeGenerator = codeGenerator;
        }



        public async Task CreateShopAsync(string userId, CreateShopDto request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                throw new FluentValidation.ValidationException(validationResult.Errors);

            var code = await _codeGenerator.GenerateAsync(request.City ?? "");

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
                Code = code,
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
                OwnerId = shop.OwnerId,
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
                CancellationHours = shop.CancellationHours,
                OpenTime = FormatTime(shop.OpenTime),
                CloseTime = FormatTime(shop.CloseTime),
                WeeklyOffDays = ParseWeeklyOffDays(shop.WeeklyOffDays),
                ClosureDates = shop.ClosureDates.Select(c => new ShopClosureDateDto { Id = c.Id, ClosureDate = c.ClosureDate, Reason = c.Reason }).ToList(),
                CoverImagePath = shop.CoverImagePath,
                PromoVideoUrl = null,
                Videos = (await _context.ShopVideos.Where(v => v.ShopId == shop.Id).OrderBy(v => v.DisplayOrder).ToListAsync())
                            .Select(v => new ShopVideoDto { Id = v.Id, Url = v.Url, DisplayOrder = v.DisplayOrder, CreatedAt = v.CreatedAt }).ToList(),
                Images = images.Select(i => new ShopImageDto { Id = i.Id, Url = i.Url, Tags = i.Tags.Select(t => new ShopImageTagDto { Id = t.Id, Name = t.Name }).ToList() }).ToList(),
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
                Code = shop.Code,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            };
        }

        public async Task<List<ShopDto>> GetMyShopsAsync(string userId)
        {
            var shops = await _shopRepository.GetAllByOwnerIdAsync(userId);

            var result = new List<ShopDto>();
            foreach (var shop in shops)
            {
                result.Add(new ShopDto
                {
                    Id = shop.Id,
                    OwnerId = shop.OwnerId,
                    Name = shop.Name,
                    Description = shop.Description,
                    Address = shop.Address,
                    City = shop.City,
                    District = shop.District,
                    Neighborhood = shop.Neighborhood,
                    PhoneNumber = shop.PhoneNumber,
                    Latitude = shop.Latitude,
                    Longitude = shop.Longitude,
                    Categories = shop.Categories.Select(c => c.CategoryValue).ToList(),
                    GenderPreference = shop.GenderPreference,
                    IsActive = shop.IsActive,
                    IsAutoProcessEnabled = shop.IsAutoProcessEnabled,
                    BookingDaysAhead = shop.BookingDaysAhead,
                    CancellationHours = shop.CancellationHours,
                    CoverImagePath = shop.CoverImagePath,
                    AverageRating = shop.AverageRating,
                    ReviewCount = shop.ReviewCount,
                    Code = shop.Code,
                    CreatedAt = shop.CreatedAt,
                    UpdatedAt = shop.UpdatedAt
                });
            }
            return result;
        }

        public async Task UpdateShopAsync(Guid shopId, string? userId, CreateShopDto request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                throw new FluentValidation.ValidationException(validationResult.Errors);

            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null) throw new NotFoundException("Salon bulunamadı.");
            if (userId != null && shop.OwnerId != userId) throw new UnauthorizedAccessException("Bu salonu düzenleme yetkiniz yok.");

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
            shop.CancellationHours = Math.Clamp(request.CancellationHours, 0, 72);
            shop.WeeklyOffDays = request.WeeklyOffDays != null && request.WeeklyOffDays.Any()
                ? string.Join(",", request.WeeklyOffDays.Distinct().OrderBy(d => d))
                : null;

            await _shopRepository.UpdateAsync(shop);
            await _shopRepository.UpdateShopCategoriesAsync(shop.Id, request.CategoryIds);
        }

        public async Task<IEnumerable<ShopDto>> GetAllShopsAsync(string? city = null, string? district = null, string? neighborhood = null)
        {
            var shops = (await _shopRepository.GetAllWithDetailsAsync(city, district, neighborhood)).ToList();

            var shopIds = shops.Select(s => s.Id).ToList();
            var minPrices = await _context.ShopServices
                .Where(ss => shopIds.Contains(ss.ShopId) && !ss.IsDeleted && ss.IsActive)
                .GroupBy(ss => ss.ShopId)
                .Select(g => new { ShopId = g.Key, MinPrice = g.Min(x => x.Price) })
                .ToDictionaryAsync(x => x.ShopId, x => x.MinPrice);

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
                CancellationHours = shop.CancellationHours,
                CoverImagePath = shop.CoverImagePath,
                PromoVideoUrl = null,
                Videos = new List<ShopVideoDto>(),
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
                MinServicePrice = minPrices.TryGetValue(shop.Id, out var mp) ? mp : null,
                OpenTime = FormatTime(shop.OpenTime),
                CloseTime = FormatTime(shop.CloseTime),
                OwnerName = shop.Owner != null ? $"{shop.Owner.FirstName} {shop.Owner.LastName}" : "Unknown",
                OwnerEmail = null,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            });
        }

        public async Task<PagedResult<ShopDto>> GetPublicShopsPagedAsync(string? city, string? district, string? neighborhood, int pageNumber, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var (shops, total) = await _shopRepository.GetPagedWithDetailsAsync(city, district, neighborhood, pageNumber, pageSize);

            var shopIds = shops.Select(s => s.Id).ToList();
            var minPrices = await _context.ShopServices
                .Where(ss => shopIds.Contains(ss.ShopId) && !ss.IsDeleted && ss.IsActive)
                .GroupBy(ss => ss.ShopId)
                .Select(g => new { ShopId = g.Key, MinPrice = g.Min(x => x.Price) })
                .ToDictionaryAsync(x => x.ShopId, x => x.MinPrice);

            var items = shops.Select(shop => new ShopDto
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
                CancellationHours = shop.CancellationHours,
                CoverImagePath = shop.CoverImagePath,
                PromoVideoUrl = null,
                Videos = new List<ShopVideoDto>(),
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
                MinServicePrice = minPrices.TryGetValue(shop.Id, out var mp) ? mp : null,
                OpenTime = FormatTime(shop.OpenTime),
                CloseTime = FormatTime(shop.CloseTime),
                OwnerName = shop.Owner != null ? $"{shop.Owner.FirstName} {shop.Owner.LastName}" : "Unknown",
                OwnerEmail = null,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            }).ToList();

            return new PagedResult<ShopDto>(items, total, pageNumber, pageSize);
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
                    _logger.LogWarning(ex, "Salon silinirken görsel silinemedi: {ImageUrl}", imageUrl);
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
                OwnerId = shop.OwnerId,
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
                CancellationHours = shop.CancellationHours,
                OpenTime = FormatTime(shop.OpenTime),
                CloseTime = FormatTime(shop.CloseTime),
                WeeklyOffDays = ParseWeeklyOffDays(shop.WeeklyOffDays),
                ClosureDates = closureDates.Select(c => new ShopClosureDateDto { Id = c.Id, ClosureDate = c.ClosureDate, Reason = c.Reason }).ToList(),
                CoverImagePath = shop.CoverImagePath,
                PromoVideoUrl = null,
                Videos = (await _context.ShopVideos.Where(v => v.ShopId == shop.Id).OrderBy(v => v.DisplayOrder).ToListAsync())
                            .Select(v => new ShopVideoDto { Id = v.Id, Url = v.Url, DisplayOrder = v.DisplayOrder, CreatedAt = v.CreatedAt }).ToList(),
                Images = images.Select(i => new ShopImageDto { Id = i.Id, Url = i.Url, Tags = i.Tags.Select(t => new ShopImageTagDto { Id = t.Id, Name = t.Name }).ToList() }).ToList(),
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
                Code = shop.Code,
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

        public async Task DeleteCoverImageAsync(Guid shopId, string userId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null) throw new NotFoundException("Salon bulunamadı.");
            if (shop.OwnerId != userId) throw new UnauthorizedAccessException("Bu salona erişim yetkiniz yok.");

            if (!string.IsNullOrEmpty(shop.CoverImagePath))
            {
                await _imageService.DeleteImageAsync(shop.CoverImagePath);
                shop.CoverImagePath = string.Empty;
                await _shopRepository.UpdateAsync(shop);
            }
        }



        public async Task<ShopVideoDto> UploadShopVideoAsync(Guid shopId, string userId, IFormFile file)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null) throw new NotFoundException("Salon bulunamadı.");
            if (shop.OwnerId != userId) throw new UnauthorizedAccessException("Bu salona erişim yetkiniz yok.");

            if (file.Length > 150L * 1024 * 1024)
            {
                throw new FluentValidation.ValidationException("Video boyutu maksimum 150MB olabilir.");
            }

            var existingCount = await _context.ShopVideos.CountAsync(v => v.ShopId == shopId);
            if (existingCount >= 1)
            {
                throw new FluentValidation.ValidationException("Şimdilik en fazla 1 adet tanıtım videosu eklenebilir.");
            }

            var videoUrl = await _imageService.UploadVideoAsync(file, "shops/videos");

            var shopVideo = new ShopVideo
            {
                ShopId = shopId,
                Url = videoUrl,
                DisplayOrder = existingCount
            };

            _context.ShopVideos.Add(shopVideo);
            await _context.SaveChangesAsync();

            return new ShopVideoDto { Id = shopVideo.Id, Url = shopVideo.Url, DisplayOrder = shopVideo.DisplayOrder, CreatedAt = shopVideo.CreatedAt };
        }

        public async Task DeleteShopVideoAsync(Guid videoId, string userId)
        {
            var video = await _context.ShopVideos.Include(v => v.Shop).FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null) throw new NotFoundException("Video bulunamadı.");
            if (video.Shop.OwnerId != userId) throw new UnauthorizedAccessException("Bu videoyu silme yetkiniz yok.");

            await _imageService.DeleteVideoAsync(video.Url);
            _context.ShopVideos.Remove(video);
            await _context.SaveChangesAsync();
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
                var shop = await _shopRepository.GetByIdAsync(image.ShopId);
                if (shop == null || shop.OwnerId != userId)
                    throw new UnauthorizedAccessException("Bu görseli silme yetkiniz yok.");
            }

            await _imageService.DeleteImageAsync(image.Url);
            await _shopImageRepository.DeleteAsync(image);
        }

        public async Task UpdateAutoProcessAsync(string? ownerId, Guid shopId, bool isEnabled)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId)) throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

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

        public async Task AddClosureDateAsync(string? ownerId, Guid shopId, DateTime date, string? reason)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId)) throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            if (date.Date < _dateTimeService.Now.Date)
                throw new FluentValidation.ValidationException("Geçmiş bir tarihe kapalı gün eklenemez.");

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

        public async Task RemoveClosureDateAsync(string? ownerId, Guid closureDateId)
        {
            var closure = await _context.ShopClosureDates
                .Include(c => c.Shop)
                .FirstOrDefaultAsync(c => c.Id == closureDateId);
            if (closure == null) throw new NotFoundException("Kapalı gün bulunamadı.");
            if (ownerId != null && closure.Shop.OwnerId != ownerId) throw new FluentValidation.ValidationException("Bu kapalı günü silme yetkiniz yok.");

            _context.ShopClosureDates.Remove(closure);
            await _context.SaveChangesAsync();
        }

        public async Task<ShopDashboardStatsDto> GetDashboardStatsAsync(Guid shopId, string? ownerId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null) throw new NotFoundException("Salon bulunamadı.");
            if (ownerId != null && shop.OwnerId != ownerId) throw new UnauthorizedAccessException("Bu salona erişim yetkiniz yok.");

            var now = _dateTimeService.Now;
            var today = now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-7);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var appointments = await _context.Appointments
                .Where(a => a.ShopId == shop.Id && a.StartTime >= startOfYear)
                .Select(a => new AppointmentSlim { StartTime = a.StartTime, Status = a.Status, Price = a.ShopService != null ? a.ShopService.Price : 0 })
                .ToListAsync();

            var serviceIsActiveList = await _context.ShopServices
                .Where(s => s.ShopId == shop.Id && !s.IsDeleted)
                .Select(s => s.IsActive)
                .ToListAsync();

            var employeeProjections = await _context.ShopEmployees
                .Where(e => e.ShopId == shop.Id && !e.IsDeleted)
                .Select(e => new { e.Id, e.IsActive })
                .ToListAsync();

            static AppointmentPeriodStats Summarize(List<AppointmentSlim> apps) => new()
            {
                Total = apps.Count,
                Completed = apps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed),
                Cancelled = apps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Cancelled),
                Rejected = apps.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Rejected),
                Revenue = apps.Where(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Completed).Sum(a => a.Price)
            };

            var activeEmployeeIds = employeeProjections.Where(e => e.IsActive).Select(e => e.Id).ToList();
            var activeServices = serviceIsActiveList.Count(x => x);
            var activeEmployees = activeEmployeeIds.Count;
            var unconfirmedApps = appointments.Count(a => a.Status == KuaforumAPI.Domain.Enums.AppointmentStatus.Pending);

            // Aktif çalışanlardan en az birinin hizmet ataması var mı?
            var hasEmployeeServices = activeEmployeeIds.Any() && await _context.ShopEmployeeServices
                .AnyAsync(ses => activeEmployeeIds.Contains(ses.ShopEmployeeId));

            // Aktif çalışanlardan en az birinin çalışma saati ayarlanmış mı? (IsWorking=true olan en az 1 gün)
            var hasEmployeeSchedules = activeEmployeeIds.Any() && await _context.EmployeeSchedules
                .AnyAsync(es => activeEmployeeIds.Contains(es.ShopEmployeeId) && es.IsWorking);

            var hasName = !string.IsNullOrWhiteSpace(shop.Name);
            var hasDescription = !string.IsNullOrWhiteSpace(shop.Description);
            var hasCoverImage = !string.IsNullOrWhiteSpace(shop.CoverImagePath);
            var hasCategories = shop.Categories != null && shop.Categories.Any();
            var hasLocation = !string.IsNullOrWhiteSpace(shop.City) && !string.IsNullOrWhiteSpace(shop.District) && !string.IsNullOrWhiteSpace(shop.Address);
            var hasOpeningHours = shop.OpenTime.HasValue && shop.CloseTime.HasValue;
            var hasActiveServices = activeServices > 0;
            var hasActiveEmployees = activeEmployees > 0;

            var setupSteps = new[] { hasName, hasDescription, hasCoverImage, hasCategories, hasLocation, hasOpeningHours, hasActiveServices, hasActiveEmployees, hasEmployeeServices, hasEmployeeSchedules };
            var completionPercentage = (int)Math.Round((double)setupSteps.Count(s => s) / setupSteps.Length * 100);

            var notificationItems = new List<NotificationItemDto>();

            if (unconfirmedApps > 0)
                notificationItems.Add(new NotificationItemDto { Type = "action", Message = $"{unconfirmedApps} randevu onayınızı bekliyor.", Link = "/salon-panel/appointments" });

            if (!hasDescription)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Salon açıklaması eksik.", Link = "/salon-panel/shop" });
            if (!hasCoverImage)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Kapak fotoğrafı eklenmemiş.", Link = "/salon-panel/shop" });
            if (!hasCategories)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Salon kategorisi seçilmemiş.", Link = "/salon-panel/shop" });
            if (!hasLocation)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Konum bilgisi eksik.", Link = "/salon-panel/shop" });
            if (!hasOpeningHours)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Çalışma saatleri girilmemiş.", Link = "/salon-panel/shop" });
            if (!hasActiveServices)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Henüz aktif hizmet bulunmuyor.", Link = "/salon-panel/services" });
            if (!hasActiveEmployees)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Henüz aktif çalışan bulunmuyor.", Link = "/salon-panel/employees" });
            if (hasActiveEmployees && !hasEmployeeServices)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Çalışanlara henüz hizmet atanmamış.", Link = "/salon-panel/employees" });
            if (hasActiveEmployees && !hasEmployeeSchedules)
                notificationItems.Add(new NotificationItemDto { Type = "setup", Message = "Çalışanların çalışma saatleri ayarlanmamış.", Link = "/salon-panel/employees" });

            // Eski format (geriye dönük uyumluluk)
            var notifications = new List<string>();
            if (unconfirmedApps > 0) notifications.Add($"{unconfirmedApps} adet onay/yanıt bekleyen randevunuz var.");
            var missingInfo = new List<string>();
            if (!hasDescription) missingInfo.Add("Açıklama");
            if (!hasCoverImage) missingInfo.Add("Kapak Fotoğrafı");
            if (!hasCategories) missingInfo.Add("Kategori");
            if (missingInfo.Any()) notifications.Add($"Dükkan profilinizde eksikler var: {string.Join(", ", missingInfo)}.");

            return new ShopDashboardStatsDto
            {
                ShopId = shop.Id,
                Notifications = notifications,
                NotificationItems = notificationItems,
                SetupStatus = new SetupStatusDto
                {
                    HasName = hasName,
                    HasDescription = hasDescription,
                    HasCoverImage = hasCoverImage,
                    HasCategories = hasCategories,
                    HasLocation = hasLocation,
                    HasOpeningHours = hasOpeningHours,
                    HasActiveServices = hasActiveServices,
                    HasActiveEmployees = hasActiveEmployees,
                    HasEmployeeServices = hasEmployeeServices,
                    HasEmployeeSchedules = hasEmployeeSchedules,
                    CompletionPercentage = completionPercentage
                },
                Appointments = new AppointmentStats
                {
                    Today = Summarize(appointments.Where(a => a.StartTime.Date == today).ToList()),
                    ThisWeek = Summarize(appointments.Where(a => a.StartTime.Date >= startOfWeek).ToList()),
                    ThisMonth = Summarize(appointments.Where(a => a.StartTime.Date >= startOfMonth).ToList()),
                    ThisYear = Summarize(appointments.Where(a => a.StartTime.Date >= startOfYear).ToList()),
                },
                Services = new ServiceStats
                {
                    Total = serviceIsActiveList.Count,
                    Active = activeServices,
                    Passive = serviceIsActiveList.Count - activeServices
                },
                Employees = new EmployeeStats
                {
                    Total = employeeProjections.Count,
                    Active = activeEmployees,
                    Passive = employeeProjections.Count - activeEmployees
                }
            };
        }

        private class AppointmentSlim
        {
            public DateTime StartTime { get; set; }
            public KuaforumAPI.Domain.Enums.AppointmentStatus Status { get; set; }
            public decimal Price { get; set; }
        }

        public async Task<(int TotalCount, IEnumerable<ShopDto> Shops)> GetAllShopsAdminAsync(string? search, int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var query = _context.Shops.Include(s => s.Owner).Include(s => s.Categories).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(lowerSearch) ||
                    (s.Owner != null && (s.Owner.FirstName.ToLower().Contains(lowerSearch) || s.Owner.LastName.ToLower().Contains(lowerSearch) || s.Owner.Email.ToLower().Contains(lowerSearch))) ||
                    (s.City != null && s.City.ToLower().Contains(lowerSearch)) ||
                    (s.PhoneNumber != null && s.PhoneNumber.Contains(search))
                );
            }

            var totalCount = await query.CountAsync();
            var pagedShops = await query.OrderByDescending(s => s.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var shops = pagedShops.Select(shop => new ShopDto
            {
                Id = shop.Id,
                OwnerId = shop.OwnerId,
                Name = shop.Name,
                Description = shop.Description,
                Address = shop.Address,
                City = shop.City,
                District = shop.District,
                PhoneNumber = shop.PhoneNumber,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                Categories = shop.Categories.Select(c => c.CategoryValue).ToList(),
                GenderPreference = shop.GenderPreference,
                IsActive = shop.IsActive,
                IsAutoProcessEnabled = shop.IsAutoProcessEnabled,
                BookingDaysAhead = shop.BookingDaysAhead,
                CancellationHours = shop.CancellationHours,
                CoverImagePath = shop.CoverImagePath,
                PromoVideoUrl = null,
                AverageRating = shop.AverageRating,
                ReviewCount = shop.ReviewCount,
                OwnerName = shop.Owner != null ? $"{shop.Owner.FirstName} {shop.Owner.LastName}" : "Unknown",
                OwnerEmail = shop.Owner != null ? shop.Owner.Email : null,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            });

            return (totalCount, shops);
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

        private static List<int> ParseWeeklyOffDays(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? new List<int>()
                : raw.Split(',').Select(int.Parse).ToList();

        public async Task<ShopImageTagDto> AddImageTagAsync(string ownerId, Guid imageId, string name)
        {
            var image = await _context.ShopImages.Include(i => i.Shop).FirstOrDefaultAsync(i => i.Id == imageId);
            if (image == null) throw new NotFoundException("Fotoğraf bulunamadı.");
            if (image.Shop.OwnerId != ownerId)
                throw new UnauthorizedAccessException("Bu fotoğrafa etiket ekleme yetkiniz yok.");

            var tag = new ShopImageTag { ShopImageId = imageId, Name = name.Trim() };
            _context.ShopImageTags.Add(tag);
            await _context.SaveChangesAsync();

            return new ShopImageTagDto { Id = tag.Id, Name = tag.Name };
        }

        public async Task UpdateImageTagAsync(string ownerId, Guid tagId, string name)
        {
            var tag = await _context.ShopImageTags
                .Include(t => t.ShopImage).ThenInclude(i => i.Shop)
                .FirstOrDefaultAsync(t => t.Id == tagId);
            if (tag == null) throw new NotFoundException("Etiket bulunamadı.");
            if (tag.ShopImage.Shop.OwnerId != ownerId)
                throw new UnauthorizedAccessException("Bu etiketi düzenleme yetkiniz yok.");

            tag.Name = name.Trim();
            await _context.SaveChangesAsync();
        }

        public async Task DeleteImageTagAsync(string ownerId, Guid tagId)
        {
            var tag = await _context.ShopImageTags
                .Include(t => t.ShopImage).ThenInclude(i => i.Shop)
                .FirstOrDefaultAsync(t => t.Id == tagId);
            if (tag == null) throw new NotFoundException("Etiket bulunamadı.");
            if (tag.ShopImage.Shop.OwnerId != ownerId)
                throw new UnauthorizedAccessException("Bu etiketi silme yetkiniz yok.");

            _context.ShopImageTags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }
}
