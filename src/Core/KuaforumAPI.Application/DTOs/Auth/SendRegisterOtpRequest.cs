using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class SendRegisterOtpRequest
    {
        [Required(ErrorMessage = "Ad zorunludur.")]
        [MaxLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [MaxLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        public string Password { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
        public string? Email { get; set; }

        public string? Role { get; set; }
    }
}
