using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.DTOs.Block
{
    public class CustomerShopInfoDto
    {
        public string CustomerId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int RejectedCount { get; set; }
        public int NoShowCount { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public decimal TotalSpent { get; set; }
        public bool IsBlocked { get; set; }
        public List<CustomerReviewSummaryDto> Reviews { get; set; } = [];
        public List<CustomerAppointmentSummaryDto> RecentAppointments { get; set; } = [];
    }

    public class CustomerReviewSummaryDto
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? EmployeeName { get; set; }
        public string? ServiceName { get; set; }
    }

    public class CustomerAppointmentSummaryDto
    {
        public string ServiceName { get; set; } = "";
        public decimal Price { get; set; }
        public DateTime StartTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? EmployeeName { get; set; }
    }
}
