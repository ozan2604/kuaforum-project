using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class AssignServicesDto
    {
        [Required(ErrorMessage = "Hizmet listesi zorunludur.")]
        [MinLength(1, ErrorMessage = "En az bir hizmet seçilmelidir.")]
        public List<Guid> ServiceIds { get; set; } = new List<Guid>();
    }
}
