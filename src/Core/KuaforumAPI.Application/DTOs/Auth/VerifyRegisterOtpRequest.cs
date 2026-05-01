namespace KuaforumAPI.Application.DTOs.Auth
{
    public class VerifyRegisterOtpRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string OtpCode { get; set; } = null!;
    }
}
