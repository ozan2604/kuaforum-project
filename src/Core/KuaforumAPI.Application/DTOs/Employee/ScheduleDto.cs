using System;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class ScheduleDto
    {
        public int DayOfWeek { get; set; } // 0 = Sunday
        public string StartTime { get; set; } // "09:00"
        public string EndTime { get; set; } // "18:00"
        public bool IsWorking { get; set; }
        public string? BreakStartTime { get; set; }
        public string? BreakEndTime { get; set; }
    }
}
