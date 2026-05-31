namespace KuaforumAPI.Application.DTOs.Auth
{
    public class VerifyGuestAuthOtpRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
