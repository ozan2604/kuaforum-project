using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class SendForgotPasswordOtpRequest
    {
        [Required]
        public string PhoneNumber { get; set; }
    }
}
