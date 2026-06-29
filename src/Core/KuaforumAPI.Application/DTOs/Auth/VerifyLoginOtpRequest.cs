using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class VerifyLoginOtpRequest
    {
        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "OTP kodu zorunludur.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP kodu 6 haneli olmalıdır.")]
        public string OtpCode { get; set; } = null!;
    }
}
