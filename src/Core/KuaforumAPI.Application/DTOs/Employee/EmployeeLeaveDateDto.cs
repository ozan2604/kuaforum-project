namespace KuaforumAPI.Application.DTOs.Employee
{
    public class EmployeeLeaveDateDto
    {
        public Guid Id { get; set; }
        public Guid ShopEmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string LeaveDate { get; set; } // "yyyy-MM-dd"
        public string? Reason { get; set; }
    }
}
