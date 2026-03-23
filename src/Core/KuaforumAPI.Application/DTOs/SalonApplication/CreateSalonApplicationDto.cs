using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.DTOs.SalonApplication
{
    public class CreateSalonApplicationDto
    {
        public string ShopName { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Neighborhood { get; set; }
        public string Street { get; set; }
        public string BuildingNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string ContactEmail { get; set; }
        public int CategoryId { get; set; }
        public TargetGender GenderPreference { get; set; }
    }
}
