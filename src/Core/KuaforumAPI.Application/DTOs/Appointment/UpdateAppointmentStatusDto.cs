using KuaforumAPI.Domain.Enums;
using System;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class UpdateAppointmentStatusDto
    {
        public AppointmentStatus Status { get; set; }
    }
}
