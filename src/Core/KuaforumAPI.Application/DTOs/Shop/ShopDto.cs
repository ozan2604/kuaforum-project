using System;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string PhoneNumber { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        public string CoverImagePath { get; set; }
        public System.Collections.Generic.List<string> ImageUrls { get; set; }

        public bool IsActive { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
