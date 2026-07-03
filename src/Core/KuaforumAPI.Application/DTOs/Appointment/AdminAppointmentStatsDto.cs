using System;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class AdminAppointmentStatsDto
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }

        public int TodayManualCount { get; set; }
        public int TodayNormalCount { get; set; }

        public int WeekManualCount { get; set; }
        public int WeekNormalCount { get; set; }

        public int MonthManualCount { get; set; }
        public int MonthNormalCount { get; set; }

        public int YearManualCount { get; set; }
        public int YearNormalCount { get; set; }
    }
}
