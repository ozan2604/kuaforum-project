using FluentValidation;
using KuaforumAPI.Application.DTOs.Block;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KuaforumAPI.Infrastructure.Services
{
    public class ShopBlockService : IShopBlockService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShopBlockService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task BlockCustomerAsync(string? staffUserId, Guid shopId, BlockCustomerDto dto)
        {
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null || !shop.IsActive) throw new ValidationException("Salon bulunamadı.");

            if (staffUserId != null)
            {
                var isOwner = shop.OwnerId == staffUserId;
                var isEmployee = await _context.ShopEmployees
                    .AnyAsync(e => e.ShopId == shopId && e.UserId == staffUserId && !e.IsDeleted && e.IsActive);
                if (!isOwner && !isEmployee)
                    throw new ValidationException("Bu salon için müşteri engelleme yetkiniz yok.");
            }

            var alreadyBlocked = await _context.ShopBlockedCustomers
                .AnyAsync(b => b.ShopId == shopId && b.CustomerId == dto.CustomerId);
            if (alreadyBlocked) return;

            var block = new ShopBlockedCustomer
            {
                ShopId = shopId,
                CustomerId = dto.CustomerId,
                Reason = dto.Reason?.Trim(),
                BlockedByUserId = staffUserId
            };
            await _context.ShopBlockedCustomers.AddAsync(block);
            await _context.SaveChangesAsync();
        }

        public async Task UnblockCustomerAsync(string? ownerId, Guid shopId, string customerId)
        {
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == shopId && (ownerId == null || s.OwnerId == ownerId));
            if (shop == null) throw new ValidationException("Yetkisiz erişim.");

            var block = await _context.ShopBlockedCustomers
                .FirstOrDefaultAsync(b => b.ShopId == shopId && b.CustomerId == customerId);
            if (block == null) return;

            _context.ShopBlockedCustomers.Remove(block);
            await _context.SaveChangesAsync();
        }

        public async Task<List<BlockedCustomerDto>> GetBlockedCustomersAsync(string? ownerId, Guid shopId)
        {
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == shopId && (ownerId == null || s.OwnerId == ownerId));
            if (shop == null) throw new ValidationException("Yetkisiz erişim.");

            return await _context.ShopBlockedCustomers
                .Where(b => b.ShopId == shopId)
                .Include(b => b.Customer)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BlockedCustomerDto
                {
                    Id = b.Id,
                    CustomerId = b.CustomerId,
                    CustomerName = b.Customer.FirstName + " " + b.Customer.LastName,
                    CustomerPhone = b.Customer.PhoneNumber,
                    Reason = b.Reason,
                    BlockedAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<CustomerShopInfoDto?> GetCustomerByPhoneAsync(string? ownerId, Guid shopId, string phone)
        {
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == shopId && (ownerId == null || s.OwnerId == ownerId));
            if (shop == null) throw new ValidationException("Yetkisiz erişim.");

            var normalizedPhone = phone.Trim();
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);

            if (user == null) return null;

            var appointments = await _context.Appointments
                .Where(a => a.ShopId == shopId && a.UserId == user.Id)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee).ThenInclude(e => e.User)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(r => r.ShopId == shopId && r.UserId == user.Id)
                .Include(r => r.ShopEmployee).ThenInclude(e => e.User)
                .Include(r => r.Appointment).ThenInclude(a => a.ShopService)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var isBlocked = await _context.ShopBlockedCustomers
                .AnyAsync(b => b.ShopId == shopId && b.CustomerId == user.Id);

            var completedAppointments = appointments.Where(a => a.Status == AppointmentStatus.Completed).ToList();

            return new CustomerShopInfoDto
            {
                CustomerId = user.Id,
                CustomerName = $"{user.FirstName} {user.LastName}",
                CustomerPhone = user.PhoneNumber,
                CustomerEmail = user.Email,
                TotalAppointments = appointments.Count,
                CompletedCount = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                CancelledCount = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                RejectedCount = appointments.Count(a => a.Status == AppointmentStatus.Rejected),
                NoShowCount = appointments.Count(a => a.Status == AppointmentStatus.NoShow),
                PendingCount = appointments.Count(a => a.Status == AppointmentStatus.Pending),
                ConfirmedCount = appointments.Count(a => a.Status == AppointmentStatus.Confirmed),
                TotalSpent = completedAppointments.Sum(a => a.ShopService?.Price ?? 0),
                IsBlocked = isBlocked,
                Reviews = reviews.Select(r => new CustomerReviewSummaryDto
                {
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    EmployeeName = r.ShopEmployee?.User != null
                        ? $"{r.ShopEmployee.User.FirstName} {r.ShopEmployee.User.LastName}"
                        : null,
                    ServiceName = r.Appointment?.ShopService?.Name
                }).ToList(),
                RecentAppointments = appointments.Take(20).Select(a => new CustomerAppointmentSummaryDto
                {
                    ServiceName = a.ShopService?.Name ?? "",
                    Price = a.ShopService?.Price ?? 0,
                    StartTime = a.StartTime,
                    Status = a.Status,
                    EmployeeName = a.ShopEmployee?.User != null
                        ? $"{a.ShopEmployee.User.FirstName} {a.ShopEmployee.User.LastName}"
                        : null
                }).ToList()
            };
        }
    }
}
