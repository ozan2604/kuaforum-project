using KuaforumAPI.Application.DTOs.Shop;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IShopService
    {
        Task CreateShopAsync(string userId, CreateShopDto request);
        Task<ShopDto> GetShopByOwnerIdAsync(string userId);
        Task UpdateShopAsync(string userId, CreateShopDto request);
        Task<IEnumerable<ShopDto>> GetAllShopsAsync(string? city = null, string? district = null, string? neighborhood = null);
        Task DeleteShopAsync(Guid id);
        Task<ShopDto> GetShopByIdAsync(Guid id);
        
        Task<string> UploadCoverImageAsync(Guid shopId, IFormFile file);
        Task<IEnumerable<string>> UploadGalleryImagesAsync(Guid shopId, IFormFileCollection files);
        Task DeleteGalleryImageAsync(Guid imageId);
        Task UpdateAutoProcessAsync(string ownerId, Guid shopId, bool isEnabled);
    }
}
