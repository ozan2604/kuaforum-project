using System;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class ScheduleDto
    {
        [Range(0, 6, ErrorMessage = "Gün 0 (Pazar) ile 6 (Cumartesi) arasında olmalıdır.")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
        [RegularExpression(@"^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Saat HH:mm formatında olmalıdır.")]
        public string StartTime { get; set; }

        [Required(ErrorMessage = "Bitiş saati zorunludur.")]
        [RegularExpression(@"^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Saat HH:mm formatında olmalıdır.")]
        public string EndTime { get; set; }

        public bool IsWorking { get; set; }

        [RegularExpression(@"^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Saat HH:mm formatında olmalıdır.")]
        public string? BreakStartTime { get; set; }

        [RegularExpression(@"^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Saat HH:mm formatında olmalıdır.")]
        public string? BreakEndTime { get; set; }
    }
}
