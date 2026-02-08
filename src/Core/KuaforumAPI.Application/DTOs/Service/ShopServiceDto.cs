using System;

namespace KuaforumAPI.Application.DTOs.Service
{
    public class ShopServiceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public bool IsActive { get; set; }
    }
}
