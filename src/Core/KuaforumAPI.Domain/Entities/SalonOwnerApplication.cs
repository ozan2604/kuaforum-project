using KuaforumAPI.Domain.Common;
using KuaforumAPI.Domain.Enums;
using System;

namespace KuaforumAPI.Domain.Entities
{
    public class SalonOwnerApplication : BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string ShopName { get; set; }
        public string Description { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    }
}
