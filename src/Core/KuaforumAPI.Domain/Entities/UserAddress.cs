using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    public class UserAddress : BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string OpenAddress { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
