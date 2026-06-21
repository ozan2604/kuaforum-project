namespace KuaforumAPI.Application.DTOs.Shop
{
    public class MediaHighlightDto
    {
        public string Id { get; set; }
        public string Type { get; set; } // "image" or "video"
        public string Url { get; set; }
        public string ShopId { get; set; }
        public string ShopName { get; set; }
        public List<string> Tags { get; set; } = new();
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}
