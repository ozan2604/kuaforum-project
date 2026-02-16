using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KuaforumAPI.Infrastructure.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ApplicationDbContext _context;

        public FavoriteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ToggleFavoriteAsync(string userId, Guid shopId)
        {
            var existingFavorite = await _context.UserFavoriteShops
                .FirstOrDefaultAsync(f => f.CircleUserId == userId && f.ShopId == shopId);

            if (existingFavorite != null)
            {
                _context.UserFavoriteShops.Remove(existingFavorite);
            }
            else
            {
                var newFavorite = new UserFavoriteShop
                {
                    CircleUserId = userId,
                    ShopId = shopId
                };
                await _context.UserFavoriteShops.AddAsync(newFavorite);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsShopFavoritedAsync(string userId, Guid shopId)
        {
            return await _context.UserFavoriteShops
                .AnyAsync(f => f.CircleUserId == userId && f.ShopId == shopId);
        }

        public async Task<IEnumerable<ShopDto>> GetUserFavoritesAsync(string userId)
        {
            var favorites = await _context.UserFavoriteShops
                .Where(f => f.CircleUserId == userId)
                .Include(f => f.Shop)
                .ThenInclude(s => s.Images)
                .Select(f => f.Shop)
                .ToListAsync();

            return favorites.Select(shop => new ShopDto
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
                Images = shop.Images.Select(i => new ShopImageDto { Id = i.Id, Url = i.Url }).ToList(),
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            }).ToList();
        }
    }
}
