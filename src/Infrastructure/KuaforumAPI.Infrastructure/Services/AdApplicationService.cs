using KuaforumAPI.Application.DTOs.AdApplication;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class AdApplicationService : IAdApplicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;

        public AdApplicationService(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<AdApplicationDto> CreateAdApplicationAsync(string userId, CreateAdApplicationDto dto)
        {
            if (dto.Media == null || dto.Media.Length == 0)
                throw new Exception("Media file is required.");

            string mediaUrl = "";
            string mediaType = dto.Media.ContentType.StartsWith("video") ? "video" : "image";

            if (mediaType == "video")
            {
                mediaUrl = await _imageService.UploadVideoAsync(dto.Media, "ads/videos");
            }
            else
            {
                mediaUrl = await _imageService.UploadImageAsync(dto.Media, "ads/images");
            }

            var adApp = new AdApplication
            {
                UserId = userId,
                Description = dto.Description,
                PhoneNumber = dto.PhoneNumber,
                ExternalLink = dto.ExternalLink,
                Price = dto.Price,
                MediaUrl = mediaUrl,
                MediaType = mediaType,
                Status = ApplicationStatus.Pending
            };

            _context.AdApplications.Add(adApp);
            await _context.SaveChangesAsync();

            return MapToDto(adApp);
        }

        public async Task<IEnumerable<AdApplicationDto>> GetActiveAdsAsync()
        {
            var activeAds = await _context.AdApplications
                .Where(a => a.Status == ApplicationStatus.Approved && (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow))
                .OrderByDescending(a => a.ApprovedAt)
                .ToListAsync();

            return activeAds.Select(MapToDto);
        }

        public async Task<IEnumerable<AdApplicationDto>> GetAllAdApplicationsAsync()
        {
            var ads = await _context.AdApplications
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return ads.Select(MapToDto);
        }

        public async Task<IEnumerable<AdApplicationDto>> GetUserAdApplicationsAsync(string userId)
        {
            var ads = await _context.AdApplications
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return ads.Select(MapToDto);
        }

        public async Task<AdApplicationDto> UpdateAdApplicationStatusAsync(Guid id, UpdateAdApplicationStatusDto dto)
        {
            var adApp = await _context.AdApplications.FindAsync(id);
            if (adApp == null)
                throw new Exception("Ad application not found.");

            adApp.Status = dto.Status;
            
            if (dto.Status == ApplicationStatus.Approved)
            {
                adApp.ApprovedAt = DateTime.UtcNow;
                adApp.ExpiresAt = DateTime.UtcNow.AddMonths(1); // 1 month expiration
            }

            await _context.SaveChangesAsync();

            return MapToDto(adApp);
        }

        private static AdApplicationDto MapToDto(AdApplication entity)
        {
            return new AdApplicationDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                MediaUrl = entity.MediaUrl,
                MediaType = entity.MediaType,
                Description = entity.Description,
                PhoneNumber = entity.PhoneNumber,
                ExternalLink = entity.ExternalLink,
                Price = entity.Price,
                Status = entity.Status.ToString(),
                CreatedAt = entity.CreatedAt,
                ApprovedAt = entity.ApprovedAt,
                ExpiresAt = entity.ExpiresAt
            };
        }
    }
}
