using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class CreateManualAppointmentDto : IValidatableObject
    {
        [Required(ErrorMessage = "Salon seçimi zorunludur.")]
        public Guid ShopId { get; set; }

        [Required(ErrorMessage = "En az bir hizmet seçilmelidir.")]
        [MinLength(1, ErrorMessage = "En az bir hizmet seçilmelidir.")]
        public List<Guid> ServiceIds { get; set; } = new List<Guid>();

        [Required(ErrorMessage = "Personel seçimi zorunludur.")]
        public Guid ShopEmployeeId { get; set; }

        [Required(ErrorMessage = "Randevu başlangıç saati zorunludur.")]
        public DateTime StartTime { get; set; }

        [MaxLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        public string? Note { get; set; }

        // Müşteri bilgileri: ya GuestCustomerName dolu olmalı ya da her ikisi de boş (anonim misafir)
        [MaxLength(100)]
        public string? GuestCustomerName { get; set; }

        [MaxLength(20)]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string? GuestCustomerPhone { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Telefon girilmişse isim de zorunlu
            if (!string.IsNullOrWhiteSpace(GuestCustomerPhone) && string.IsNullOrWhiteSpace(GuestCustomerName))
                yield return new ValidationResult("Telefon numarası girildiğinde müşteri adı zorunludur.", new[] { nameof(GuestCustomerName) });
        }
    }
}
