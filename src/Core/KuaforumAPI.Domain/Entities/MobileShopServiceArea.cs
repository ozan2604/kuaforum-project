using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    public class MobileShopServiceArea : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }

        public string City { get; set; }
        public string District { get; set; }
        public string? Neighborhood { get; set; }
    }
}
