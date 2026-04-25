using KuaforumAPI.Domain.Common;
using KuaforumAPI.Domain.Enums;
using System;
using System.Collections.Generic;

namespace KuaforumAPI.Domain.Entities
{
    public class SalonOwnerApplication : BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string ShopName { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Neighborhood { get; set; } // Added
        public string Street { get; set; } // Added
        public string BuildingNumber { get; set; } // Added
        public string PhoneNumber { get; set; }
        public string ContactEmail { get; set; } // Added
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public virtual ICollection<SalonApplicationCategoryItem> Categories { get; set; } = new List<SalonApplicationCategoryItem>();
        public TargetGender GenderPreference { get; set; } = TargetGender.Unisex;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    }
}
