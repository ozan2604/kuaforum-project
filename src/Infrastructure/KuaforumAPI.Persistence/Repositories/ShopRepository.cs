using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace KuaforumAPI.Persistence.Repositories
{
    public class ShopRepository : GenericRepository<Shop>, IShopRepository
    {
        private readonly ApplicationDbContext _context;

        public ShopRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Shop> GetByOwnerIdAsync(string ownerId)
        {
            return await _context.Shops
                .Include(s => s.Categories)
                .Include(s => s.ClosureDates)
                .FirstOrDefaultAsync(s => s.OwnerId == ownerId);
        }

        public override async Task<Shop> GetByIdAsync(Guid id)
        {
            return await _context.Shops
                .Include(s => s.Categories)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateShopCategoriesAsync(Guid shopId, List<int> categoryValues)
        {
            var existing = await _context.ShopCategoryAssignments
                .Where(c => c.ShopId == shopId)
                .ToListAsync();
            _context.ShopCategoryAssignments.RemoveRange(existing);

            foreach (var val in categoryValues)
                _context.ShopCategoryAssignments.Add(new ShopCategoryAssignment { ShopId = shopId, CategoryValue = val });

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Shop>> GetAllWithDetailsAsync(string? city = null, string? district = null, string? neighborhood = null)
        {
            var query = _context.Shops.Include(s => s.Owner).Include(s => s.Categories).AsQueryable();

            if (!string.IsNullOrEmpty(city))
                query = query.Where(s => s.City.ToLower() == city.ToLower());

            if (!string.IsNullOrEmpty(district))
                query = query.Where(s => s.District.ToLower() == district.ToLower());

            if (!string.IsNullOrEmpty(neighborhood))
                query = query.Where(s => s.Neighborhood.ToLower() == neighborhood.ToLower());

            return await query.ToListAsync();
        }

        public async Task<(List<Shop> Items, int TotalCount)> GetPagedWithDetailsAsync(
            string? city, string? district, string? neighborhood, int pageNumber, int pageSize)
        {
            var query = _context.Shops
                .Include(s => s.Owner)
                .Include(s => s.Categories)
                .Where(s => s.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(city))
                query = query.Where(s => s.City.ToLower() == city.ToLower());
            if (!string.IsNullOrEmpty(district))
                query = query.Where(s => s.District.ToLower() == district.ToLower());
            if (!string.IsNullOrEmpty(neighborhood))
                query = query.Where(s => s.Neighborhood.ToLower() == neighborhood.ToLower());

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<List<string>> DeleteShopWithDependenciesAsync(Guid shopId)
        {
            var imagesToDelete = new List<string>();

            // 1. Get Shop Cover Image
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == shopId);
            if (shop != null && !string.IsNullOrEmpty(shop.CoverImagePath))
            {
                imagesToDelete.Add(shop.CoverImagePath);
            }

            // 2. Get Shop Gallery Images
            var galleryImages = await _context.ShopImages.Where(si => si.ShopId == shopId).ToListAsync();
            imagesToDelete.AddRange(galleryImages.Select(si => si.Url));

            // 3. Get Review Images
            var reviewImages = await _context.ReviewImages
                .Include(ri => ri.Review)
                .Where(ri => ri.Review.ShopId == shopId)
                .ToListAsync();
            imagesToDelete.AddRange(reviewImages.Select(ri => ri.Url));

            // Now delete entities in order to avoid foreign key conflicts

            // Delete Review Images (Cascade does this, but to be safe we can let EF handle or do it manually)
            _context.ReviewImages.RemoveRange(reviewImages);

            // Delete Reviews
            var reviews = await _context.Reviews.Where(r => r.ShopId == shopId).ToListAsync();
            _context.Reviews.RemoveRange(reviews);

            // Delete Appointments
            var appointments = await _context.Appointments.Where(a => a.ShopId == shopId).ToListAsync();
            _context.Appointments.RemoveRange(appointments);

            // Delete Employee Schedules
            var employeeSchedules = await _context.EmployeeSchedules
                .Include(es => es.ShopEmployee)
                .Where(es => es.ShopEmployee.ShopId == shopId)
                .ToListAsync();
            _context.EmployeeSchedules.RemoveRange(employeeSchedules);

            // Delete Shop Employee Services
            var employeeServices = await _context.ShopEmployeeServices
                .Include(ses => ses.ShopEmployee)
                .Where(ses => ses.ShopEmployee.ShopId == shopId)
                .ToListAsync();
            _context.ShopEmployeeServices.RemoveRange(employeeServices);

            // Delete Shop Employees
            var employees = await _context.ShopEmployees.Where(se => se.ShopId == shopId).ToListAsync();
            _context.ShopEmployees.RemoveRange(employees);

            // Delete Shop Services
            var services = await _context.ShopServices.Where(ss => ss.ShopId == shopId).ToListAsync();
            _context.ShopServices.RemoveRange(services);

            // Delete Service Categories
            var categories = await _context.ServiceCategories.Where(sc => sc.ShopId == shopId).ToListAsync();
            _context.ServiceCategories.RemoveRange(categories);

            // Delete User Favorite Shops
            var favorites = await _context.UserFavoriteShops.Where(f => f.ShopId == shopId).ToListAsync();
            _context.UserFavoriteShops.RemoveRange(favorites);

            // Delete Shop Images
            _context.ShopImages.RemoveRange(galleryImages);

            // Delete Applications
            var applications = await _context.SalonOwnerApplications.Where(a => a.UserId == shop.OwnerId).ToListAsync();
            _context.SalonOwnerApplications.RemoveRange(applications);

            // Finally delete the shop
            if (shop != null)
            {
                _context.Shops.Remove(shop);
            }

            await _context.SaveChangesAsync();

            return imagesToDelete;
        }
    }
}
