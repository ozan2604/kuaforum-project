using KuaforumAPI.Domain.Common;
using KuaforumAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KuaforumAPI.Domain.Entities
{
    public class Shop : BaseEntity
    {
        public string OwnerId { get; set; }

        public virtual ApplicationUser Owner { get; set; }

        public string Name { get; set; }

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

        public string CoverImagePath { get; set; }
        public virtual ICollection<ShopImage> Images { get; set; }

        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public virtual ICollection<ShopCategoryAssignment> Categories { get; set; } = new List<ShopCategoryAssignment>();

        public TargetGender GenderPreference { get; set; } = TargetGender.Unisex;

        public bool IsActive { get; set; } = true;
        
        public bool IsAutoProcessEnabled { get; set; } = false;
    }
}
