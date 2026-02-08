using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class AssignServicesDto
    {
        public List<Guid> ServiceIds { get; set; } = new List<Guid>();
    }
}
