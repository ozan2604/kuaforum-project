using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Ad zorunludur.")]
        [MaxLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [MaxLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public string LastName { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; }
    }
}
