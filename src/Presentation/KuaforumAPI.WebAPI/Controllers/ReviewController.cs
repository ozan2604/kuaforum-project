using KuaforumAPI.Application.DTOs.Review;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> AddReview([FromForm] CreateReviewDto createReviewDto)
        {
            try
            {
                Console.WriteLine($"[ReviewController] AddReview called. Images count: {createReviewDto.Images?.Count ?? 0}");
                
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var review = await _reviewService.AddReviewAsync(createReviewDto, userId);
                
                // Map to DTO to avoid circular references during serialization
                var reviewDto = new ReviewListDto
                {
                    Id = review.Id,
                    AppointmentId = review.AppointmentId,
                    UserId = review.UserId,
                    UserName = User.Identity.Name, 
                    ShopId = review.ShopId,
                    ShopName = review.Shop?.Name ?? "Unknown Shop",
                    ShopEmployeeId = review.ShopEmployeeId,
                    EmployeeName = review.ShopEmployee?.User != null ? $"{review.ShopEmployee.User.FirstName} {review.ShopEmployee.User.LastName}" : "Unknown Employee",
                    ServiceName = review.Appointment?.ShopService?.Name ?? "Unknown Service",
                    AppointmentDate = review.Appointment?.StartTime ?? review.CreatedAt,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    ImageUrls = review.Images?.Select(i => i.Url).ToList() ?? new List<string>()
                };

                return Ok(reviewDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReviewController] Error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
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
                ImageUrls = r.Images?.Select(i => i.Url).ToList() ?? new List<string>()
            });

            return Ok(dtos);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(Guid id, [FromForm] UpdateReviewDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest("ID mismatch");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var updatedReview = await _reviewService.UpdateReviewAsync(dto, userId);

                // Map to DTO
                var reviewDto = new ReviewListDto
                {
                    Id = updatedReview.Id,
                    AppointmentId = updatedReview.AppointmentId,
                    UserId = updatedReview.UserId,
                    UserName = User.Identity.Name, // Or null if not critical
                    ShopId = updatedReview.ShopId,
                    ShopName = updatedReview.Shop?.Name ?? "Unknown Shop",
                    ShopEmployeeId = updatedReview.ShopEmployeeId,
                    EmployeeName = updatedReview.ShopEmployee?.User != null ? $"{updatedReview.ShopEmployee.User.FirstName} {updatedReview.ShopEmployee.User.LastName}" : "Unknown Employee",
                    ServiceName = updatedReview.Appointment?.ShopService?.Name ?? "Unknown Service",
                    AppointmentDate = updatedReview.Appointment?.StartTime ?? updatedReview.CreatedAt,
                    Rating = updatedReview.Rating,
                    Comment = updatedReview.Comment,
                    CreatedAt = updatedReview.CreatedAt,
                    ImageUrls = updatedReview.Images?.Select(i => i.Url).ToList() ?? new List<string>()
                };

                return Ok(reviewDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReviewController] Update error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _reviewService.DeleteReviewAsync(id, userId);
                return Ok(new { message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-reviews")]
        [Authorize]
        public async Task<IActionResult> GetMyReviews()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var reviews = await _reviewService.GetMyReviewsAsync(userId);
                
                var dtos = reviews.Select(r => new ReviewListDto
                {
                    Id = r.Id,
                    AppointmentId = r.AppointmentId,
                    UserId = r.UserId,
                    UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Me",
                    ShopId = r.ShopId,
                    ShopName = r.Shop?.Name ?? "Unknown Shop",
                    ShopEmployeeId = r.ShopEmployeeId,
                    EmployeeName = r.ShopEmployee != null && r.ShopEmployee.User != null 
                        ? $"{r.ShopEmployee.User.FirstName} {r.ShopEmployee.User.LastName}" 
                        : "Unknown Employee",
                    ServiceName = r.Appointment?.ShopService?.Name ?? "Unknown Service",
                    AppointmentDate = r.Appointment?.StartTime ?? r.CreatedAt,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    ImageUrls = r.Images?.Select(i => i.Url).ToList() ?? new List<string>()
                });

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
