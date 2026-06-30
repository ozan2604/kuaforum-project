using System;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopCustomerDto
    {
        public string? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}
