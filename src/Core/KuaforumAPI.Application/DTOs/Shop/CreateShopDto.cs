using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class CreateShopDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Neighborhood { get; set; }
        public string Street { get; set; }
        public string BuildingNumber { get; set; }
        public string PhoneNumber { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<int> CategoryIds { get; set; } = new List<int>();
        public TargetGender GenderPreference { get; set; }

        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }
        public int BookingDaysAhead { get; set; } = 30;
        public int CancellationHours { get; set; } = 2;
        public List<int>? WeeklyOffDays { get; set; }
    }
}
