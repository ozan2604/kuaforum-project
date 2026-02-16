using KuaforumAPI.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KuaforumAPI.Domain.Entities
{
    public class Review : BaseEntity
    {
        public Guid AppointmentId { get; set; }
        [ForeignKey("AppointmentId")]
        public virtual Appointment Appointment { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public Guid ShopId { get; set; }
        [ForeignKey("ShopId")]
        public virtual Shop Shop { get; set; }

        public Guid ShopEmployeeId { get; set; }
        [ForeignKey("ShopEmployeeId")]
        public virtual ShopEmployee ShopEmployee { get; set; }

        public int Rating { get; set; } // 1-5 stars

        public string? Comment { get; set; }

        public virtual ICollection<ReviewImage> Images { get; set; }

        public Review()
        {
            Images = new HashSet<ReviewImage>();
        }
    }
}
