using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Service
{
    public class CreateServiceCategoryDto
    {
        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [MaxLength(50, ErrorMessage = "Kategori adı en fazla 50 karakter olabilir.")]
        public string Name { get; set; }

        [MaxLength(250, ErrorMessage = "Açıklama en fazla 250 karakter olabilir.")]
        public string? Description { get; set; }
    }
}
