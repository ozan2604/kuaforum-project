using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopClosureDate : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }

        public DateTime ClosureDate { get; set; }

        public string? Reason { get; set; }
    }
}
