using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    public class UserFavoriteShop : BaseEntity
    {
        public string CircleUserId { get; set; }
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }
    }
}
