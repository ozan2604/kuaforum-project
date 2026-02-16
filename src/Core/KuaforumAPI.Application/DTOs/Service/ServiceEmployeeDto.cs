using System;

namespace KuaforumAPI.Application.DTOs.Service
{
    public class ServiceEmployeeDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public string? ImageUrl { get; set; } // Optional: if we want to show avatars
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
