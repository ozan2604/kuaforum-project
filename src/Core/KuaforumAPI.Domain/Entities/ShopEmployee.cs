using KuaforumAPI.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KuaforumAPI.Domain.Entities
{
    public class ShopEmployee : BaseEntity
    {
        [Required]
        public Guid ShopId { get; set; }

        [ForeignKey("ShopId")]
        public virtual Shop Shop { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } // e.g., Senior Stylist, Colorist

        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<EmployeeSchedule> Schedules { get; set; }
    }
}
