using KuaforumAPI.Domain.Enums;
using System;

namespace KuaforumAPI.Application.DTOs.SalonApplication
{
    public class SalonApplicationListDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; } // Extra info for Admin
        public string ShopName { get; set; }
        public string Description { get; set; }
        public string TaxNumber { get; set; } // Added TaxNumber
        public ApplicationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
