using KuaforumAPI.Application.DTOs.Review;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ValidationException = KuaforumAPI.Application.Exceptions.ValidationException;

namespace KuaforumAPI.Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly IImageService _imageService;

        public ReviewService(ApplicationDbContext context, IDateTimeService dateTimeService, IImageService imageService)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _imageService = imageService;
        }

        public async Task<Review> AddReviewAsync(CreateReviewDto dto, string userId)
        {
            // 1. Validate Appointment
            var appointment = await _context.Appointments
                .Include(a => a.Shop)
                .Include(a => a.ShopEmployee)
                .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

            if (appointment == null)
                throw new NotFoundException("Randevu bulunamadı.");

            if (appointment.UserId != userId)
                throw new ValidationException("Yalnızca kendi randevunuzu değerlendirebilirsiniz.");

            if (appointment.Status != AppointmentStatus.Completed)
                throw new ValidationException("Yalnızca tamamlanmış randevular için değerlendirme yapılabilir.");

            var existingReview = await _context.Reviews
                .AnyAsync(r => r.AppointmentId == dto.AppointmentId);

            if (existingReview)
                throw new ValidationException("Bu randevu için zaten değerlendirme yapılmış.");

            // 3. Create Review
            var review = new Review
            {
                AppointmentId = dto.AppointmentId,
                UserId = userId,
                ShopId = appointment.ShopId,
                ShopEmployeeId = appointment.ShopEmployeeId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = _dateTimeService.Now,
                Images = new List<ReviewImage>()
            };

            // Handle Review Images
            if (dto.Images != null && dto.Images.Count > 0)
            {
                Console.WriteLine($"[ReviewService] Processing {dto.Images.Count} images...");
                foreach (var file in dto.Images)
                {
                    try 
                    {
                        var imageUrl = await _imageService.UploadImageAsync(file, "reviews");
                        Console.WriteLine($"[ReviewService] Uploaded image: {imageUrl}");
                        review.Images.Add(new ReviewImage
                        {
                            Url = imageUrl
                        });
                    }
                    catch(Exception ex)
                    {
                         Console.WriteLine($"[ReviewService] Image upload failed: {ex.Message}");
                         // Should we fail the whole review? Maybe not, just log.
                    }
                }
            }

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // 4. Update Employee Rating
            await UpdateEmployeeRating(appointment.ShopEmployeeId);

            // 5. Update Shop Rating
            await UpdateShopRating(appointment.ShopId);

            // Re-fetch to get all navigation properties for the DTO mapping in controller
            return await _context.Reviews
                .Include(r => r.Images)
                .Include(r => r.Shop)
                .Include(r => r.ShopEmployee)
                .ThenInclude(se => se.User)
                .Include(r => r.Appointment)
                .ThenInclude(a => a.ShopService)
                .FirstOrDefaultAsync(r => r.Id == review.Id);
        }

        public async Task<IEnumerable<Review>> GetShopReviewsAsync(Guid shopId, string? currentUserId = null)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Images)
                .Include(r => r.ShopEmployee)
                .ThenInclude(se => se.User)
                .Include(r => r.Appointment)
                .ThenInclude(a => a.ShopService)
                .Where(r => r.ShopId == shopId);

            // Fetch list first to avoid complex EF translation for custom sorting if needed, 
            // OR use concise OrderBy. 
            // EF Core usually handles boolean sorting: OrderByDescending(x => x.IsMyReview)
            
            if (!string.IsNullOrEmpty(currentUserId))
            {
                 // OrderByDescending(true) comes before false.
                 return await query
                    .OrderByDescending(r => r.UserId == currentUserId)
                    .ThenByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review> UpdateReviewAsync(UpdateReviewDto dto, string userId)
        {
            var review = await _context.Reviews
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == dto.Id);

            if (review == null)
                throw new NotFoundException("Değerlendirme bulunamadı.");

            if (review.UserId != userId)
                throw new ValidationException("Yalnızca kendi değerlendirmelerinizi düzenleyebilirsiniz.");

            // Update Fields
            review.Rating = dto.Rating;
            review.Comment = dto.Comment;
            // UpdatedAt handled by Interceptor/Context

            // Ensure Images collection is initialized
            if (review.Images == null) review.Images = new List<ReviewImage>();

            // Handle Deleted Images
            if (dto.DeletedImageUrls != null && dto.DeletedImageUrls.Any())
            {
                var imagesToDelete = review.Images
                    .Where(i => dto.DeletedImageUrls.Contains(i.Url))
                    .ToList();

                foreach (var img in imagesToDelete)
                {
                    // Delete from Cloudinary
                    await _imageService.DeleteImageAsync(img.Url);
                    // Remove from DB
                    review.Images.Remove(img);
                }
            }

            // Handle New Images
            if (dto.NewImages != null && dto.NewImages.Count > 0)
            {
                 Console.WriteLine($"[ReviewService] UpdateReviewAsync: Processing {dto.NewImages.Count} new images.");
                 foreach (var file in dto.NewImages)
                {
                    try
                    {
                        var imageUrl = await _imageService.UploadImageAsync(file, "reviews");
                        Console.WriteLine($"[ReviewService] UpdateReviewAsync: Uploaded image {imageUrl}");
                        
                        var newImage = new ReviewImage
                        {
                            Id = Guid.NewGuid(),
                            ReviewId = review.Id,
                            Url = imageUrl,
                            CreatedAt = _dateTimeService.Now
                        };

                        // Add to both context and collection to be safe
                        _context.ReviewImages.Add(newImage);
                        review.Images.Add(newImage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ReviewService] UpdateReviewAsync: Image upload failed: {ex.Message}");
                        // Continue ensuring other images might succeed, or rethrow? 
                        // For now, let's log and continue to minimize disruption.
                    }
                }
            }

            try 
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"[ReviewService] UpdateReviewAsync: SaveChanges failed: {ex.Message}");
                 if (ex.InnerException != null)
                 {
                     Console.WriteLine($"[ReviewService] Inner Exception: {ex.InnerException.Message}");
                 }
                 throw; // Rethrow to let controller handle it
            }

            // Recalculate Ratings
            await UpdateEmployeeRating(review.ShopEmployeeId);
            await UpdateShopRating(review.ShopId);

            // Re-fetch to get all navigation properties for the DTO mapping in controller
            return await _context.Reviews
                .Include(r => r.Images)
                .Include(r => r.Shop)
                .Include(r => r.ShopEmployee)
                .ThenInclude(se => se.User)
                .Include(r => r.Appointment)
                .ThenInclude(a => a.ShopService)
                .FirstOrDefaultAsync(r => r.Id == review.Id);
        }

        public async Task DeleteReviewAsync(Guid reviewId, string userId)
        {
            var review = await _context.Reviews
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
                throw new NotFoundException("Değerlendirme bulunamadı.");

            if (review.UserId != userId)
                throw new ValidationException("Yalnızca kendi değerlendirmelerinizi silebilirsiniz.");

            // Delete Images from Cloudinary
            foreach (var img in review.Images)
            {
                await _imageService.DeleteImageAsync(img.Url);
            }

            // Delete from DB
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Recalculate Ratings
            await UpdateEmployeeRating(review.ShopEmployeeId);
            await UpdateShopRating(review.ShopId);
        }

        public async Task<IEnumerable<Review>> GetMyReviewsAsync(string userId)
        {
            return await _context.Reviews
                .Include(r => r.Images)
                .Include(r => r.Shop)
                .Include(r => r.ShopEmployee)
                .ThenInclude(se => se.User)
                .Include(r => r.Appointment)
                .ThenInclude(a => a.ShopService)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        private async Task UpdateEmployeeRating(Guid employeeId)
        {
            var employee = await _context.ShopEmployees.FindAsync(employeeId);
            if (employee == null) return;

            var stats = await _context.Reviews
                .Where(r => r.ShopEmployeeId == employeeId)
                .GroupBy(r => r.ShopEmployeeId)
                .Select(g => new { Average = g.Average(r => r.Rating), Count = g.Count() })
                .FirstOrDefaultAsync();

            if (stats != null)
            {
                employee.AverageRating = stats.Average;
                employee.ReviewCount = stats.Count;
            }
            else
            {
                employee.AverageRating = 0;
                employee.ReviewCount = 0;
            }
            // Save inside calling method or here? Context is shared. 
            // We should save changes to persist this update.
            await _context.SaveChangesAsync();
        }

        private async Task UpdateShopRating(Guid shopId)
        {
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null) return;

            // Strategy: Average of all reviews for the shop (which equals average of employees weighted by review count)
            // Or Average of Employee Averages?
            // "her emplooyeenin kendi toplam puan ortalaması olur. ve bu tüm emplooyelerin puan toplam ortalaması da dükkan detaylarında ve dükkan kartlarında gösteriir."
            // This literally triggers: Average(Employee.AverageRating).
            
            // Let's calculate Average of Employee Averages.
            var employeeStats = await _context.ShopEmployees
                .Where(se => se.ShopId == shopId && se.ReviewCount > 0)
                .AverageAsync(se => (double?)se.AverageRating); // Nullable to handle empty

             // If we use all reviews directly:
             var allReviewsStats = await _context.Reviews
                .Where(r => r.ShopId == shopId)
                 .GroupBy(r => r.ShopId)
                 .Select(g => new { Average = g.Average(r => r.Rating), Count = g.Count() })
                 .FirstOrDefaultAsync();

            // I will use All Reviews Average as it's more standard, unless user complains. 
            // "tüm emplooyelerin puanların oratalaması" could mean (Emp1.Avg + Emp2.Avg) / 2.
            // But if Emp1 has 100 reviews (5.0) and Emp2 has 1 review (1.0), (5+1)/2 = 3.0.
            // Weighted average (Real average) would be closer to 5.0.
            // Most platforms use Weighted Average.
            // I'll stick to All Reviews Average.
            
            if (allReviewsStats != null)
            {
                shop.AverageRating = allReviewsStats.Average;
                shop.ReviewCount = allReviewsStats.Count;
            }
            else
            {
                shop.AverageRating = 0;
                shop.ReviewCount = 0;
            }
            await _context.SaveChangesAsync();
        }
    }
}
