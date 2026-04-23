using KuaforumAPI.Domain.Enums;
using System;
using System.Collections.Generic;

namespace KuaforumAPI.Application.DTOs.SalonApplication
{
    public class SalonApplicationListDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ShopName { get; set; }
        public string Description { get; set; }
        public string ContactEmail { get; set; }
        public List<int> Categories { get; set; } = new List<int>();
        public TargetGender GenderPreference { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
