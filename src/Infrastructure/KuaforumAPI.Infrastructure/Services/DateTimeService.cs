using System;

using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.Infrastructure.Services
{
    public class DateTimeService : IDateTimeService
    {
        private readonly TimeZoneInfo _turkeyTimeZone;

        public DateTimeService()
        {
            try
            {
                _turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
            catch
            {
                // Fallback for Linux/Docker environments where "Turkey Standard Time" might be "Europe/Istanbul"
                try 
                {
                    _turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
                }
                catch
                {
                    // Fallback to UTC+3 if timezone not found
                    _turkeyTimeZone = TimeZoneInfo.CreateCustomTimeZone("Turkey Standard Time", TimeSpan.FromHours(3), "Turkey Standard Time", "Turkey Standard Time");
                }
            }
        }

        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _turkeyTimeZone);
        
        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime ToTurkeyTime(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _turkeyTimeZone);
        }
    }
}
