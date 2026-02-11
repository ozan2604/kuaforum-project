using KuaforumAPI.Domain.Common;
using System;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopImage : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }

        public string Url { get; set; }
    }
}
