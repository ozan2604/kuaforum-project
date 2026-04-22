using System;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class UpdateEmployeeOwnerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
    }
}
