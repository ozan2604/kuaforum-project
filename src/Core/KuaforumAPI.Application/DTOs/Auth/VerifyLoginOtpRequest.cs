namespace KuaforumAPI.Application.DTOs.Auth
{
    public class VerifyLoginOtpRequest
    {
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
    }
}
