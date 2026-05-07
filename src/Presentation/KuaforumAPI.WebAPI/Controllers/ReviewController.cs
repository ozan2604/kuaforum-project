using KuaforumAPI.Application.DTOs.Review;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KuaforumAPI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("reviews")]
        public async Task<IActionResult> AddReview([FromForm] CreateReviewDto createReviewDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = await _reviewService.AddReviewAsync(createReviewDto, userId);

            var reviewDto = new ReviewListDto
            {
                Id = review.Id,
                AppointmentId = review.AppointmentId,
                UserId = review.UserId,
                UserName = User.Identity?.Name,
                ShopId = review.ShopId,
                ShopName = review.Shop?.Name ?? string.Empty,
                ShopEmployeeId = review.ShopEmployeeId,
                EmployeeName = review.ShopEmployee?.User != null ? $"{review.ShopEmployee.User.FirstName} {review.ShopEmployee.User.LastName}" : string.Empty,
                ServiceName = review.Appointment?.ShopService?.Name ?? string.Empty,
                AppointmentDate = review.Appointment?.StartTime ?? review.CreatedAt,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                ImageUrls = review.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                ServicePrice = review.Appointment?.ShopService?.Price ?? 0
            };

            return Ok(reviewDto);
        }

        [HttpGet("shop/{shopId}")]
        public async Task<IActionResult> GetShopReviews(Guid shopId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var reviews = await _reviewService.GetShopReviewsAsync(shopId, userId);
            
            // Map manually to DTO to avoid cycles and simplify response
            var dtos = reviews.Select(r => new ReviewListDto
            {
                Id = r.Id,
                AppointmentId = r.AppointmentId,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Unknown User",
                ShopId = r.ShopId,
                ShopEmployeeId = r.ShopEmployeeId,
                EmployeeName = r.ShopEmployee != null && r.ShopEmployee.User != null 
                    ? $"{r.ShopEmployee.User.FirstName} {r.ShopEmployee.User.LastName}" 
                    : "Unknown Employee", 
                ShopName = r.Shop?.Name ?? "Unknown Shop",
                ServiceName = r.Appointment?.ShopService?.Name ?? "Unknown Service",
                AppointmentDate = r.Appointment?.StartTime ?? r.CreatedAt,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                ImageUrls = r.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                ServicePrice = r.Appointment?.ShopService?.Price ?? 0
            });

            return Ok(dtos);
        }

        [HttpPut("{id}")]
        [Authorize]
        [EnableRateLimiting("reviews")]
        public async Task<IActionResult> UpdateReview(Guid id, [FromForm] UpdateReviewDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { message = "ID uyuşmazlığı." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var updatedReview = await _reviewService.UpdateReviewAsync(dto, userId);

            var reviewDto = new ReviewListDto
            {
                Id = updatedReview.Id,
                AppointmentId = updatedReview.AppointmentId,
                UserId = updatedReview.UserId,
                UserName = User.Identity?.Name,
                ShopId = updatedReview.ShopId,
                ShopName = updatedReview.Shop?.Name ?? string.Empty,
                ShopEmployeeId = updatedReview.ShopEmployeeId,
                EmployeeName = updatedReview.ShopEmployee?.User != null ? $"{updatedReview.ShopEmployee.User.FirstName} {updatedReview.ShopEmployee.User.LastName}" : string.Empty,
                ServiceName = updatedReview.Appointment?.ShopService?.Name ?? string.Empty,
                AppointmentDate = updatedReview.Appointment?.StartTime ?? updatedReview.CreatedAt,
                Rating = updatedReview.Rating,
                Comment = updatedReview.Comment,
                CreatedAt = updatedReview.CreatedAt,
                ImageUrls = updatedReview.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                ServicePrice = updatedReview.Appointment?.ShopService?.Price ?? 0
            };

            return Ok(reviewDto);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _reviewService.DeleteReviewAsync(id, userId);
            return Ok(new { message = "Değerlendirme silindi." });
        }

        [HttpGet("my-shop")]
        [Authorize]
        public async Task<IActionResult> GetMyShopReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reviews = await _reviewService.GetMyShopReviewsAsync(userId);

            var dtos = reviews.Select(r => new ReviewListDto
            {
                Id = r.Id,
                AppointmentId = r.AppointmentId,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Unknown User",
                ShopId = r.ShopId,
                ShopEmployeeId = r.ShopEmployeeId,
                EmployeeName = r.ShopEmployee != null && r.ShopEmployee.User != null
                    ? $"{r.ShopEmployee.User.FirstName} {r.ShopEmployee.User.LastName}"
                    : "Unknown Employee",
                ShopName = r.Shop?.Name ?? "Unknown Shop",
                ServiceName = r.Appointment?.ShopService?.Name ?? "Unknown Service",
                AppointmentDate = r.Appointment?.StartTime ?? r.CreatedAt,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                ImageUrls = r.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                ServicePrice = r.Appointment?.ShopService?.Price ?? 0
            });

            return Ok(dtos);
        }

        [HttpGet("my-reviews")]
        [Authorize]
        public async Task<IActionResult> GetMyReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reviews = await _reviewService.GetMyReviewsAsync(userId);

            var dtos = reviews.Select(r => new ReviewListDto
            {
                Id = r.Id,
                AppointmentId = r.AppointmentId,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : string.Empty,
                ShopId = r.ShopId,
                ShopName = r.Shop?.Name ?? string.Empty,
                ShopEmployeeId = r.ShopEmployeeId,
                EmployeeName = r.ShopEmployee?.User != null
                    ? $"{r.ShopEmployee.User.FirstName} {r.ShopEmployee.User.LastName}"
                    : string.Empty,
                ServiceName = r.Appointment?.ShopService?.Name ?? string.Empty,
                AppointmentDate = r.Appointment?.StartTime ?? r.CreatedAt,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                ImageUrls = r.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                ServicePrice = r.Appointment?.ShopService?.Price ?? 0
            });

            return Ok(dtos);
        }
    }
}
