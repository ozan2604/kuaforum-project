using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class PublicEmployeeScheduleDto
    {
        public Guid EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Title { get; set; }
        public List<ScheduleDto> Schedule { get; set; } = new();
    }
}
