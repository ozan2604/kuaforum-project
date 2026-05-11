using System;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class EmployeeProfileDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsActive { get; set; }
        public int BookingDaysAhead { get; set; }
    }
}
