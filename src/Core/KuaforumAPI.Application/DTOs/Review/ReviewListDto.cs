using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Review
{
    public class ReviewListDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; } // User.FirstName + LastName
        public string UserProfileImage { get; set; } // Optional
        public Guid ShopId { get; set; }
        public Guid ShopEmployeeId { get; set; }
        public string EmployeeName { get; set; } // ShopEmployee.User.FirstName + LastName OR Title
        public string ShopName { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> ImageUrls { get; set; }
    }
}
