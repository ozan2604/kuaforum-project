using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.Analytics
{
    public class SiteStatsDto
    {
        public int TotalVisitsToday { get; set; }
        public int TotalVisitsThisWeek { get; set; }
        public int TotalVisitsThisMonth { get; set; }
        
        public List<SourceStatDto> Sources { get; set; } = new();
        public List<DeviceStatDto> Devices { get; set; } = new();
        public List<BrowserStatDto> Browsers { get; set; } = new();
    }

    public class SourceStatDto
    {
        public string Source { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DeviceStatDto
    {
        public string Device { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class BrowserStatDto
    {
        public string Browser { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
