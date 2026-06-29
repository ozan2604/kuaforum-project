using KuaforumAPI.Application.DTOs.Common;
using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IShopService
    {
        Task CreateShopAsync(string userId, CreateShopDto request);
        // Backward-compat: sahibin ilk salonunu döndürür
        Task<ShopDto> GetShopByOwnerIdAsync(string userId);
        // Sahibin tüm salonlarını döndürür
        Task<List<ShopDto>> GetMyShopsAsync(string userId);
        Task UpdateShopAsync(Guid shopId, string? userId, CreateShopDto request);
        Task<IEnumerable<ShopDto>> GetAllShopsAsync(string? city = null, string? district = null, string? neighborhood = null);
        Task<PagedResult<ShopDto>> GetPublicShopsPagedAsync(string? city, string? district, string? neighborhood, int pageNumber, int pageSize, ShopType? shopType = null);
        Task DeleteShopAsync(Guid id);
        Task<ShopDto> GetShopByIdAsync(Guid id, string? userId = null);

        Task<string> UploadCoverImageAsync(Guid shopId, IFormFile file);
        Task DeleteCoverImageAsync(Guid shopId, string userId);

        Task<ShopVideoDto> UploadShopVideoAsync(Guid shopId, string userId, IFormFile file, bool isAdmin = false);
        Task DeleteShopVideoAsync(Guid videoId, string userId, bool isAdmin = false);
        Task<int> RecordVideoViewAsync(Guid videoId);
        
        Task<ShopVideoTagDto> AddVideoTagAsync(string ownerId, Guid videoId, string name, bool isAdmin = false);
        Task UpdateVideoTagAsync(string ownerId, Guid tagId, string name, bool isAdmin = false);
        Task DeleteVideoTagAsync(string ownerId, Guid tagId, bool isAdmin = false);

        Task<IEnumerable<string>> UploadGalleryImagesAsync(Guid shopId, IFormFileCollection files);
        Task DeleteGalleryImageAsync(Guid imageId, string userId, bool isAdmin);
        Task UpdateAutoProcessAsync(string? ownerId, Guid shopId, bool isEnabled);

        Task<List<ShopClosureDateDto>> GetClosureDatesAsync(Guid shopId);
        Task AddClosureDateAsync(string? ownerId, Guid shopId, DateTime date, string? reason);
        Task RemoveClosureDateAsync(string? ownerId, Guid closureDateId);

        Task<ShopDashboardStatsDto> GetDashboardStatsAsync(Guid shopId, string? ownerId);
        Task<(int TotalCount, IEnumerable<ShopDto> Shops)> GetAllShopsAdminAsync(string? search, int page, int pageSize);

        Task<ShopImageTagDto> AddImageTagAsync(string ownerId, Guid imageId, string name);
        Task UpdateImageTagAsync(string ownerId, Guid tagId, string name);
        Task DeleteImageTagAsync(string ownerId, Guid tagId);

        Task<List<MediaHighlightDto>> GetMediaHighlightsAsync(string? city, string? district, string? neighborhood, int limit = 40, string? userId = null);
    }
}
