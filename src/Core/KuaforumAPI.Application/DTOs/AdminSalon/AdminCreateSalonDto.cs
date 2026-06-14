using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.DTOs.AdminSalon
{
    public class AdminCreateSalonDto
    {
        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05\d{9}$", ErrorMessage = "Telefon 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Dükkan adı zorunludur.")]
        [MaxLength(100, ErrorMessage = "Dükkan adı en fazla 100 karakter olabilir.")]
        public string ShopName { get; set; }

        [Required(ErrorMessage = "En az bir kategori seçimi zorunludur.")]
        [MinLength(1, ErrorMessage = "En az bir kategori seçimi zorunludur.")]
        public List<int> CategoryIds { get; set; } = new List<int>();

        [Required(ErrorMessage = "Cinsiyet tercihi zorunludur.")]
        public TargetGender GenderPreference { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? District { get; set; }

        [MaxLength(200)]
        public string? Neighborhood { get; set; }

        [MaxLength(200)]
        public string? Street { get; set; }

        [MaxLength(20)]
        public string? BuildingNumber { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }
    }
}
