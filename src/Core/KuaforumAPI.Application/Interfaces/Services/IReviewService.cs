using KuaforumAPI.Application.DTOs.Review;
using KuaforumAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IReviewService
    {
        Task<Review> AddReviewAsync(CreateReviewDto createReviewDto, string userId);
        Task<IEnumerable<Review>> GetShopReviewsAsync(Guid shopId, string? currentUserId = null);
        Task<(List<Review> Items, int TotalCount)> GetShopReviewsPagedAsync(Guid shopId, string? currentUserId, int pageNumber, int pageSize);
        Task<Review> UpdateReviewAsync(UpdateReviewDto updateReviewDto, string userId);
        Task DeleteReviewAsync(Guid reviewId, string userId);
        Task<IEnumerable<Review>> GetMyReviewsAsync(string userId);
        Task<IEnumerable<Review>> GetMyShopReviewsAsync(string? ownerId, Guid shopId);
    }
}
