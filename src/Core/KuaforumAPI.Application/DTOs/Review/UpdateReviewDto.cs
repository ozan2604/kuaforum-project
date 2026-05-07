using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KuaforumAPI.Application.DTOs.Review
{
    public class UpdateReviewDto
    {
        [Required(ErrorMessage = "Değerlendirme ID zorunludur.")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Puan zorunludur.")]
        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        public int Rating { get; set; }

        [MaxLength(1000, ErrorMessage = "Yorum en fazla 1000 karakter olabilir.")]
        public string? Comment { get; set; }

        public List<IFormFile>? NewImages { get; set; }
        public List<string>? DeletedImageUrls { get; set; }
    }
}
