using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Appointment
{
    public class SendGuestOtpDto
    {
        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        public string Phone { get; set; } = string.Empty;
    }
}
