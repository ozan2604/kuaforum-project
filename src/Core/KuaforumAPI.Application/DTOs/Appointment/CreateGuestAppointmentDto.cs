using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class CreateGuestAppointmentDto
    {
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [MaxLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string CustomerPhone { get; set; } = string.Empty;

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
    }
}
