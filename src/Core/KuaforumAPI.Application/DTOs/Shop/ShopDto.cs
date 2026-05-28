using KuaforumAPI.Domain.Enums;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ShopDto
    {
        public Guid Id { get; set; }
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
        public List<int> Categories { get; set; } = new List<int>();
        public TargetGender GenderPreference { get; set; }
        
        public string SaturdayClosingTime { get; set; }
        public List<ShopScheduleDto> WeeklySchedule { get; set; }

        public string CoverImagePath { get; set; }
        public string PromoVideoUrl { get; set; }  // Legacy alan - geriye dönük uyumluluk
        public List<ShopVideoDto> Videos { get; set; } = new List<ShopVideoDto>();
        public List<ShopImageDto> Images { get; set; }

        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal? MinServicePrice { get; set; }

        public bool IsActive { get; set; }
        public bool IsAutoProcessEnabled { get; set; }
        public int BookingDaysAhead { get; set; }
        public int CancellationHours { get; set; }

        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }
        public List<int> WeeklyOffDays { get; set; } = new List<int>();
        public List<ShopClosureDateDto> ClosureDates { get; set; } = new List<ShopClosureDateDto>();

        public string? Code { get; set; }

        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
