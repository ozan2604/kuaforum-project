using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class SendLoginOtpRequest
    {
        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; } = null!;
    }
}
