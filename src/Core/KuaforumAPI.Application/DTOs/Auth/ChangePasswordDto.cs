using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Şifre onayı zorunludur.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; }
    }
}
