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
        Task<IEnumerable<ShopDto>> GetAllShopsAsync();
        Task<ShopDto> GetShopByIdAsync(Guid id);
        
        Task<string> UploadCoverImageAsync(Guid shopId, IFormFile file);
        Task<IEnumerable<string>> UploadGalleryImagesAsync(Guid shopId, IFormFileCollection files);
        Task DeleteGalleryImageAsync(Guid imageId);
    }
}
