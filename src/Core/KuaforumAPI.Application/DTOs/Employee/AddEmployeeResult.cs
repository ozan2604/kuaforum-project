namespace KuaforumAPI.Application.DTOs.Employee
{
    public class AddEmployeeResult
    {
        public bool IsNewUser { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string? TemporaryPassword { get; set; }
    }
}
