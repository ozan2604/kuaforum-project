using KuaforumAPI.Domain.Common;
using System;

namespace KuaforumAPI.Domain.Entities
{
    public class EmployeeSchedule : BaseEntity
    {
        public Guid ShopEmployeeId { get; set; }
        public virtual ShopEmployee ShopEmployee { get; set; }

        public DayOfWeek DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday...

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool IsWorking { get; set; } = true;

        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
    }
}
