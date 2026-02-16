using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KuaforumAPI.Application.DTOs.Shop;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IFavoriteService
    {
        Task ToggleFavoriteAsync(string userId, Guid shopId);
        Task<bool> IsShopFavoritedAsync(string userId, Guid shopId);
        Task<IEnumerable<ShopDto>> GetUserFavoritesAsync(string userId);
    }
}
