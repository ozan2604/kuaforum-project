using System.ComponentModel.DataAnnotations;
using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.DTOs.SalonApplication
{
    public class CreateSalonApplicationDto
    {
        [Required(ErrorMessage = "Salon adı zorunludur.")]
        [MaxLength(200)]
        public string ShopName { get; set; }

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [MaxLength(2000)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Açık adres zorunludur.")]
        [MaxLength(500)]
        public string Address { get; set; }

        [Required(ErrorMessage = "İl zorunludur.")]
        [MaxLength(100)]
        public string City { get; set; }

        [Required(ErrorMessage = "İlçe zorunludur.")]
        [MaxLength(100)]
        public string District { get; set; }

        [Required(ErrorMessage = "Mahalle zorunludur.")]
        [MaxLength(200)]
        public string Neighborhood { get; set; }

        [Required(ErrorMessage = "Sokak/Cadde zorunludur.")]
        [MaxLength(200)]
        public string Street { get; set; }

        [Required(ErrorMessage = "Bina numarası zorunludur.")]
        [MaxLength(20)]
        public string BuildingNumber { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05\d{9}$", ErrorMessage = "Telefon 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(200)]
        public string ContactEmail { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Kategori seçimi zorunludur.")]
        public int CategoryId { get; set; }

        [Required]
        public TargetGender GenderPreference { get; set; }
    }
}

