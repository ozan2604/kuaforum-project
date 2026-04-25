using System;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class CreateAppointmentDto
    {
        public Guid ShopId { get; set; }
        public List<Guid> ServiceIds { get; set; } = new List<Guid>();
        public Guid ShopEmployeeId { get; set; }
        public DateTime StartTime { get; set; }
        public string? Note { get; set; }
    }
}
