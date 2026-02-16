using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class EmployeeAvailabilityDto
    {
        public bool IsWorking { get; set; }
        public TimeSpan? WorkStartTime { get; set; }
        public TimeSpan? WorkEndTime { get; set; }
        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
        public List<TimeSlotDto> BookedSlots { get; set; } = new List<TimeSlotDto>();
    }

    public class TimeSlotDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
