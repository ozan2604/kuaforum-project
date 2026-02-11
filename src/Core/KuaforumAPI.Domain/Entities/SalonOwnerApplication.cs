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
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string PhoneNumber { get; set; }
        public string TaxNumber { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    }
}
