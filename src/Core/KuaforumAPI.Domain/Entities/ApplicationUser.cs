using Microsoft.AspNetCore.Identity;

namespace KuaforumAPI.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    }
}
