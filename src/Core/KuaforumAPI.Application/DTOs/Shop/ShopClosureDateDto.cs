namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopClosureDateDto
    {
        public Guid Id { get; set; }
        public DateTime ClosureDate { get; set; }
        public string? Reason { get; set; }
    }
}
