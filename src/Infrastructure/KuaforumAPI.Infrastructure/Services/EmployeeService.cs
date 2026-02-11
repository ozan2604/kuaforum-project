using FluentValidation;
using System.Linq;
using System.Threading.Tasks;
using KuaforumAPI.Application.DTOs.Employee;
using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Application.DTOs.Service;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace KuaforumAPI.Infrastructure.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IShopRepository _shopRepository;
        private readonly IGenericRepository<ShopEmployee> _shopEmployeeRepository; // Using generic repo strictly
        private readonly IValidator<CreateEmployeeDto> _validator;
        private readonly ApplicationDbContext _context; // Ideally avoid this, but needed for transaction if not using UnitOfWork

        public EmployeeService(
            UserManager<ApplicationUser> userManager,
            IShopRepository shopRepository,
            IGenericRepository<ShopEmployee> shopEmployeeRepository,
            IValidator<CreateEmployeeDto> validator,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _shopRepository = shopRepository;
            _shopEmployeeRepository = shopEmployeeRepository;
            _validator = validator;
            _context = context;
        }

        public async Task AddEmployeeAsync(string ownerId, CreateEmployeeDto request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new FluentValidation.ValidationException(validationResult.Errors);
            }

            // 1. Get Shop
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null)
            {
                throw new FluentValidation.ValidationException("You must have a shop to add employees.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Create User
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    EmailConfirmed = true // Auto-confirm for simplicity in this flow
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"User creation failed: {errors}");
                }

                // 3. Assign Role
                await _userManager.AddToRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee);

                // 4. Link to Shop
                var shopEmployee = new ShopEmployee
                {
                    ShopId = shop.Id,
                    UserId = user.Id,
                    Title = request.Title,
                    StartDate = DateTime.UtcNow,
                    IsActive = true
                };

                await _shopEmployeeRepository.AddAsync(shopEmployee);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<EmployeeListDto>> GetEmployeesAsync(string ownerId)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) return new List<EmployeeListDto>();

            var employees = await _context.ShopEmployees
                .Where(se => se.ShopId == shop.Id)
                .Include(se => se.User)
                .ToListAsync();

            return employees.Select(e => new EmployeeListDto
            {
                Id = e.Id,
                UserId = e.UserId,
                FirstName = e.User.FirstName,
                LastName = e.User.LastName,
                Email = e.User.Email,
                Title = e.Title,
                IsActive = e.IsActive
            }).ToList();
        }

        public async Task<List<EmployeeListDto>> GetEmployeesByShopIdAsync(Guid shopId)
        {
            var employees = await _context.ShopEmployees
                .Where(se => se.ShopId == shopId && se.IsActive)
                .Include(se => se.User)
                .ToListAsync();

            return employees.Select(e => new EmployeeListDto
            {
                Id = e.Id,
                UserId = e.UserId,
                FirstName = e.User.FirstName,
                LastName = e.User.LastName,
                Email = e.User.Email, // Maybe hide email for public?
                Title = e.Title,
                IsActive = e.IsActive
            }).ToList();
        }

        public async Task AssignServicesAsync(string ownerId, Guid shopEmployeeId, List<Guid> serviceIds)
        {
            // 1. Validate Owner and Employee
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            // Use generic repo to find employee, but ensure it belongs to shop
            // Or use a specific method in ShopEmployeeRepo if existed. 
            // For now, load by ID and check ShopId. 
            var employee = await _shopEmployeeRepository.GetByIdAsync(shopEmployeeId);
            if (employee == null || employee.ShopId != shop.Id)
            {
                throw new FluentValidation.ValidationException("Employee not found in your shop.");
            }

            // 2. Validate Services (Brief check: ensure they belong to shop)
            // Ideally we should check strict validity. 
            // We can fetch all services of the shop and compare IDs.
            var shopServicesCursor = _context.ShopServices.Where(s => s.ShopId == shop.Id && s.IsActive).Select(s => s.Id);
            // Verify all requested IDs exist in shop's services
            // If list is empty, we are clearing assignments, which is valid.
            if (serviceIds != null && serviceIds.Any())
            {
                var validIds = await shopServicesCursor.ToListAsync();
                var invalidIds = serviceIds.Except(validIds).ToList();
                if (invalidIds.Any())
                {
                    throw new FluentValidation.ValidationException($"Invalid services: {string.Join(", ", invalidIds)}");
                }
            }

            // 3. Update Assignments (Full Replacement Strategy)
            // Remove existing
            var existingAssignments = await _context.ShopEmployeeServices
                .Where(ses => ses.ShopEmployeeId == shopEmployeeId)
                .ToListAsync();
            
            _context.ShopEmployeeServices.RemoveRange(existingAssignments);

            // Add new
            if (serviceIds != null && serviceIds.Any())
            {
                var newAssignments = serviceIds.Select(sid => new ShopEmployeeService
                {
                    ShopEmployeeId = shopEmployeeId,
                    ShopServiceId = sid
                });
                await _context.ShopEmployeeServices.AddRangeAsync(newAssignments);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ShopServiceDto>> GetEmployeeServicesAsync(string ownerId, Guid shopEmployeeId)
        {
             // 1. Validate Owner
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            // 2. Validate Employee
            var employee = await _shopEmployeeRepository.GetByIdAsync(shopEmployeeId);
            if (employee == null || employee.ShopId != shop.Id)
            {
                throw new FluentValidation.ValidationException("Employee not found in your shop.");
            }

            // 3. Get Services
            var assignedServices = await _context.ShopEmployeeServices
                .Where(ses => ses.ShopEmployeeId == shopEmployeeId)
                .Include(ses => ses.ShopService)
                .Select(ses => ses.ShopService)
                .ToListAsync();

            // 4. Map
            return assignedServices.Select(s => new ShopServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                Duration = s.Duration,
                IsActive = s.IsActive
            }).ToList();
        }

        public async Task UpdateScheduleAsync(string ownerId, Guid shopEmployeeId, UpdateScheduleDto request)
        {
             // 1. Validate Owner
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            // 2. Validate Employee
            var employee = await _shopEmployeeRepository.GetByIdAsync(shopEmployeeId);
            if (employee == null || employee.ShopId != shop.Id)
            {
                throw new FluentValidation.ValidationException("Employee not found in your shop.");
            }

            // 3. Update Logic (Replace all)
            var existingSchedules = await _context.EmployeeSchedules
                .Where(es => es.ShopEmployeeId == shopEmployeeId)
                .ToListAsync();

            _context.EmployeeSchedules.RemoveRange(existingSchedules);

            if (request.Schedules != null)
            {
                var newSchedules = request.Schedules.Select(s => new EmployeeSchedule
                {
                    ShopEmployeeId = shopEmployeeId,
                    DayOfWeek = (DayOfWeek)s.DayOfWeek,
                    IsWorking = s.IsWorking,
                    StartTime = TimeSpan.Parse(s.StartTime ?? "09:00"), // Default if null, or handle validation
                    EndTime = TimeSpan.Parse(s.EndTime ?? "18:00"),
                    BreakStartTime = string.IsNullOrEmpty(s.BreakStartTime) ? null : TimeSpan.Parse(s.BreakStartTime),
                    BreakEndTime = string.IsNullOrEmpty(s.BreakEndTime) ? null : TimeSpan.Parse(s.BreakEndTime)
                }).ToList();
                
                await _context.EmployeeSchedules.AddRangeAsync(newSchedules);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ScheduleDto>> GetScheduleAsync(string ownerId, Guid shopEmployeeId)
        {
             // 1. Validate Owner
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            // 2. Validate Employee
            var employee = await _shopEmployeeRepository.GetByIdAsync(shopEmployeeId);
            if (employee == null || employee.ShopId != shop.Id)
            {
                throw new FluentValidation.ValidationException("Employee not found in your shop.");
            }

            var schedules = await _context.EmployeeSchedules
                .Where(es => es.ShopEmployeeId == shopEmployeeId)
                .OrderBy(es => es.DayOfWeek)
                .ToListAsync();

            return schedules.Select(s => new ScheduleDto
            {
                DayOfWeek = (int)s.DayOfWeek,
                IsWorking = s.IsWorking,
                StartTime = s.StartTime.ToString(@"hh\:mm"),
                EndTime = s.EndTime.ToString(@"hh\:mm"),
                BreakStartTime = s.BreakStartTime?.ToString(@"hh\:mm"),
                BreakEndTime = s.BreakEndTime?.ToString(@"hh\:mm")
            }).ToList();
        }
    }
}
