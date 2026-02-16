using System;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IDateTimeService
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
        DateTime ToTurkeyTime(DateTime utcDateTime);
    }
}
