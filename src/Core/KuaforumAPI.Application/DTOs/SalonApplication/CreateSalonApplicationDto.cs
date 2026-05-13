using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.DTOs.SalonApplication
{
    public class CreateSalonApplicationDto
    {
        [Required(ErrorMessage = "Salon adı zorunludur.")]
        [MaxLength(100, ErrorMessage = "Salon adı en fazla 100 karakter olabilir.")]
        public string ShopName { get; set; }

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [MaxLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Açık adres zorunludur.")]
        [MaxLength(250, ErrorMessage = "Adres en fazla 250 karakter olabilir.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "İl zorunludur.")]
        [MaxLength(50, ErrorMessage = "İl en fazla 50 karakter olabilir.")]
        public string City { get; set; }

        [Required(ErrorMessage = "İlçe zorunludur.")]
        [MaxLength(50, ErrorMessage = "İlçe en fazla 50 karakter olabilir.")]
        public string District { get; set; }

        [Required(ErrorMessage = "Mahalle zorunludur.")]
        [MaxLength(200, ErrorMessage = "Mahalle en fazla 200 karakter olabilir.")]
        public string Neighborhood { get; set; }

        [Required(ErrorMessage = "Sokak/Cadde zorunludur.")]
        [MaxLength(200, ErrorMessage = "Sokak en fazla 200 karakter olabilir.")]
        public string Street { get; set; }

        [Required(ErrorMessage = "Bina numarası zorunludur.")]
        [MaxLength(20, ErrorMessage = "Bina numarası en fazla 20 karakter olabilir.")]
        public string BuildingNumber { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05\d{9}$", ErrorMessage = "Telefon 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "En az bir kategori seçimi zorunludur.")]
        [MinLength(1, ErrorMessage = "En az bir kategori seçimi zorunludur.")]
        public List<int> CategoryIds { get; set; } = new List<int>();

        [Required(ErrorMessage = "Cinsiyet tercihi zorunludur.")]
        public TargetGender GenderPreference { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
