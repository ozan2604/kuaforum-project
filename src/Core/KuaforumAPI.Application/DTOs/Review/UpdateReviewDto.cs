using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace KuaforumAPI.Application.DTOs.Review
{
    public class UpdateReviewDto
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public List<IFormFile>? NewImages { get; set; }
        public List<string>? DeletedImageUrls { get; set; } // Optional: to handle image deletion
    }
}
