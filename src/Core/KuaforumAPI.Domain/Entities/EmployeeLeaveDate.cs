using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    public class EmployeeLeaveDate : BaseEntity
    {
        public Guid ShopEmployeeId { get; set; }
        public virtual ShopEmployee ShopEmployee { get; set; }

        public DateTime LeaveDate { get; set; }

        public string? Reason { get; set; }
    }
}
