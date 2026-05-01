namespace KuaforumAPI.Application.DTOs.Auth
{
    public class SendLoginOtpRequest
    {
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
