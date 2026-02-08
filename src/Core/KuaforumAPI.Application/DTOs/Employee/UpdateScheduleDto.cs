using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class UpdateScheduleDto
    {
        public List<ScheduleDto> Schedules { get; set; } = new List<ScheduleDto>();
    }
}
