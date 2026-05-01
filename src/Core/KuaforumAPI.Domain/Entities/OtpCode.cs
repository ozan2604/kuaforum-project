using KuaforumAPI.Domain.Common;
using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Domain.Entities
{
    public class OtpCode : BaseEntity
    {
        public string PhoneNumber { get; set; } = null!;
        public string CodeHash { get; set; } = null!;
        public OtpPurpose Purpose { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AttemptCount { get; set; } = 0;
        public bool IsUsed { get; set; } = false;
    }
}
