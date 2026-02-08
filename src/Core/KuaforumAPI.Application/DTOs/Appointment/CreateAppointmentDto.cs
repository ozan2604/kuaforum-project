using System;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class CreateAppointmentDto
    {
        public Guid ShopId { get; set; }
        public Guid ShopServiceId { get; set; }
        public Guid ShopEmployeeId { get; set; }
        public DateTime StartTime { get; set; }
        public string? Note { get; set; }
    }
}
