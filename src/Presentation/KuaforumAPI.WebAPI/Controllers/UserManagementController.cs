using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = KuaforumAPI.Application.Constants.Roles.Admin)]
    public class UserManagementController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly KuaforumAPI.Application.Interfaces.Services.IShopService _shopService;
        private readonly KuaforumAPI.Application.Interfaces.Services.IImageService _imageService;

        public UserManagementController(
            UserManager<ApplicationUser> userManager, 
            ApplicationDbContext context,
            KuaforumAPI.Application.Interfaces.Services.IShopService shopService,
            KuaforumAPI.Application.Interfaces.Services.IImageService imageService)
        {
            _userManager = userManager;
            _context = context;
            _shopService = shopService;
            _imageService = imageService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string search = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (pageSize > 100) pageSize = 100;
            var query = _userManager.Users.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(u => 
                    (u.FirstName != null && u.FirstName.ToLower().Contains(lowerSearch)) || 
                    (u.LastName != null && u.LastName.ToLower().Contains(lowerSearch)) || 
                    (u.Email != null && u.Email.ToLower().Contains(lowerSearch)) || 
                    (u.UserName != null && u.UserName.ToLower().Contains(lowerSearch)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
                );
            }

            var totalCount = await query.CountAsync();
            
            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            // Tek sorguda tüm roller
            var userRoles = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name }
            ).ToListAsync();

            // Tek sorguda sahip olunan salonlar
            var ownedShopsAll = await _context.Shops
                .Where(s => userIds.Contains(s.OwnerId))
                .Select(s => new { s.OwnerId, s.Name })
                .ToListAsync();

            // Tek sorguda çalışan olunan salonlar
            var employedShopsAll = await _context.ShopEmployees
                .Where(se => userIds.Contains(se.UserId))
                .Select(se => new { se.UserId, ShopName = se.Shop.Name })
                .ToListAsync();

            var usersWithRoles = users.Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.UserName,
                Roles = userRoles.Where(r => r.UserId == u.Id).Select(r => r.RoleName).ToList(),
                OwnedShops = ownedShopsAll.Where(s => s.OwnerId == u.Id).Select(s => s.Name).ToList(),
                EmployedShops = employedShopsAll.Where(e => e.UserId == u.Id).Select(e => e.ShopName).ToList()
            }).ToList();

            return Ok(new { TotalCount = totalCount, Users = usersWithRoles });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            // Güvenlik: Admin kullanıcıları sistemden silinemez
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return BadRequest(new { message = "Sistem yöneticileri (Admin) silinemez." });
            } 
            
            // 1. If user is a shop owner, delete their shop completely
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == id);
            if (shop != null)
            {
                await _shopService.DeleteShopAsync(shop.Id);
            }

            // 2. Delete Reviews by user
            var reviews = await _context.Reviews.Include(r => r.Images).Where(r => r.UserId == id).ToListAsync();
            foreach (var r in reviews)
            {
                foreach (var img in r.Images)
                {
                    try { await _imageService.DeleteImageAsync(img.Url); } catch { }
                }
            }
            _context.Reviews.RemoveRange(reviews);

            // 3. Delete Appointments by user
            var appointments = await _context.Appointments.Where(a => a.UserId == id).ToListAsync();
            _context.Appointments.RemoveRange(appointments);

            // 4. Delete Shop Employee references if the user is an employee
            var shopEmployee = await _context.ShopEmployees.FirstOrDefaultAsync(se => se.UserId == id);
            if (shopEmployee != null)
            {
                var schedules = await _context.EmployeeSchedules.Where(es => es.ShopEmployeeId == shopEmployee.Id).ToListAsync();
                _context.EmployeeSchedules.RemoveRange(schedules);
                
                var services = await _context.ShopEmployeeServices.Where(ses => ses.ShopEmployeeId == shopEmployee.Id).ToListAsync();
                _context.ShopEmployeeServices.RemoveRange(services);

                var employeeAppointments = await _context.Appointments.Where(a => a.ShopEmployeeId == shopEmployee.Id).ToListAsync();
                _context.Appointments.RemoveRange(employeeAppointments);
                
                var employeeReviews = await _context.Reviews.Include(r => r.Images).Where(r => r.ShopEmployeeId == shopEmployee.Id).ToListAsync();
                foreach (var r in employeeReviews)
                {
                    foreach (var img in r.Images) { try { await _imageService.DeleteImageAsync(img.Url); } catch { } }
                }
                _context.Reviews.RemoveRange(employeeReviews);

                _context.ShopEmployees.Remove(shopEmployee);
            }

            // 5. Delete Salon Owner Applications
            var applications = await _context.SalonOwnerApplications.Where(a => a.UserId == id).ToListAsync();
            _context.SalonOwnerApplications.RemoveRange(applications);

            // 6. Delete Favorites
            var favorites = await _context.UserFavoriteShops.Where(f => f.CircleUserId == id).ToListAsync();
            _context.UserFavoriteShops.RemoveRange(favorites);



            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(new { Message = "Failed to delete user.", Errors = result.Errors });

            return Ok(new { Message = "User deleted successfully." });
        }
    }
}
