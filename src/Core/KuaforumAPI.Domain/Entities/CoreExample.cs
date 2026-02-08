using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    public class CoreExample : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
