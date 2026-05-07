using KuaforumAPI.Domain.Common;
using System;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopImageTag : BaseEntity
    {
        public Guid ShopImageId { get; set; }
        public virtual ShopImage ShopImage { get; set; }

        public string Name { get; set; }
    }
}
