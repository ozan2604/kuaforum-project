namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class NoShowResultDto
    {
        public int NoShowCount { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
    }
}
