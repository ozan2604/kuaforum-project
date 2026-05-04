using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopDashboardStatsDto
    {
        public Guid ShopId { get; set; }
        public List<string> Notifications { get; set; } = new();
        public List<NotificationItemDto> NotificationItems { get; set; } = new();
        public SetupStatusDto SetupStatus { get; set; } = new();
        public AppointmentStats Appointments { get; set; } = new();
        public ServiceStats Services { get; set; } = new();
        public EmployeeStats Employees { get; set; } = new();
    }

    public class NotificationItemDto
    {
        public string Type { get; set; } = "info"; // "setup" | "action" | "warning"
        public string Message { get; set; } = "";
        public string? Link { get; set; }
    }

    public class SetupStatusDto
    {
        public bool HasName { get; set; }
        public bool HasDescription { get; set; }
        public bool HasCoverImage { get; set; }
        public bool HasCategories { get; set; }
        public bool HasLocation { get; set; }
        public bool HasOpeningHours { get; set; }
        public bool HasActiveServices { get; set; }
        public bool HasActiveEmployees { get; set; }
        public bool HasEmployeeServices { get; set; }
        public bool HasEmployeeSchedules { get; set; }
        public int CompletionPercentage { get; set; }
    }

    public class AppointmentStats
    {
        public AppointmentPeriodStats Today { get; set; } = new();
        public AppointmentPeriodStats ThisWeek { get; set; } = new();
        public AppointmentPeriodStats ThisMonth { get; set; } = new();
        public AppointmentPeriodStats ThisYear { get; set; } = new();
    }

    public class AppointmentPeriodStats
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public int Rejected { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ServiceStats
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Passive { get; set; }
    }

    public class EmployeeStats
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Passive { get; set; }
    }
}
