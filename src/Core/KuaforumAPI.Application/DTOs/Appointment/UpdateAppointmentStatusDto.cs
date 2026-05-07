using KuaforumAPI.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class UpdateAppointmentStatusDto
    {
        [EnumDataType(typeof(AppointmentStatus), ErrorMessage = "Geçersiz randevu durumu.")]
        public AppointmentStatus Status { get; set; }

        [MaxLength(500, ErrorMessage = "Neden en fazla 500 karakter olabilir.")]
        public string? Reason { get; set; }
    }
}
