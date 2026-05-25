using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Service
{
    public class ServiceCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public List<ShopServiceDto> Services { get; set; } = new List<ShopServiceDto>();
    }
}
