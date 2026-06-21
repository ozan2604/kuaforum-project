using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Shop
{
    public class ServiceAreaDto
    {
        [Required]
        [MaxLength(50)]
        public string City { get; set; }

        [Required]
        [MaxLength(50)]
        public string District { get; set; }

        [MaxLength(200)]
        public string? Neighborhood { get; set; }
    }
}
