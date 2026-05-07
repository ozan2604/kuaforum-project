using KuaforumAPI.Domain.Common;
using System;
using System.Collections.Generic;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopImage : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }

        public string Url { get; set; }

        public virtual ICollection<ShopImageTag> Tags { get; set; } = new List<ShopImageTag>();
    }
}
