using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopVideoDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<ShopVideoTagDto> Tags { get; set; } = new List<ShopVideoTagDto>();
    }
}
