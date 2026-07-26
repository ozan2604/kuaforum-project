using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.AdApplication
{
    public class CreateAdApplicationDto
    {
        [Required(ErrorMessage = "Reklam medyası gereklidir.")]
        public IFormFile Media { get; set; }

        [Required(ErrorMessage = "Açıklama gereklidir.")]
        [MaxLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "İletişim numarası gereklidir.")]
        [MaxLength(20, ErrorMessage = "Geçersiz telefon numarası.")]
        public string PhoneNumber { get; set; }

        public string ExternalLink { get; set; }
        
        public decimal? Price { get; set; }
    }
}
