using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http; // For IFormFile? No, images handled via separate upload or review image upload.

namespace KuaforumAPI.Application.DTOs.Review
{
    public class CreateReviewDto
    {
        public Guid AppointmentId { get; set; }
        public int Rating { get; set; } // 1-5
        public string? Comment { get; set; }
        public List<IFormFile>? Images { get; set; } // If handling images directly in multipart
    }
}
