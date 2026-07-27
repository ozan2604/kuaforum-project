using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    public class SiteVisit : BaseEntity
    {
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Referrer { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string Os { get; set; } = string.Empty;
    }
}
