using KuaforumAPI.Application.DTOs.Shop;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IMediaLikeService
    {
        /// <summary>Beğeni ekle/kaldır. true=beğenildi, false=kaldırıldı</summary>
        Task<bool> ToggleLikeAsync(string userId, Guid mediaItemId, string mediaItemType);

        /// <summary>Birden fazla item için likeCount ve isLiked bilgisini döner.</summary>
        Task<Dictionary<string, (int Count, bool IsLiked)>> GetLikeInfoBatchAsync(
            IEnumerable<(Guid Id, string Type)> items, string? userId);

        /// <summary>Kullanıcının beğendiği medya öğelerini döner (Favoriler sayfası için).</summary>
        Task<List<MediaHighlightDto>> GetLikedByUserAsync(string userId);
    }
}
