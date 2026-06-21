using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KuaforumAPI.Infrastructure.Services
{
    public class MediaLikeService : IMediaLikeService
    {
        private readonly ApplicationDbContext _context;

        public MediaLikeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ToggleLikeAsync(string userId, Guid mediaItemId, string mediaItemType)
        {
            var existing = await _context.MediaLikes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.MediaItemId == mediaItemId && l.MediaItemType == mediaItemType);

            if (existing != null)
            {
                _context.MediaLikes.Remove(existing);
                await _context.SaveChangesAsync();
                return false; // beğeni kaldırıldı
            }

            _context.MediaLikes.Add(new MediaLike
            {
                UserId = userId,
                MediaItemId = mediaItemId,
                MediaItemType = mediaItemType
            });
            await _context.SaveChangesAsync();
            return true; // beğenildi
        }

        public async Task<Dictionary<string, (int Count, bool IsLiked)>> GetLikeInfoBatchAsync(
            IEnumerable<(Guid Id, string Type)> items, string? userId)
        {
            var itemList = items.ToList();
            if (itemList.Count == 0) return new Dictionary<string, (int, bool)>();

            var ids = itemList.Select(x => x.Id).ToList();

            var counts = await _context.MediaLikes
                .Where(l => ids.Contains(l.MediaItemId))
                .GroupBy(l => new { l.MediaItemId, l.MediaItemType })
                .Select(g => new { g.Key.MediaItemId, g.Key.MediaItemType, Count = g.Count() })
                .ToListAsync();

            HashSet<string>? likedKeys = null;
            if (!string.IsNullOrEmpty(userId))
            {
                var liked = await _context.MediaLikes
                    .Where(l => l.UserId == userId && ids.Contains(l.MediaItemId))
                    .Select(l => new { l.MediaItemId, l.MediaItemType })
                    .ToListAsync();
                likedKeys = liked.Select(l => $"{l.MediaItemId}_{l.MediaItemType}").ToHashSet();
            }

            var result = new Dictionary<string, (int Count, bool IsLiked)>();
            foreach (var item in itemList)
            {
                var key = $"{item.Id}_{item.Type}";
                var countRow = counts.FirstOrDefault(c => c.MediaItemId == item.Id && c.MediaItemType == item.Type);
                result[key] = (
                    Count: countRow?.Count ?? 0,
                    IsLiked: likedKeys?.Contains(key) ?? false
                );
            }
            return result;
        }

        public async Task<List<MediaHighlightDto>> GetLikedByUserAsync(string userId)
        {
            var likes = await _context.MediaLikes
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var imageIds = likes.Where(l => l.MediaItemType == "image").Select(l => l.MediaItemId).ToList();
            var videoIds = likes.Where(l => l.MediaItemType == "video").Select(l => l.MediaItemId).ToList();

            var images = await _context.ShopImages
                .AsNoTracking()
                .Include(i => i.Shop)
                .Include(i => i.Tags)
                .Where(i => imageIds.Contains(i.Id))
                .ToListAsync();

            var videos = await _context.ShopVideos
                .AsNoTracking()
                .Include(v => v.Shop)
                .Where(v => videoIds.Contains(v.Id))
                .ToListAsync();

            // like count'ları tek sorguda çek
            var allIds = imageIds.Concat(videoIds).ToList();
            var countMap = await _context.MediaLikes
                .Where(l => allIds.Contains(l.MediaItemId))
                .GroupBy(l => l.MediaItemId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var result = new List<MediaHighlightDto>();

            foreach (var like in likes)
            {
                if (like.MediaItemType == "image")
                {
                    var img = images.FirstOrDefault(i => i.Id == like.MediaItemId);
                    if (img == null) continue;
                    result.Add(new MediaHighlightDto
                    {
                        Id = img.Id.ToString(),
                        Type = "image",
                        Url = img.Url,
                        ShopId = img.ShopId.ToString(),
                        ShopName = img.Shop?.Name ?? "",
                        Tags = img.Tags.Select(t => t.Name).ToList(),
                        LikeCount = countMap.GetValueOrDefault(img.Id, 0),
                        IsLikedByCurrentUser = true
                    });
                }
                else
                {
                    var vid = videos.FirstOrDefault(v => v.Id == like.MediaItemId);
                    if (vid == null) continue;
                    result.Add(new MediaHighlightDto
                    {
                        Id = vid.Id.ToString(),
                        Type = "video",
                        Url = vid.Url,
                        ShopId = vid.ShopId.ToString(),
                        ShopName = vid.Shop?.Name ?? "",
                        Tags = new List<string>(),
                        LikeCount = countMap.GetValueOrDefault(vid.Id, 0),
                        IsLikedByCurrentUser = true
                    });
                }
            }

            return result;
        }
    }
}
