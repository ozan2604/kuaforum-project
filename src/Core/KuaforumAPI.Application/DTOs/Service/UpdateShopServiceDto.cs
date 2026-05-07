using System;
using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Service
{
    public class UpdateShopServiceDto
    {
        [Required(ErrorMessage = "Hizmet adı zorunludur.")]
        [MaxLength(200, ErrorMessage = "Hizmet adı en fazla 200 karakter olabilir.")]
        public string Name { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Fiyat 0.01 ile 100.000 arasında olmalıdır.")]
        public decimal Price { get; set; }

        [Range(5, 480, ErrorMessage = "Süre 5 dakika ile 480 dakika (8 saat) arasında olmalıdır.")]
        public int Duration { get; set; }

        public bool IsActive { get; set; }
    }
}
