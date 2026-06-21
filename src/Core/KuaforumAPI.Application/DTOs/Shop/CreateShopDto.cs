using KuaforumAPI.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class CreateShopDto
    {
        [Required(ErrorMessage = "Salon adı zorunludur.")]
        [MaxLength(100, ErrorMessage = "Salon adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [MaxLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
        public string? Description { get; set; }

        [MaxLength(250, ErrorMessage = "Adres en fazla 250 karakter olabilir.")]
        public string? Address { get; set; }

        [MaxLength(50, ErrorMessage = "İl en fazla 50 karakter olabilir.")]
        public string? City { get; set; }

        [MaxLength(50, ErrorMessage = "İlçe en fazla 50 karakter olabilir.")]
        public string? District { get; set; }

        [MaxLength(200, ErrorMessage = "Mahalle en fazla 200 karakter olabilir.")]
        public string? Neighborhood { get; set; }

        [MaxLength(200, ErrorMessage = "Sokak en fazla 200 karakter olabilir.")]
        public string? Street { get; set; }

        [MaxLength(20, ErrorMessage = "Bina numarası en fazla 20 karakter olabilir.")]
        public string? BuildingNumber { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; }

        [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalıdır.")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalıdır.")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "En az bir kategori seçilmelidir.")]
        [MinLength(1, ErrorMessage = "En az bir kategori seçilmelidir.")]
        public List<int> CategoryIds { get; set; } = new List<int>();

        [EnumDataType(typeof(TargetGender), ErrorMessage = "Geçersiz cinsiyet tercihi.")]
        public TargetGender GenderPreference { get; set; }

        [RegularExpression(@"^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Açılış saati HH:mm formatında olmalıdır.")]
        public string? OpenTime { get; set; }

        [RegularExpression(@"^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Kapanış saati HH:mm formatında olmalıdır.")]
        public string? CloseTime { get; set; }

        [Range(1, 365, ErrorMessage = "İleri randevu günü 1 ile 365 arasında olmalıdır.")]
        public int BookingDaysAhead { get; set; } = 30;

        [Range(0, 72, ErrorMessage = "İptal süresi 0 ile 72 saat arasında olmalıdır.")]
        public int CancellationHours { get; set; } = 2;

        public List<int>? WeeklyOffDays { get; set; }

        public ShopType ShopType { get; set; } = ShopType.Fixed;

        // Mobile ise en az 1 bölge zorunlu
        public List<ServiceAreaDto>? ServiceAreas { get; set; }
    }
}
