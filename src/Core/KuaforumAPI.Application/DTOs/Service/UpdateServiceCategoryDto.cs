using System;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Service
{
    public class UpdateServiceCategoryDto
    {
        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [MaxLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; }

        public bool IsActive { get; set; }
    }
}
