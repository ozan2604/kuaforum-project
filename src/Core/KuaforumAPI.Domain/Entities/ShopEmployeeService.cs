using KuaforumAPI.Domain.Common;
using System;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopEmployeeService : BaseEntity
    {
        public Guid ShopEmployeeId { get; set; }
        public virtual ShopEmployee ShopEmployee { get; set; }

        public Guid ShopServiceId { get; set; }
        public virtual ShopService ShopService { get; set; }
    }
}
