using KuaforumAPI.Domain.Common;
using KuaforumAPI.Domain.Enums;
using System;

namespace KuaforumAPI.Domain.Entities
{
    public class Appointment : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }

        public Guid ShopServiceId { get; set; }
        public virtual ShopService ShopService { get; set; }

        public Guid ShopEmployeeId { get; set; }
        public virtual ShopEmployee ShopEmployee { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string? Note { get; set; }

        public Guid? GroupId { get; set; }

        public string? CancellationReason { get; set; }

        public bool Is48hReminderSent { get; set; } = false;
        public bool Is2hReminderSent { get; set; } = false;
        public bool IsIncludedInOwnerSummary { get; set; } = false;
    }
}
