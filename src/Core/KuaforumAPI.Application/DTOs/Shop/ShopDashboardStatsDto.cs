using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopDashboardStatsDto
    {
        public Guid ShopId { get; set; }
        public List<string> Notifications { get; set; } = new();
        public AppointmentStats Appointments { get; set; } = new();
        public ServiceStats Services { get; set; } = new();
        public EmployeeStats Employees { get; set; } = new();
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
