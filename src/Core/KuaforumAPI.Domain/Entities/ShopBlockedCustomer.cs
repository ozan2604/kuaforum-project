using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopBlockedCustomer : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }

        public string CustomerId { get; set; }
        public virtual ApplicationUser Customer { get; set; }

        public string? Reason { get; set; }
        public string BlockedByUserId { get; set; }
    }
}
