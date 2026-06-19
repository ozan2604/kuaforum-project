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

        public async Task<AddEmployeeResult> AddEmployeeAsync(Guid shopId, string ownerId, CreateEmployeeDto request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                throw new FluentValidation.ValidationException(validationResult.Errors);

            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new KuaforumAPI.Application.Exceptions.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
                bool isNewUser = false;
                string? tempPassword = null;

                if (user == null)
                {
                    isNewUser = true;
                    user = new ApplicationUser
                    {
                        UserName = request.PhoneNumber,
                        Email = $"{request.PhoneNumber}@salonbir.dummy",
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

                // Tek dükkan kuralı: başka aktif dükkanı var mı?
                var activeInOtherShop = await _context.ShopEmployees
                    .AnyAsync(se => se.UserId == user.Id && !se.IsDeleted && se.ShopId != shopId);
                if (activeInOtherShop)
                    throw new KuaforumAPI.Application.Exceptions.ValidationException("Bu kullanıcı başka bir dükkanın aktif çalışanıdır.");

                if (!await _userManager.IsInRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee))
                    await _userManager.AddToRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee);

                var existingEmployee = await _context.ShopEmployees.FirstOrDefaultAsync(se => se.ShopId == shopId && se.UserId == user.Id);
                if (existingEmployee != null)
                {
                    if (!existingEmployee.IsDeleted)
                        throw new KuaforumAPI.Application.Exceptions.ValidationException("Bu kullanıcı zaten dükkanınızda çalışan olarak ekli.");

                    existingEmployee.IsDeleted = false;
                    existingEmployee.IsActive = true;
                    existingEmployee.Title = request.Title;
                    existingEmployee.StartDate = _dateTimeService.Now;
                    await _shopEmployeeRepository.UpdateAsync(existingEmployee);

                    if (!await _userManager.IsInRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee))
                        await _userManager.AddToRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Employee);

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

                var shopEmployee = new ShopEmployee
                {
                    ShopId = shopId,
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

        public async Task<List<EmployeeListDto>> GetEmployeesAsync(Guid shopId, string ownerId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId)) return new List<EmployeeListDto>();

            var employees = await _context.ShopEmployees
                .Where(se => se.ShopId == shopId)
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

        public async Task AssignServicesAsync(Guid shopId, string ownerId, Guid shopEmployeeId, List<Guid> serviceIds)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _shopEmployeeRepository.GetByIdAsync(shopEmployeeId);
            if (employee == null || employee.ShopId != shopId)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

            var shopServicesCursor = _context.ShopServices.Where(s => s.ShopId == shopId && s.IsActive && !s.IsDeleted).Select(s => s.Id);
            if (serviceIds != null && serviceIds.Any())
            {
                var validIds = await shopServicesCursor.ToListAsync();
                var invalidIds = serviceIds.Except(validIds).ToList();
                if (invalidIds.Any())
                    throw new FluentValidation.ValidationException($"Geçersiz hizmetler: {string.Join(", ", invalidIds)}");
            }

            var existingAssignments = await _context.ShopEmployeeServices
                .Where(ses => ses.ShopEmployeeId == shopEmployeeId)
                .ToListAsync();

            _context.ShopEmployeeServices.RemoveRange(existingAssignments);

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

        public async Task<List<ShopServiceDto>> GetEmployeeServicesAsync(Guid shopId, string ownerId, Guid shopEmployeeId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _shopEmployeeRepository.GetByIdAsync(shopEmployeeId);
            if (employee == null || employee.ShopId != shopId)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

            var assignedServices = await _context.ShopEmployeeServices
                .Where(ses => ses.ShopEmployeeId == shopEmployeeId)
                .Include(ses => ses.ShopService)
                .Select(ses => ses.ShopService)
                .ToListAsync();

            return assignedServices.Select(s => new ShopServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                Duration = s.Duration,
                IsActive = s.IsActive
            }).ToList();
        }

        public async Task UpdateEmployeeAsync(Guid shopId, string ownerId, Guid shopEmployeeId, UpdateEmployeeOwnerDto request)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shopId);

            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

            employee.Title = request.Title;
            employee.IsActive = request.IsActive;

            var user = employee.User;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            var identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
                throw new FluentValidation.ValidationException("Kullanıcı profili güncellenemedi.");

            await _context.SaveChangesAsync();
        }

        public async Task DeleteEmployeeAsync(Guid shopId, string ownerId, Guid shopEmployeeId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shopId);
            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            employee.IsActive = false;
            employee.IsDeleted = true;
            await _shopEmployeeRepository.UpdateAsync(employee);

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

        public async Task RestoreEmployeeAsync(Guid shopId, string ownerId, Guid shopEmployeeId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shopId);
            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

            if (!employee.IsDeleted)
                throw new KuaforumAPI.Application.Exceptions.ValidationException("Bu çalışan zaten aktif.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            employee.IsDeleted = false;
            employee.IsActive = true;
            await _shopEmployeeRepository.UpdateAsync(employee);

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

        public async Task UpdateScheduleAsync(Guid shopId, string ownerId, Guid shopEmployeeId, UpdateScheduleDto request)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _shopEmployeeRepository.GetByIdAsync(shopEmployeeId);
            if (employee == null || employee.ShopId != shopId)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

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
                    StartTime = TimeSpan.Parse(s.StartTime ?? "09:00"),
                    EndTime = TimeSpan.Parse(s.EndTime ?? "18:00"),
                    BreakStartTime = string.IsNullOrEmpty(s.BreakStartTime) ? null : TimeSpan.Parse(s.BreakStartTime),
                    BreakEndTime = string.IsNullOrEmpty(s.BreakEndTime) ? null : TimeSpan.Parse(s.BreakEndTime)
                }).ToList();

                await _context.EmployeeSchedules.AddRangeAsync(newSchedules);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ScheduleDto>> GetScheduleAsync(Guid shopId, string ownerId, Guid shopEmployeeId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _shopEmployeeRepository.GetByIdAsync(shopEmployeeId);
            if (employee == null || employee.ShopId != shopId)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

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
                throw new FluentValidation.ValidationException("Çalışan profili bulunamadı.");

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
                .FirstOrDefaultAsync(se => se.UserId == userId);

            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan profili bulunamadı.");

            employee.Title = request.Title;

            var user = employee.User;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            var identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
                throw new FluentValidation.ValidationException("Kullanıcı profili güncellenemedi.");

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

        public async Task<List<EmployeeLeaveDateDto>> GetLeaveDatesAsync(Guid shopId, string ownerId, Guid shopEmployeeId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _context.ShopEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shopId);
            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

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

        public async Task AddLeaveDateAsync(Guid shopId, string ownerId, Guid shopEmployeeId, string leaveDate, string? reason)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var employee = await _context.ShopEmployees
                .FirstOrDefaultAsync(se => se.Id == shopEmployeeId && se.ShopId == shopId);
            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan bu salonda bulunamadı.");

            if (!DateTime.TryParseExact(leaveDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
                throw new FluentValidation.ValidationException("Geçersiz tarih formatı. Beklenen: yyyy-MM-dd");

            if (parsedDate.Date < DateTime.Today)
                throw new FluentValidation.ValidationException("Geçmiş bir tarihe izin günü eklenemez.");

            var alreadyExists = await _context.EmployeeLeaveDates
                .AnyAsync(l => l.ShopEmployeeId == shopEmployeeId && l.LeaveDate.Date == parsedDate.Date);
            if (alreadyExists)
                throw new FluentValidation.ValidationException("Bu çalışan için bu tarihte zaten izin tanımlanmış.");

            await _context.EmployeeLeaveDates.AddAsync(new EmployeeLeaveDate
            {
                ShopEmployeeId = shopEmployeeId,
                LeaveDate = parsedDate,
                Reason = reason
            });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveLeaveDateAsync(Guid shopId, string ownerId, Guid leaveDateId)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId);
            if (shop == null || (ownerId != null && shop.OwnerId != ownerId))
                throw new FluentValidation.ValidationException("Salon bulunamadı veya yetkiniz yok.");

            var leave = await _context.EmployeeLeaveDates
                .Include(l => l.ShopEmployee)
                .FirstOrDefaultAsync(l => l.Id == leaveDateId);

            if (leave == null)
                throw new FluentValidation.ValidationException("İzin günü bulunamadı.");
            if (leave.ShopEmployee.ShopId != shopId)
                throw new FluentValidation.ValidationException("Yetkisiz erişim.");

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
            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan profili bulunamadı.");

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
            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan profili bulunamadı.");

            if (!DateTime.TryParseExact(leaveDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
                throw new FluentValidation.ValidationException("Geçersiz tarih formatı. Beklenen: yyyy-MM-dd");

            if (parsedDate.Date < DateTime.Today)
                throw new FluentValidation.ValidationException("Geçmiş bir tarihe izin günü eklenemez.");

            var alreadyExists = await _context.EmployeeLeaveDates
                .AnyAsync(l => l.ShopEmployeeId == employee.Id && l.LeaveDate.Date == parsedDate.Date);
            if (alreadyExists)
                throw new FluentValidation.ValidationException("Bu tarihte zaten izin günü tanımlı.");

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
            if (employee == null)
                throw new FluentValidation.ValidationException("Çalışan profili bulunamadı.");

            var leave = await _context.EmployeeLeaveDates
                .FirstOrDefaultAsync(l => l.Id == leaveDateId && l.ShopEmployeeId == employee.Id);
            if (leave == null)
                throw new FluentValidation.ValidationException("İzin günü bulunamadı.");

            _context.EmployeeLeaveDates.Remove(leave);
            await _context.SaveChangesAsync();
        }
    }
}
