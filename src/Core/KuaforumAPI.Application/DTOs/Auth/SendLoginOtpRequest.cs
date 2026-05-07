using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class SendLoginOtpRequest
    {
        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        public string Password { get; set; } = null!;
    }
}
