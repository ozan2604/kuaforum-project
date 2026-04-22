using KuaforumAPI.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopService : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }

        public Guid CategoryId { get; set; }
        public virtual ServiceCategory Category { get; set; }

        public string Name { get; set; } // e.g., Haircut, Fön
        public decimal Price { get; set; }
        public int Duration { get; set; } = 15; // In minutes, default 15
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }
}
