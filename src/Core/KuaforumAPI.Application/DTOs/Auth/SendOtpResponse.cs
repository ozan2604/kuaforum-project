namespace KuaforumAPI.Application.DTOs.Auth
{
    public class SendOtpResponse
    {
        public string Message { get; set; } = null!;
        public int ExpiresInSeconds { get; set; }
    }
}
