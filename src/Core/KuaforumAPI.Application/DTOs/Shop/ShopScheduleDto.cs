using System;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopScheduleDto
    {
        public string Day { get; set; }
        public int DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday...
        public string OpeningTime { get; set; }
        public string ClosingTime { get; set; }
        public bool IsClosed { get; set; }
    }
}
