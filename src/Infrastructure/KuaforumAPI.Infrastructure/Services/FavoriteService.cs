using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KuaforumAPI.Infrastructure.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(ApplicationDbContext context, ILogger<FavoriteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ToggleFavoriteAsync(string userId, Guid shopId)
        {
            var existingFavorite = await _context.UserFavoriteShops
                .FirstOrDefaultAsync(f => f.CircleUserId == userId && f.ShopId == shopId);

            if (existingFavorite != null)
            {
                _context.UserFavoriteShops.Remove(existingFavorite);
                _logger.LogInformation("Favoriden çıkarıldı. Kullanıcı: {UserId}, Salon: {ShopId}", userId, shopId);
            }
            else
            {
                var newFavorite = new UserFavoriteShop
                {
                    CircleUserId = userId,
                    ShopId = shopId
                };
                await _context.UserFavoriteShops.AddAsync(newFavorite);
                _logger.LogInformation("Favoriye eklendi. Kullanıcı: {UserId}, Salon: {ShopId}", userId, shopId);
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
                .AsNoTracking()
                .Where(f => f.CircleUserId == userId && f.Shop.IsActive)
                .Include(f => f.Shop)
                    .ThenInclude(s => s.Images)
                .Include(f => f.Shop)
                    .ThenInclude(s => s.Categories)
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
                Images = shop.Images?.Select(i => new ShopImageDto { Id = i.Id, Url = i.Url }).ToList() ?? new(),
                Categories = shop.Categories?.Select(c => c.CategoryValue).ToList() ?? new(),
                AverageRating = shop.AverageRating,
                OpenTime = shop.OpenTime?.ToString(@"hh\:mm"),
                CloseTime = shop.CloseTime?.ToString(@"hh\:mm"),
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            }).ToList();
        }
    }
}
