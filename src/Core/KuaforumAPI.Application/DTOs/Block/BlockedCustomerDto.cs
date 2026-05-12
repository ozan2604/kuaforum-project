namespace KuaforumAPI.Application.DTOs.Block
{
    public class BlockedCustomerDto
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Reason { get; set; }
        public DateTime BlockedAt { get; set; }
    }
}
