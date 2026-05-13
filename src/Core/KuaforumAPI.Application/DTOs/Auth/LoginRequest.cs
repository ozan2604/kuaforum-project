using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [MaxLength(50, ErrorMessage = "Geçersiz giriş bilgisi.")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        public string Password { get; set; } = string.Empty;
    }
}
