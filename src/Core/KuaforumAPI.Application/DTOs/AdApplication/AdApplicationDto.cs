using System;

namespace KuaforumAPI.Application.DTOs.AdApplication
{
    public class AdApplicationDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string MediaUrl { get; set; }
        public string MediaType { get; set; }
        public string Description { get; set; }
        public string PhoneNumber { get; set; }
        public string ExternalLink { get; set; }
        public decimal? Price { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
