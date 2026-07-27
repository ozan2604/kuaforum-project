using System.Threading.Tasks;
using KuaforumAPI.Application.DTOs.Analytics;
using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface ISiteAnalyticsService
    {
        Task<bool> LogVisitAsync(LogVisitDto dto, string ipAddress);
        Task<SiteStatsDto> GetSiteStatsAsync();
        Task<SiteStatsDto> GetShopStatsAsync(Guid shopId);
    }
}
