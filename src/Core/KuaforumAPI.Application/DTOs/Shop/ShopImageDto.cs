using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopImageDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public List<ShopImageTagDto> Tags { get; set; } = new List<ShopImageTagDto>();
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}
