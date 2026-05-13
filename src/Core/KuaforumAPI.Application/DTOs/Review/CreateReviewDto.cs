using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KuaforumAPI.Application.DTOs.Review
{
    public class CreateReviewDto : IValidatableObject
    {
        [Required(ErrorMessage = "Randevu seçimi zorunludur.")]
        public Guid AppointmentId { get; set; }

        [Required(ErrorMessage = "Puan zorunludur.")]
        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        public int Rating { get; set; }

        [MaxLength(1000, ErrorMessage = "Yorum en fazla 1000 karakter olabilir.")]
        public string? Comment { get; set; }

        public List<IFormFile>? Images { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Images != null && Images.Count > 5)
                yield return new ValidationResult("En fazla 5 fotoğraf yükleyebilirsiniz.", new[] { nameof(Images) });
        }
    }
}
