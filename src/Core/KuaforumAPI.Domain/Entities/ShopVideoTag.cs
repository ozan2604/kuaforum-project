using KuaforumAPI.Domain.Common;
using System;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopVideoTag : BaseEntity
    {
        public Guid ShopVideoId { get; set; }
        public virtual ShopVideo ShopVideo { get; set; }

        public string Name { get; set; }
    }
}
