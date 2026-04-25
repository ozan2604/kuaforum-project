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
using KuaforumAPI.Infrastructure.Services;
using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.Infrastructure.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IShopRepository _shopRepository;
        private readonly IGenericRepository<ShopEmployee> _shopEmployeeRepository; // Using generic repo strictly
        private readonly IValidator<CreateEmployeeDto> _validator;
        private readonly ApplicationDbContext _context; // Ideally avoid this, but needed for transaction if not using UnitOfWork
        private readonly IDateTimeService _dateTimeService;

        public EmployeeService(
            UserManager<ApplicationUser> userManager,
            IShopRepository shopRepository,
            IGenericRepository<ShopEmployee> shopEmployeeRepository,
            IValidator<CreateEmployeeDto> validator,
            ApplicationDbContext context,
            IDateTimeService dateTimeService)
        {
            _userManager = userManager;
            _shopRepository = shopRepository;
            _shopEmployeeRepository = shopEmployeeRepository;
            _validator = validator;
            _context = context;
            _dateTimeService = dateTimeService;
        }

        public async Task<AddEmployeeResult> AddEmployeeAsync(string ownerId, CreateEmployeeDto request)
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
                // 2. Check if User Exists by Phone
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
                bool isNewUser = false;

                if (user == null)
                {
                    isNewUser = true;
                    // Create New User
                    user = new ApplicationUser
                    {
                        UserName = request.PhoneNumber, // Use phone number as username
                        Email = $"{request.PhoneNumber}@kuaforum.dummy", // Dummy email
                        PhoneNumber = request.PhoneNumber,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true
                    };

                    // Default password for newly created employees
                    var result = await _userManager.CreateAsync(user, "Kuaforum123!");
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"User creation failed: {errors}");
                    }
                }

                // 3. Tek dükkan kuralı: başka aktif dükkanı var mı?
                var activeInOtherShop = await _context.ShopEmployees
                    .AnyAsync(se => se.UserId == user.Id && !se.IsDeleted && se.ShopId != shop.Id);
                if (activeInOtherShop)
                    throw new Exception("Bu kullanıcı başka bir dükkanın aktif çalışanıdır.");

                // 4. Assign Role if not already
                if (!await _userManager.IsInRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee))
                {
                    await _userManager.AddToRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee);
                }

                // 5. Ensure not already an employee in this shop
                var existingEmployee = await _context.ShopEmployees.FirstOrDefaultAsync(se => se.ShopId == shop.Id && se.UserId == user.Id);
                if (existingEmployee != null)
                {
                    if (!existingEmployee.IsDeleted)
                        throw new Exception("Bu kullanıcı zaten dükkanınızda çalışan olarak ekli.");

                    // Silinmiş kayıt varsa reaktive et
                    existingEmployee.IsDeleted = false;
                    existingEmployee.IsActive = true;
                    existingEmployee.Title = request.Title;
                    existingEmployee.StartDate = _dateTimeService.Now;
                    await _shopEmployeeRepository.UpdateAsync(existingEmployee);
                    await transaction.CommitAsync();
                    return new AddEmployeeResult
                    {
                        IsNewUser = isNewUser,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber
                    };
                }

                // 4. Link to Shop
                var shopEmployee = new ShopEmployee
                {
                    ShopId = shop.Id,
                    UserId = user.Id,
                    Title = request.Title,
                    StartDate = _dateTimeService.Now,
                    IsActive = true
                };

                await _shopEmployeeRepository.AddAsync(shopEmployee);

                await transaction.CommitAsync();

                return new AddEmployeeResult
                {
                    IsNewUser = isNewUser,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber
                };
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
                IsActive = e.IsActive,
                IsDeleted = e.IsDeleted
            }).ToList();
        }

        public async Task<List<EmployeeListDto>> GetEmployeesByShopIdAsync(Guid shopId)
        {
            var employees = await _context.ShopEmployees
                .Where(se => se.ShopId == shopId && se.IsActive && !se.IsDeleted)
                .Include(se => se.User)
                .ToListAsync();

            var employeeIds = employees.Select(e => e.Id).ToList();
            var services = await _context.ShopEmployeeServices
                .Where(ses => employeeIds.Contains(ses.ShopEmployeeId))
                .ToListAsync();

            return employees.Select(e => new EmployeeListDto
            {
                Id = e.Id,
                UserId = e.UserId,
                FirstName = e.User.FirstName,
                LastName = e.User.LastName,
                Email = e.User.Email, // Maybe hide email for public?
                Title = e.Title,
                AverageRating = e.AverageRating,
                ReviewCount = e.ReviewCount,
                IsActive = e.IsActive,
                IsDeleted = e.IsDeleted,
                ServiceIds = services.Where(s => s.ShopEmployeeId == e.Id).Select(s => s.ShopServiceId).ToList()
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

        public async Task UpdateEmployeeAsync(string ownerId, Guid shopEmployeeId, UpdateEmployeeOwnerDto request)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shop.Id);

            if (employee == null)
            {
                throw new FluentValidation.ValidationException("Employee not found in your shop.");
            }

            employee.Title = request.Title;
            employee.IsActive = request.IsActive;

            var user = employee.User;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            var identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                throw new FluentValidation.ValidationException("Failed to update user profile.");
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteEmployeeAsync(string ownerId, Guid shopEmployeeId)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shop.Id);
            if (employee == null)
                throw new FluentValidation.ValidationException("Employee not found in your shop.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            employee.IsActive = false;
            employee.IsDeleted = true;
            await _shopEmployeeRepository.UpdateAsync(employee);

            // Çalışan silinince Employee rolü kaldırılır
            var removeResult = await _userManager.RemoveFromRoleAsync(employee.User, KuaforumAPI.Application.Constants.Roles.Employee);
            if (!removeResult.Succeeded)
                throw new FluentValidation.ValidationException("Çalışan rolü kaldırılamadı.");

            await transaction.CommitAsync();
        }

        public async Task RestoreEmployeeAsync(string ownerId, Guid shopEmployeeId)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shop.Id);
            if (employee == null)
                throw new FluentValidation.ValidationException("Employee not found in your shop.");

            if (!employee.IsDeleted)
                throw new Exception("Bu çalışan zaten aktif.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            employee.IsDeleted = false;
            employee.IsActive = true;
            await _shopEmployeeRepository.UpdateAsync(employee);

            // Geri yüklenince Employee rolü yeniden atanır
            if (!await _userManager.IsInRoleAsync(employee.User, KuaforumAPI.Application.Constants.Roles.Employee))
            {
                var addResult = await _userManager.AddToRoleAsync(employee.User, KuaforumAPI.Application.Constants.Roles.Employee);
                if (!addResult.Succeeded)
                    throw new FluentValidation.ValidationException("Çalışan rolü atanamadı.");
            }

            await transaction.CommitAsync();
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

        public async Task<EmployeeProfileDto> GetMyProfileAsync(string userId)
        {
            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .Include(se => se.Shop)
                .FirstOrDefaultAsync(se => se.UserId == userId && se.IsActive);

            if (employee == null)
            {
                // Optionally throw exception or return null if user is not an employee.
                // For now, let's assume if they hit this endpoint (Role = Employee), they SHOULD have a record.
                throw new FluentValidation.ValidationException("Employee profile not found.");
            }

            return new EmployeeProfileDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                ShopId = employee.ShopId,
                ShopName = employee.Shop.Name,
                FirstName = employee.User.FirstName,
                LastName = employee.User.LastName,
                Email = employee.User.Email,
                Title = employee.Title,
                AverageRating = employee.AverageRating,
                ReviewCount = employee.ReviewCount,
                IsActive = employee.IsActive
            };
        }

        public async Task UpdateMyProfileAsync(string userId, UpdateEmployeeProfileDto request)
        {
            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.UserId == userId); // Allow updating even if inactive? Usually yes.

            if (employee == null)
            {
                throw new FluentValidation.ValidationException("Employee profile not found.");
            }

            // Update Employee specific fields
            employee.Title = request.Title;

            // Update User fields
            var user = employee.User;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            // We are using DbContext for ShopEmployee and Identity UserManager for User usually,
            // but since we included User navigation prop and tracked it via context, EF might update it.
            // However, Identity best practice is to use UserManager for user updates to handle security stamps etc.
            // But for simple name change, context update often works if concurrent edits aren't an issue.
            // Let's try standard context save first. 
            // Better: Use UserManager to be safe and consistent with Identity.
            
            var identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                 throw new FluentValidation.ValidationException("Failed to update user profile.");
            }

            // Save changes to ShopEmployee
            await _context.SaveChangesAsync();
        }
    }
}
