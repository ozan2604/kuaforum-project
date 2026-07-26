using KuaforumAPI.Domain.Common;
using KuaforumAPI.Domain.Enums;
using System;

namespace KuaforumAPI.Domain.Entities
{
    public class AdApplication : BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string MediaUrl { get; set; }
        public string MediaType { get; set; } // "image" or "video"
        public string Description { get; set; }
        public string PhoneNumber { get; set; }
        public string ExternalLink { get; set; } // Optional PR/Sales link
        public decimal? Price { get; set; } // Optional Product price
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
