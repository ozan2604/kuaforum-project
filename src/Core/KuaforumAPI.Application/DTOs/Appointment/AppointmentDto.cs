using KuaforumAPI.Domain.Enums;
using System;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }

        public Guid ShopServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }

        public Guid ShopEmployeeId { get; set; }
        public string EmployeeName { get; set; }

        public string? UserId { get; set; }
        public string CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public bool IsManual { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public AppointmentStatus Status { get; set; }
        public string? Note { get; set; }
        public Guid? GroupId { get; set; }
        public bool HasReview { get; set; }
        public string? CancellationReason { get; set; }
        public int ShopCancellationHours { get; set; }
    }
}
