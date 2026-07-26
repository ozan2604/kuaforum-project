using KuaforumAPI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.AdApplication
{
    public class UpdateAdApplicationStatusDto
    {
        [Required]
        public ApplicationStatus Status { get; set; }
    }
}
