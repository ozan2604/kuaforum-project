using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KuaforumAPI.Application.DTOs.Analytics;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;

namespace KuaforumAPI.Infrastructure.Services
{
    public class SiteAnalyticsService : ISiteAnalyticsService
    {
        private readonly ISiteVisitRepository _siteVisitRepository;
        private readonly IDateTimeService _dateTimeService;

        public SiteAnalyticsService(ISiteVisitRepository siteVisitRepository, IDateTimeService dateTimeService)
        {
            _siteVisitRepository = siteVisitRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task<bool> LogVisitAsync(LogVisitDto dto, string ipAddress)
        {
            var now = _dateTimeService.Now;

            // Simple parser for Source
            var source = "Direct";
            var refLower = dto.Referrer?.ToLower() ?? "";
            if (refLower.Contains("instagram.com")) source = "Instagram";
            else if (refLower.Contains("google.com") || refLower.Contains("google.com.tr")) source = "Google";
            else if (refLower.Contains("facebook.com")) source = "Facebook";
            else if (refLower.Contains("twitter.com") || refLower.Contains("x.com")) source = "Twitter";
            else if (refLower.Contains("tiktok.com")) source = "TikTok";
            else if (!string.IsNullOrEmpty(refLower)) source = "Other";

            // Simple parser for Device
            var device = "Desktop";
            var uaLower = dto.UserAgent?.ToLower() ?? "";
            if (uaLower.Contains("mobile") || uaLower.Contains("android") || uaLower.Contains("iphone"))
            {
                device = "Mobile";
                if (uaLower.Contains("ipad") || uaLower.Contains("tablet"))
                    device = "Tablet";
            }

            // Simple parser for Browser
            var browser = "Unknown";
            if (uaLower.Contains("chrome") && !uaLower.Contains("edg") && !uaLower.Contains("opr")) browser = "Chrome";
            else if (uaLower.Contains("safari") && !uaLower.Contains("chrome")) browser = "Safari";
            else if (uaLower.Contains("firefox")) browser = "Firefox";
            else if (uaLower.Contains("edg")) browser = "Edge";
            else if (uaLower.Contains("opr") || uaLower.Contains("opera")) browser = "Opera";
            else browser = "Other";

            // Simple parser for OS
            var os = "Unknown";
            if (uaLower.Contains("windows")) os = "Windows";
            else if (uaLower.Contains("mac os") || uaLower.Contains("macos")) os = "MacOS";
            else if (uaLower.Contains("android")) os = "Android";
            else if (uaLower.Contains("iphone") || uaLower.Contains("ipad")) os = "iOS";
            else if (uaLower.Contains("linux")) os = "Linux";
            else os = "Other";

            var visit = new SiteVisit
            {
                IpAddress = ipAddress,
                UserAgent = dto.UserAgent ?? "",
                Referrer = dto.Referrer ?? "",
                Source = source,
                DeviceType = device,
                Browser = browser,
                Os = os,
                ShopId = dto.ShopId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _siteVisitRepository.AddAsync(visit);

            return true;
        }

        public async Task<SiteStatsDto> GetSiteStatsAsync()
        {
            var now = _dateTimeService.Now;
            var today = now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var visits = await _siteVisitRepository.GetAllAsync();
            var allVisits = visits.Where(v => v.ShopId == null).ToList();

            var stats = new SiteStatsDto
            {
                TotalVisitsToday = allVisits.Count(v => v.CreatedAt.Date == today),
                TotalVisitsThisWeek = allVisits.Count(v => v.CreatedAt.Date >= startOfWeek),
                TotalVisitsThisMonth = allVisits.Count(v => v.CreatedAt.Date >= startOfMonth),

                Sources = allVisits
                    .GroupBy(v => v.Source)
                    .Select(g => new SourceStatDto { Source = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),

                Devices = allVisits
                    .GroupBy(v => v.DeviceType)
                    .Select(g => new DeviceStatDto { Device = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),

                Browsers = allVisits
                    .GroupBy(v => v.Browser)
                    .Select(g => new BrowserStatDto { Browser = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList()
            };

            return stats;
        }

        public async Task<SiteStatsDto> GetShopStatsAsync(Guid shopId)
        {
            var now = _dateTimeService.Now;
            var today = now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var visits = await _siteVisitRepository.GetAllAsync();
            var allVisits = visits.Where(v => v.ShopId == shopId).ToList();

            var stats = new SiteStatsDto
            {
                TotalVisitsToday = allVisits.Count(v => v.CreatedAt.Date == today),
                TotalVisitsThisWeek = allVisits.Count(v => v.CreatedAt.Date >= startOfWeek),
                TotalVisitsThisMonth = allVisits.Count(v => v.CreatedAt.Date >= startOfMonth),

                Sources = allVisits
                    .GroupBy(v => v.Source)
                    .Select(g => new SourceStatDto { Source = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),

                Devices = allVisits
                    .GroupBy(v => v.DeviceType)
                    .Select(g => new DeviceStatDto { Device = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),

                Browsers = allVisits
                    .GroupBy(v => v.Browser)
                    .Select(g => new BrowserStatDto { Browser = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList()
            };

            return stats;
        }
    }
}
