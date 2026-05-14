using FluentValidation;
using System.Linq;
using System.Threading.Tasks;
using KuaforumAPI.Application.Constants;
using Microsoft.Extensions.Logging;
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
        private readonly IGenericRepository<ShopEmployee> _shopEmployeeRepository;
        private readonly IValidator<CreateEmployeeDto> _validator;
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ISmsService _smsService;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            UserManager<ApplicationUser> userManager,
            IShopRepository shopRepository,
            IGenericRepository<ShopEmployee> shopEmployeeRepository,
            IValidator<CreateEmployeeDto> validator,
            ApplicationDbContext context,
            IDateTimeService dateTimeService,
            ISmsService smsService,
            ILogger<EmployeeService> logger)
        {
            _userManager = userManager;
            _shopRepository = shopRepository;
            _shopEmployeeRepository = shopEmployeeRepository;
            _validator = validator;
            _context = context;
            _dateTimeService = dateTimeService;
            _smsService = smsService;
            _logger = logger;
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
                string? tempPassword = null;

                if (user == null)
                {
                    isNewUser = true;
                    // Create New User
                    user = new ApplicationUser
                    {
                        UserName = request.PhoneNumber,
                        Email = $"{request.PhoneNumber}@kuaforum.dummy",
                        PhoneNumber = request.PhoneNumber,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true
                    };

                    tempPassword = GenerateTemporaryPassword();
                    var result = await _userManager.CreateAsync(user, tempPassword);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new KuaforumAPI.Application.Exceptions.ValidationException($"Kullanıcı oluşturulamadı: {errors}");
                    }
                }

                // 3. Tek dükkan kuralı: başka aktif dükkanı var mı?
                var activeInOtherShop = await _context.ShopEmployees
                    .AnyAsync(se => se.UserId == user.Id && !se.IsDeleted && se.ShopId != shop.Id);
                if (activeInOtherShop)
                    throw new KuaforumAPI.Application.Exceptions.ValidationException("Bu kullanıcı başka bir dükkanın aktif çalışanıdır.");

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
                        throw new KuaforumAPI.Application.Exceptions.ValidationException("Bu kullanıcı zaten dükkanınızda çalışan olarak ekli.");

                    // Silinmiş kayıt varsa reaktive et
                    existingEmployee.IsDeleted = false;
                    existingEmployee.IsActive = true;
                    existingEmployee.Title = request.Title;
                    existingEmployee.StartDate = _dateTimeService.Now;
                    await _shopEmployeeRepository.UpdateAsync(existingEmployee);
                    
                    if (!await _userManager.IsInRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee))
                    {
                        await _userManager.AddToRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee);
                    }

                    await transaction.CommitAsync();

                    try
                    {
                        if (user.PhoneNumber != null)
                            await _smsService.SendSmsAsync(user.PhoneNumber, SmsTemplates.EmployeeAddedExisting(shop.Name));
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }

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

                try
                {
                    if (user.PhoneNumber != null)
                    {
                        var msg = isNewUser && tempPassword != null
                            ? SmsTemplates.EmployeeAdded(shop.Name, tempPassword)
                            : SmsTemplates.EmployeeAddedExisting(shop.Name);
                        await _smsService.SendSmsAsync(user.PhoneNumber, msg);
                    }
                }
                catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }

                return new AddEmployeeResult
                {
                    IsNewUser = isNewUser,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    TemporaryPassword = isNewUser ? tempPassword : null
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$";

            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            char Pick(string s) { var b = new byte[1]; rng.GetBytes(b); return s[b[0] % s.Length]; }

            var chars = new[]
            {
                Pick(upper), Pick(upper),
                Pick(lower), Pick(lower), Pick(lower),
                Pick(digits), Pick(digits),
                Pick(special)
            };

            var orderBytes = new byte[chars.Length * 4];
            rng.GetBytes(orderBytes);
            return new string(chars.Select((c, i) => (c, BitConverter.ToInt32(orderBytes, i * 4)))
                                   .OrderBy(x => x.Item2)
                                   .Select(x => x.c)
                                   .ToArray());
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
                Email = null,
                Title = e.Title,
                AverageRating = e.AverageRating,
                ReviewCount = e.ReviewCount,
                IsActive = e.IsActive,
                IsDeleted = e.IsDeleted,
                ServiceIds = services.Where(s => s.ShopEmployeeId == e.Id).Select(s => s.ShopServiceId).ToList(),
                ImageUrl = e.User.ProfileImageUrl
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
            var shopServicesCursor = _context.ShopServices.Where(s => s.ShopId == shop.Id && s.IsActive && !s.IsDeleted).Select(s => s.Id);
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

            try
            {
                if (employee.User?.PhoneNumber != null)
                    await _smsService.SendSmsAsync(employee.User.PhoneNumber, SmsTemplates.EmployeeRemoved(shop.Name));
            }
            catch { }
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
                throw new KuaforumAPI.Application.Exceptions.ValidationException("Bu çalışan zaten aktif.");

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

            try
            {
                if (employee.User?.PhoneNumber != null)
                    await _smsService.SendSmsAsync(employee.User.PhoneNumber, SmsTemplates.EmployeeRestored(shop.Name));
            }
            catch { }
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
                .FirstOrDefaultAsync(se => se.UserId == userId && se.IsActive && !se.IsDeleted);

            if (employee == null)
            {
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
                IsActive = employee.IsActive,
                BookingDaysAhead = employee.Shop.BookingDaysAhead
            };
        }

        public async Task<List<PublicEmployeeScheduleDto>> GetPublicShopSchedulesAsync(Guid shopId)
        {
            var employees = await _context.ShopEmployees
                .Where(se => se.ShopId == shopId && se.IsActive && !se.IsDeleted)
                .Include(se => se.User)
                .Include(se => se.Schedules)
                .ToListAsync();

            return employees.Select(e => new PublicEmployeeScheduleDto
            {
                EmployeeId = e.Id,
                FirstName = e.User.FirstName,
                LastName = e.User.LastName,
                Title = e.Title,
                Schedule = e.Schedules
                    .OrderBy(s => s.DayOfWeek)
                    .Select(s => new ScheduleDto
                    {
                        DayOfWeek = (int)s.DayOfWeek,
                        IsWorking = s.IsWorking,
                        StartTime = s.StartTime.ToString(@"hh\:mm"),
                        EndTime = s.EndTime.ToString(@"hh\:mm"),
                        BreakStartTime = s.BreakStartTime?.ToString(@"hh\:mm"),
                        BreakEndTime = s.BreakEndTime?.ToString(@"hh\:mm")
                    }).ToList()
            }).ToList();
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

        public async Task<List<ScheduleDto>> GetMyScheduleAsync(string userId)
        {
            var employee = await _context.ShopEmployees
                .FirstOrDefaultAsync(se => se.UserId == userId && se.IsActive && !se.IsDeleted);

            if (employee == null)
                throw new NotFoundException("Çalışan kaydı bulunamadı.");

            var schedules = await _context.EmployeeSchedules
                .Where(es => es.ShopEmployeeId == employee.Id)
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

        public async Task UpdateMyScheduleAsync(string userId, UpdateScheduleDto request)
        {
            var employee = await _context.ShopEmployees
                .FirstOrDefaultAsync(se => se.UserId == userId && se.IsActive && !se.IsDeleted);

            if (employee == null)
                throw new NotFoundException("Çalışan kaydı bulunamadı.");

            var existing = await _context.EmployeeSchedules
                .Where(es => es.ShopEmployeeId == employee.Id)
                .ToListAsync();

            _context.EmployeeSchedules.RemoveRange(existing);

            if (request.Schedules != null)
            {
                var newSchedules = request.Schedules.Select(s => new EmployeeSchedule
                {
                    ShopEmployeeId = employee.Id,
                    DayOfWeek = (DayOfWeek)s.DayOfWeek,
                    IsWorking = s.IsWorking,
                    StartTime = TimeSpan.Parse(s.StartTime ?? "09:00"),
                    EndTime = TimeSpan.Parse(s.EndTime ?? "18:00"),
                    BreakStartTime = string.IsNullOrEmpty(s.BreakStartTime) ? null : TimeSpan.Parse(s.BreakStartTime),
                    BreakEndTime = string.IsNullOrEmpty(s.BreakEndTime) ? null : TimeSpan.Parse(s.BreakEndTime)
                }).ToList();

                await _context.EmployeeSchedules.AddRangeAsync(newSchedules);
            }

            await _context.SaveChangesAsync();
        }

        // ─── LEAVE DATES ─────────────────────────────────────────────────────────

        public async Task<List<EmployeeLeaveDateDto>> GetLeaveDatesAsync(string ownerId, Guid shopEmployeeId)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shop.Id);
            if (employee == null) throw new FluentValidation.ValidationException("Employee not found in your shop.");

            var leaveDates = await _context.EmployeeLeaveDates
                .Where(l => l.ShopEmployeeId == shopEmployeeId)
                .OrderBy(l => l.LeaveDate)
                .ToListAsync();

            return leaveDates.Select(l => new EmployeeLeaveDateDto
            {
                Id = l.Id,
                ShopEmployeeId = l.ShopEmployeeId,
                EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}",
                LeaveDate = l.LeaveDate.ToString("yyyy-MM-dd"),
                Reason = l.Reason
            }).ToList();
        }

        public async Task AddLeaveDateAsync(string ownerId, Guid shopEmployeeId, string leaveDate, string? reason)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            var employee = await _context.ShopEmployees
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shop.Id);
            if (employee == null) throw new FluentValidation.ValidationException("Employee not found in your shop.");

            if (!DateTime.TryParseExact(leaveDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
                throw new FluentValidation.ValidationException("Geçersiz tarih formatı. Beklenen: yyyy-MM-dd");

            if (parsedDate.Date < DateTime.Today)
                throw new FluentValidation.ValidationException("Geçmiş bir tarihe izin günü eklenemez.");

            var alreadyExists = await _context.EmployeeLeaveDates
                .AnyAsync(l => l.ShopEmployeeId == shopEmployeeId && l.LeaveDate.Date == parsedDate.Date);
            if (alreadyExists) throw new FluentValidation.ValidationException("Bu çalışan için bu tarihte zaten izin tanımlanmış.");

            var leave = new EmployeeLeaveDate
            {
                ShopEmployeeId = shopEmployeeId,
                LeaveDate = parsedDate,
                Reason = reason
            };

            await _context.EmployeeLeaveDates.AddAsync(leave);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveLeaveDateAsync(string ownerId, Guid leaveDateId)
        {
            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new FluentValidation.ValidationException("Shop not found.");

            var leave = await _context.EmployeeLeaveDates
                .Include(l => l.ShopEmployee)
                .FirstOrDefaultAsync(l => l.Id == leaveDateId);

            if (leave == null) throw new FluentValidation.ValidationException("İzin günü bulunamadı.");
            if (leave.ShopEmployee.ShopId != shop.Id) throw new FluentValidation.ValidationException("Yetkisiz erişim.");

            _context.EmployeeLeaveDates.Remove(leave);
            await _context.SaveChangesAsync();
        }

        public async Task<List<EmployeeLeaveDateDto>> GetPublicEmployeeLeaveDatesAsync(Guid shopEmployeeId)
        {
            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.IsActive && !se.IsDeleted);
            if (employee == null) return new List<EmployeeLeaveDateDto>();

            var leaveDates = await _context.EmployeeLeaveDates
                .Where(l => l.ShopEmployeeId == shopEmployeeId && l.LeaveDate >= DateTime.Today)
                .OrderBy(l => l.LeaveDate)
                .ToListAsync();

            return leaveDates.Select(l => new EmployeeLeaveDateDto
            {
                Id = l.Id,
                ShopEmployeeId = l.ShopEmployeeId,
                EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}",
                LeaveDate = l.LeaveDate.ToString("yyyy-MM-dd"),
                Reason = l.Reason
            }).ToList();
        }

        // ─── Self-managed leave dates (Employee) ─────────────────────────────

        public async Task<List<EmployeeLeaveDateDto>> GetMyLeaveDatesAsync(string userId)
        {
            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.UserId == userId && se.IsActive && !se.IsDeleted);
            if (employee == null) throw new FluentValidation.ValidationException("Çalışan profili bulunamadı.");

            var leaveDates = await _context.EmployeeLeaveDates
                .Where(l => l.ShopEmployeeId == employee.Id)
                .OrderBy(l => l.LeaveDate)
                .ToListAsync();

            return leaveDates.Select(l => new EmployeeLeaveDateDto
            {
                Id = l.Id,
                ShopEmployeeId = l.ShopEmployeeId,
                EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}",
                LeaveDate = l.LeaveDate.ToString("yyyy-MM-dd"),
                Reason = l.Reason
            }).ToList();
        }

        public async Task AddMyLeaveDateAsync(string userId, string leaveDate, string? reason)
        {
            var employee = await _context.ShopEmployees
                .FirstOrDefaultAsync(se => se.UserId == userId && se.IsActive && !se.IsDeleted);
            if (employee == null) throw new FluentValidation.ValidationException("Çalışan profili bulunamadı.");

            if (!DateTime.TryParseExact(leaveDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
                throw new FluentValidation.ValidationException("Geçersiz tarih formatı. Beklenen: yyyy-MM-dd");

            if (parsedDate.Date < DateTime.Today)
                throw new FluentValidation.ValidationException("Geçmiş bir tarihe izin günü eklenemez.");

            var alreadyExists = await _context.EmployeeLeaveDates
                .AnyAsync(l => l.ShopEmployeeId == employee.Id && l.LeaveDate.Date == parsedDate.Date);
            if (alreadyExists) throw new FluentValidation.ValidationException("Bu tarihte zaten izin günü tanımlı.");

            await _context.EmployeeLeaveDates.AddAsync(new EmployeeLeaveDate
            {
                ShopEmployeeId = employee.Id,
                LeaveDate = parsedDate,
                Reason = reason
            });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMyLeaveDateAsync(string userId, Guid leaveDateId)
        {
            var employee = await _context.ShopEmployees
                .FirstOrDefaultAsync(se => se.UserId == userId && se.IsActive && !se.IsDeleted);
            if (employee == null) throw new FluentValidation.ValidationException("Çalışan profili bulunamadı.");

            var leave = await _context.EmployeeLeaveDates
                .FirstOrDefaultAsync(l => l.Id == leaveDateId && l.ShopEmployeeId == employee.Id);
            if (leave == null) throw new FluentValidation.ValidationException("İzin günü bulunamadı.");

            _context.EmployeeLeaveDates.Remove(leave);
            await _context.SaveChangesAsync();
        }
    }
}
