using System;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class CreateAppointmentDto
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

        [MaxLength(500)]
        public string? CustomerAddress { get; set; }
    }
}
