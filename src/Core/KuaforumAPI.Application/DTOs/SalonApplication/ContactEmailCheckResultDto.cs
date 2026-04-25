namespace KuaforumAPI.Application.DTOs.SalonApplication
{
    public class ContactEmailCheckResultDto
    {
        public bool IsAvailable { get; set; }
        public bool IsUsedByShop { get; set; }
        public bool IsUsedByApplication { get; set; }
        public bool IsRegisteredUser { get; set; }
    }
}
