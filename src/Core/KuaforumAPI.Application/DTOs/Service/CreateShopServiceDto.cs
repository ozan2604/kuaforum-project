using System;

namespace KuaforumAPI.Application.DTOs.Service
{
    public class CreateShopServiceDto
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; } // Minutes
    }
}
