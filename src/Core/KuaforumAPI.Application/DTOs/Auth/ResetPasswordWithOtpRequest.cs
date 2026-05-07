using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class ResetPasswordWithOtpRequest
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
}
