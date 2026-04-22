using KuaforumAPI.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KuaforumAPI.Domain.Entities
{
    public class ServiceCategory : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }
        public string Name { get; set; } // e.g., Hair, Beard, Skin Care
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }
}
