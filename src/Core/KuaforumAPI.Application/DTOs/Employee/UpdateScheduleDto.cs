using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class UpdateScheduleDto
    {
        [Required(ErrorMessage = "Program listesi zorunludur.")]
        [MinLength(1, ErrorMessage = "En az bir gün programı girilmelidir.")]
        public List<ScheduleDto> Schedules { get; set; } = new List<ScheduleDto>();
    }
}
