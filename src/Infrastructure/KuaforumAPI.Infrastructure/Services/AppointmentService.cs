using FluentValidation;
using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.Appointment;
using Microsoft.Extensions.Logging;
using KuaforumAPI.Application.DTOs.Common;
using KuaforumAPI.Application.DTOs.Service;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using KuaforumAPI.Infrastructure.Services;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace KuaforumAPI.Infrastructure.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ISmsService _smsService;
        private readonly ILogger<AppointmentService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentService(ApplicationDbContext context, IDateTimeService dateTimeService, ISmsService smsService, ILogger<AppointmentService> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _smsService = smsService;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task CreateAsync(string userId, CreateAppointmentDto request)
        {
            // 1. Basic Validations
            var shop = await _context.Shops.FindAsync(request.ShopId);
            if (shop == null || !shop.IsActive) throw new ValidationException("Salon bulunamadı veya aktif değil.");

            var isBlocked = await _context.ShopBlockedCustomers
                .AnyAsync(b => b.ShopId == request.ShopId && b.CustomerId == userId);
            if (isBlocked) throw new ValidationException("Bu salona randevu oluşturamazsınız. Daha önceki randevulara gitmediğiniz tespit edilmiştir. Lütfen İşletme ile iletişime geçiniz.");

            var employee = await _context.ShopEmployees.FindAsync(request.ShopEmployeeId);
            if (employee == null || employee.ShopId != request.ShopId || employee.IsDeleted || !employee.IsActive) throw new ValidationException("Personel bu salonda bulunamadı veya aktif değil.");

            if (request.ServiceIds == null || !request.ServiceIds.Any())
                throw new ValidationException("En az bir hizmet seçilmelidir.");

            var rawStart = _dateTimeService.ToTurkeyTime(request.StartTime);
            // Saniye ve milisaniyeleri temizleyerek dakikaya normalize et (Çakışma kontrolü için kritik)
            var appointmentStart = new DateTime(rawStart.Year, rawStart.Month, rawStart.Day, rawStart.Hour, rawStart.Minute, 0, DateTimeKind.Unspecified);

            // 4. Past date check
            if (appointmentStart < _dateTimeService.Now)
            {
                throw new ValidationException("Geçmiş bir tarihe randevu oluşturamazsınız.");
            }

            // 4a. Booking days ahead check
            var maxBookingDate = _dateTimeService.Now.Date.AddDays(shop.BookingDaysAhead);
            if (appointmentStart.Date > maxBookingDate)
            {
                throw new ValidationException($"Bu salon en fazla {shop.BookingDaysAhead} gün öncesinden randevu kabul etmektedir.");
            }

            // 4b. Shop closure check
            var isClosed = await _context.ShopClosureDates
                .AnyAsync(c => c.ShopId == request.ShopId && c.ClosureDate.Date == appointmentStart.Date);
            if (isClosed)
                throw new ValidationException("Salon bu tarihte kapalıdır.");

            // 4c. Weekly off day check
            if (!string.IsNullOrWhiteSpace(shop.WeeklyOffDays))
            {
                var offDays = shop.WeeklyOffDays.Split(',').Select(int.Parse);
                if (offDays.Contains((int)appointmentStart.DayOfWeek))
                    throw new ValidationException("Salon bu günde haftalık tatildir.");
            }

            // 5. Schedule Check
            var dayOfWeek = appointmentStart.DayOfWeek;
            var schedule = await _context.EmployeeSchedules
                .FirstOrDefaultAsync(es => es.ShopEmployeeId == request.ShopEmployeeId && es.DayOfWeek == dayOfWeek);

            if (schedule == null || !schedule.IsWorking)
            {
                throw new ValidationException("Seçili personel bu gün çalışmıyor.");
            }

            var groupId = Guid.NewGuid();
            var currentStartTime = appointmentStart;

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                foreach (var serviceId in request.ServiceIds)
                {
                    var service = await _context.ShopServices.FindAsync(serviceId);
                    if (service == null || service.ShopId != request.ShopId || service.IsDeleted || !service.IsActive)
                        throw new ValidationException($"Hizmet bulunamadı veya pasif.");

                    var hasSkill = await _context.ShopEmployeeServices
                        .AnyAsync(ses => ses.ShopEmployeeId == request.ShopEmployeeId && ses.ShopServiceId == serviceId);
                    if (!hasSkill) throw new ValidationException("Seçili personel bu hizmetlerden birini sağlayamıyor.");

                    var currentEndTime = currentStartTime.AddMinutes(service.Duration);

                    var timeStart = currentStartTime.TimeOfDay;
                    var timeEnd = currentEndTime.TimeOfDay;

                    if (timeStart < schedule.StartTime || timeEnd > schedule.EndTime)
                    {
                        throw new ValidationException($"Seçilen hizmetler mesai saatleri dışına taşıyor ({schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}).");
                    }

                    if (schedule.BreakStartTime.HasValue && schedule.BreakEndTime.HasValue)
                    {
                        if (timeStart < schedule.BreakEndTime.Value && timeEnd > schedule.BreakStartTime.Value)
                        {
                            throw new ValidationException("Randevu mola saatleri ile çakışıyor.");
                        }
                    }

                    var hasConflict = await _context.Appointments
                        .AnyAsync(a =>
                            a.ShopEmployeeId == request.ShopEmployeeId &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.Status != AppointmentStatus.Rejected &&
                            a.StartTime < currentEndTime &&
                            a.EndTime > currentStartTime);

                    if (hasConflict)
                    {
                        throw new ValidationException("Bu saat dilimi az önce başka bir müşteri tarafından alındı. Lütfen farklı bir saat seçin.");
                    }

                    var appointment = new Appointment
                    {
                        ShopId = request.ShopId,
                        ShopServiceId = serviceId,
                        ShopEmployeeId = request.ShopEmployeeId,
                        UserId = userId,
                        StartTime = currentStartTime,
                        EndTime = currentEndTime,
                        Status = shop.IsAutoProcessEnabled ? AppointmentStatus.Confirmed : AppointmentStatus.Pending,
                        Note = request.Note,
                        GroupId = groupId
                    };

                    await _context.Appointments.AddAsync(appointment);

                    currentStartTime = currentEndTime;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            try
            {
                var customer = await _context.Users.FindAsync(userId);
                var customerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Müşteri";

                if (customer?.PhoneNumber != null)
                {
                    var msg = shop.IsAutoProcessEnabled
                        ? SmsTemplates.AppointmentAutoConfirmed(shop.Name, appointmentStart)
                        : SmsTemplates.AppointmentCreated(shop.Name, appointmentStart);
                    await _smsService.SendSmsAsync(customer.PhoneNumber, msg);
                }

                // Çalışana anlık SMS gönder
                var employeeWithUser = await _context.ShopEmployees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == request.ShopEmployeeId);

                if (employeeWithUser?.User?.PhoneNumber != null)
                {
                    var serviceNames = await _context.ShopServices
                        .Where(s => request.ServiceIds.Contains(s.Id))
                        .Select(s => s.Name)
                        .ToListAsync();
                    var servicesDisplay = serviceNames.Count == 1
                        ? serviceNames[0]
                        : $"{serviceNames.Count} hizmet";
                    await _smsService.SendSmsAsync(
                        employeeWithUser.User.PhoneNumber,
                        SmsTemplates.NewAppointmentForEmployee(customerName, servicesDisplay, appointmentStart));
                  }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }
        }

        public async Task CreateManualAsync(string staffUserId, CreateManualAppointmentDto request)
        {
            // 1. Shop kontrolü — staff bu salona mı ait?
            var shop = await _context.Shops.FindAsync(request.ShopId);
            if (shop == null || !shop.IsActive) throw new ValidationException("Salon bulunamadı veya aktif değil.");

            var isOwner = shop.OwnerId == staffUserId;
            var isEmployee = await _context.ShopEmployees
                .AnyAsync(e => e.ShopId == request.ShopId && e.UserId == staffUserId && !e.IsDeleted && e.IsActive);

            if (!isOwner && !isEmployee)
                throw new ValidationException("Bu salon için randevu oluşturma yetkiniz yok.");

            // 2. Personel kontrolü
            var employee = await _context.ShopEmployees.FindAsync(request.ShopEmployeeId);
            if (employee == null || employee.ShopId != request.ShopId || employee.IsDeleted || !employee.IsActive)
                throw new ValidationException("Personel bu salonda bulunamadı veya aktif değil.");

            if (request.ServiceIds == null || !request.ServiceIds.Any())
                throw new ValidationException("En az bir hizmet seçilmelidir.");

            var rawStart = _dateTimeService.ToTurkeyTime(request.StartTime);
            var appointmentStart = new DateTime(rawStart.Year, rawStart.Month, rawStart.Day, rawStart.Hour, rawStart.Minute, 0, DateTimeKind.Unspecified);

            // 3. Geçmiş tarih yasağı (aynı kural)
            if (appointmentStart < _dateTimeService.Now)
                throw new ValidationException("Geçmiş bir tarihe randevu oluşturamazsınız.");

            // 4. İleriye dönük limit
            var maxBookingDate = _dateTimeService.Now.Date.AddDays(shop.BookingDaysAhead);
            if (appointmentStart.Date > maxBookingDate)
                throw new ValidationException($"Bu salon en fazla {shop.BookingDaysAhead} gün öncesinden randevu kabul etmektedir.");

            // 5. Kapanış tarihi kontrolü
            var isClosed = await _context.ShopClosureDates
                .AnyAsync(c => c.ShopId == request.ShopId && c.ClosureDate.Date == appointmentStart.Date);
            if (isClosed) throw new ValidationException("Salon bu tarihte kapalıdır.");

            // 6. Haftalık tatil kontrolü
            if (!string.IsNullOrWhiteSpace(shop.WeeklyOffDays))
            {
                var offDays = shop.WeeklyOffDays.Split(',').Select(int.Parse);
                if (offDays.Contains((int)appointmentStart.DayOfWeek))
                    throw new ValidationException("Salon bu günde haftalık tatildir.");
            }

            // 7. Personel program kontrolü
            var dayOfWeek = appointmentStart.DayOfWeek;
            var schedule = await _context.EmployeeSchedules
                .FirstOrDefaultAsync(es => es.ShopEmployeeId == request.ShopEmployeeId && es.DayOfWeek == dayOfWeek);

            if (schedule == null || !schedule.IsWorking)
                throw new ValidationException("Seçili personel bu gün çalışmıyor.");

            var guestName = string.IsNullOrWhiteSpace(request.GuestCustomerName) ? null : request.GuestCustomerName.Trim();
            var guestPhone = string.IsNullOrWhiteSpace(request.GuestCustomerPhone) ? null : request.GuestCustomerPhone.Trim();

            var groupId = Guid.NewGuid();
            var currentStartTime = appointmentStart;

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                foreach (var serviceId in request.ServiceIds)
                {
                    var service = await _context.ShopServices.FindAsync(serviceId);
                    if (service == null || service.ShopId != request.ShopId || service.IsDeleted || !service.IsActive)
                        throw new ValidationException("Hizmet bulunamadı veya pasif.");

                    var hasSkill = await _context.ShopEmployeeServices
                        .AnyAsync(ses => ses.ShopEmployeeId == request.ShopEmployeeId && ses.ShopServiceId == serviceId);
                    if (!hasSkill) throw new ValidationException("Seçili personel bu hizmetlerden birini sağlayamıyor.");

                    var currentEndTime = currentStartTime.AddMinutes(service.Duration);
                    var timeStart = currentStartTime.TimeOfDay;
                    var timeEnd = currentEndTime.TimeOfDay;

                    if (timeStart < schedule.StartTime || timeEnd > schedule.EndTime)
                        throw new ValidationException($"Seçilen hizmetler mesai saatleri dışına taşıyor ({schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}).");

                    if (schedule.BreakStartTime.HasValue && schedule.BreakEndTime.HasValue)
                    {
                        if (timeStart < schedule.BreakEndTime.Value && timeEnd > schedule.BreakStartTime.Value)
                            throw new ValidationException("Randevu mola saatleri ile çakışıyor.");
                    }

                    var hasConflict = await _context.Appointments
                        .AnyAsync(a =>
                            a.ShopEmployeeId == request.ShopEmployeeId &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.Status != AppointmentStatus.Rejected &&
                            a.StartTime < currentEndTime &&
                            a.EndTime > currentStartTime);

                    if (hasConflict)
                        throw new ValidationException("Bu saat dilimi dolu. Lütfen farklı bir saat seçin.");

                    var appointment = new Appointment
                    {
                        ShopId = request.ShopId,
                        ShopServiceId = serviceId,
                        ShopEmployeeId = request.ShopEmployeeId,
                        UserId = null,
                        GuestCustomerName = guestName,
                        GuestCustomerPhone = guestPhone,
                        StartTime = currentStartTime,
                        EndTime = currentEndTime,
                        Status = AppointmentStatus.Confirmed,
                        Note = request.Note,
                        GroupId = groupId
                    };

                    await _context.Appointments.AddAsync(appointment);
                    currentStartTime = currentEndTime;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            try
            {
                if (guestPhone != null)
                {
                    var guestDisplayName = guestName ?? "Müşteri";
                    await _smsService.SendSmsAsync(guestPhone, SmsTemplates.AppointmentAutoConfirmed(shop.Name, appointmentStart));
                }

                var employeeWithUser = await _context.ShopEmployees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == request.ShopEmployeeId);

                if (employeeWithUser?.User?.PhoneNumber != null)
                {
                    var serviceNames = await _context.ShopServices
                        .Where(s => request.ServiceIds.Contains(s.Id))
                        .Select(s => s.Name)
                        .ToListAsync();
                    var servicesDisplay = serviceNames.Count == 1 ? serviceNames[0] : $"{serviceNames.Count} hizmet";
                    var displayName = guestName ?? "Misafir";
                    await _smsService.SendSmsAsync(
                        employeeWithUser.User.PhoneNumber,
                        SmsTemplates.NewAppointmentForEmployee(displayName, servicesDisplay, appointmentStart));
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }
        }

        public async Task CreateGuestAsync(CreateGuestAppointmentDto request)
        {
            // 1. Telefon zaten kayıtlı mı?
            var existing = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.CustomerPhone);
            if (existing != null)
                throw new ValidationException("PHONE_EXISTS");

            // 2. Yeni Customer hesabı oluştur
            var nameParts = request.CustomerName.Trim().Split(' ', 2);
            var user = new ApplicationUser
            {
                UserName = request.CustomerPhone,
                PhoneNumber = request.CustomerPhone,
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            };

            var tempPassword = GenerateGuestPassword();
            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
                throw new ValidationException(string.Join(", ", createResult.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.Customer);

            // 3. Randevuyu mevcut CreateAsync mantığıyla kaydet; hata olursa kullanıcıyı geri al
            try
            {
                await CreateAsync(user.Id, new CreateAppointmentDto
                {
                    ShopId = request.ShopId,
                    ServiceIds = request.ServiceIds,
                    ShopEmployeeId = request.ShopEmployeeId,
                    StartTime = request.StartTime,
                    Note = request.Note,
                });
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                throw;
            }

            // 4. Hesap bilgilerini SMS ile gönder (randevu SMS'i CreateAsync içinden gönderildi)
            try
            {
                await _smsService.SendSmsAsync(request.CustomerPhone, SmsTemplates.GuestAccountCreated(tempPassword));
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Misafir hesap SMS gönderilemedi."); }
        }

        private static string GenerateGuestPassword()
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
            return new string(chars.OrderBy(_ => Guid.NewGuid()).ToArray());
        }

        public async Task<PagedResult<AppointmentDto>> GetMyAppointmentsAsync(string userId, int page = 1, int pageSize = 20)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.StartTime);

            var totalCount = await query.CountAsync();

            var appointments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (appointments.Count == 0)
                return new PagedResult<AppointmentDto>(new List<AppointmentDto>(), totalCount, page, pageSize);

            var appointmentIds = appointments.Select(a => a.Id).ToList();
            var reviewedIds = await _context.Reviews
                .Where(r => appointmentIds.Contains(r.AppointmentId))
                .Select(r => r.AppointmentId)
                .ToHashSetAsync();

            var dtos = appointments.Select(a => MapToDto(a, reviewedIds.Contains(a.Id))).ToList();
            return new PagedResult<AppointmentDto>(dtos, totalCount, page, pageSize);
        }

        public async Task<PagedResult<AppointmentDto>> GetShopAppointmentsAsync(string ownerId, Guid shopId, AppointmentStatus? status = null, int page = 1, int pageSize = 10, string? searchTerm = null, DateTime? date = null, Guid? employeeId = null, Guid? serviceId = null)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
             var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == ownerId);
             if (shop == null || shop.Id != shopId) throw new ValidationException("Yetkisiz erişim veya salon bulunamadı.");

             var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Include(a => a.User)
                .Where(a => a.ShopId == shopId);

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerTerm = searchTerm.ToLower();
                query = query.Where(a =>
                    (a.UserId != null && a.User != null && (a.User.FirstName + " " + a.User.LastName).ToLower().Contains(lowerTerm)) ||
                    (a.UserId == null && a.GuestCustomerName != null && a.GuestCustomerName.ToLower().Contains(lowerTerm)) ||
                    (a.UserId == null && a.GuestCustomerPhone != null && a.GuestCustomerPhone.Contains(lowerTerm)) ||
                    a.ShopService.Name.ToLower().Contains(lowerTerm) ||
                    (a.ShopEmployee.Title + " " + (a.ShopEmployee.User != null ? a.ShopEmployee.User.FirstName : "")).ToLower().Contains(lowerTerm)
                );
            }

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                query = query.Where(a => a.StartTime.Date == targetDate);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(a => a.ShopEmployeeId == employeeId.Value);
            }

            if (serviceId.HasValue)
            {
                query = query.Where(a => a.ShopServiceId == serviceId.Value);
            }

            var totalCount = await query.CountAsync();

            var appointments = await query
                .OrderByDescending(a => a.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var appointmentDtos = appointments.Select(a => MapToDto(a)).ToList();

            return new PagedResult<AppointmentDto>(appointmentDtos, totalCount, page, pageSize);
        }

        public async Task<NoShowResultDto?> UpdateStatusAsync(string ownerId, Guid appointmentId, UpdateAppointmentStatusDto request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Shop)
                .Include(a => a.User)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) throw new ValidationException("Appointment not found.");

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == ownerId);
            if (shop == null || shop.Id != appointment.ShopId) throw new ValidationException("Yetkisiz erişim.");

            ValidateStatusTransition(appointment.Status, request.Status);

            if (request.Status == AppointmentStatus.Completed && appointment.StartTime > _dateTimeService.Now)
                throw new ValidationException("Randevu henüz başlamadığı için bu işlem yapılamaz.");

            if (request.Status == AppointmentStatus.NoShow)
            {
                if (appointment.Status == AppointmentStatus.Completed)
                {
                    if (!shop.IsAutoProcessEnabled)
                        throw new ValidationException("Manuel onaylı salonlarda tamamlanmış randevular 'Gelmedi' durumuna çevrilemez.");
                    if (_dateTimeService.Now > appointment.EndTime.AddHours(3))
                        throw new ValidationException("Otomatik tamamlanan randevular, bitiş saatinin üzerinden 3 saat geçtikten sonra 'Gelmedi' durumuna çevrilemez.");
                }
                else if (appointment.StartTime > _dateTimeService.Now)
                {
                    throw new ValidationException("Randevu henüz başlamadığı için bu işlem yapılamaz.");
                }
            }

            appointment.Status = request.Status;
            if ((request.Status == AppointmentStatus.Cancelled || request.Status == AppointmentStatus.Rejected)
                && !string.IsNullOrWhiteSpace(request.Reason))
            {
                appointment.CancellationReason = request.Reason;
            }
            await _context.SaveChangesAsync();

            NoShowResultDto? noShowResult = null;
            if (request.Status == AppointmentStatus.NoShow && appointment.UserId != null)
            {
                var count = await _context.Appointments
                    .CountAsync(a => a.UserId == appointment.UserId && a.ShopId == appointment.ShopId && a.Status == AppointmentStatus.NoShow);
                noShowResult = new NoShowResultDto
                {
                    NoShowCount = count,
                    CustomerId = appointment.UserId,
                    CustomerName = appointment.User != null
                        ? $"{appointment.User.FirstName} {appointment.User.LastName}"
                        : null
                };
            }

            try
            {
                var phone = appointment.User?.PhoneNumber;
                if (phone != null)
                {
                    var msg = request.Status switch
                    {
                        AppointmentStatus.Confirmed  => SmsTemplates.AppointmentConfirmed(appointment.Shop.Name, appointment.StartTime),
                        AppointmentStatus.Rejected   => SmsTemplates.AppointmentRejected(appointment.Shop.Name, appointment.StartTime, request.Reason),
                        AppointmentStatus.Cancelled  => SmsTemplates.AppointmentCancelledByShop(appointment.Shop.Name, appointment.StartTime, request.Reason),
                        AppointmentStatus.Completed  => SmsTemplates.AppointmentCompleted(appointment.Shop.Name),
                        _ => null
                    };
                    if (msg != null)
                        await _smsService.SendSmsAsync(phone, msg);
                }

                var empPhone = appointment.ShopEmployee?.User?.PhoneNumber;
                if (empPhone != null)
                {
                    var empMsg = request.Status switch
                    {
                        AppointmentStatus.Rejected  => SmsTemplates.AppointmentRejectedToEmployee(appointment.Shop.Name, appointment.StartTime),
                        AppointmentStatus.Cancelled => SmsTemplates.AppointmentCancelledByShopToEmployee(appointment.Shop.Name, appointment.StartTime),
                        _ => null
                    };
                    if (empMsg != null)
                        await _smsService.SendSmsAsync(empPhone, empMsg);
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }

            return noShowResult;
        }

        private static bool IsValidTransition(AppointmentStatus current, AppointmentStatus next)
        {
            var allowed = current switch
            {
                AppointmentStatus.Pending   => new[] { AppointmentStatus.Confirmed, AppointmentStatus.Rejected, AppointmentStatus.Cancelled },
                AppointmentStatus.Confirmed => new[] { AppointmentStatus.Completed, AppointmentStatus.Cancelled, AppointmentStatus.NoShow },
                AppointmentStatus.Completed => new[] { AppointmentStatus.NoShow },
                _ => Array.Empty<AppointmentStatus>()
            };
            return allowed.Contains(next);
        }

        private static void ValidateStatusTransition(AppointmentStatus current, AppointmentStatus next)
        {
            if (!IsValidTransition(current, next))
                throw new ValidationException($"'{current}' durumundan '{next}' durumuna geçiş yapılamaz.");
        }

        private AppointmentDto MapToDto(Appointment a, bool hasReview = false)
        {
            return new AppointmentDto
            {
                Id = a.Id,
                ShopId = a.ShopId,
                ShopName = a.Shop.Name,
                ShopServiceId = a.ShopServiceId,
                ServiceName = a.ShopService.Name,
                Price = a.ShopService.Price,
                Duration = a.ShopService.Duration,
                ShopEmployeeId = a.ShopEmployeeId,
                EmployeeName = a.ShopEmployee.Title + " " + (a.ShopEmployee.User != null ? a.ShopEmployee.User.FirstName : ""), 
                UserId = a.UserId,
                CustomerName = a.UserId != null
                    ? (a.User != null ? a.User.FirstName + " " + a.User.LastName : "")
                    : (a.GuestCustomerName ?? "Misafir"),
                CustomerPhone = a.UserId != null ? a.User?.PhoneNumber : a.GuestCustomerPhone,
                IsManual = a.UserId == null,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status,
                Note = a.Note,
                GroupId = a.GroupId,
                HasReview = hasReview,
                CancellationReason = a.CancellationReason,
                ShopCancellationHours = a.Shop?.CancellationHours ?? 2
            };
        }
        public async Task<EmployeeAvailabilityDto> GetEmployeeAvailabilityAsync(Guid employeeId, DateTime date)
        {
            // 0. Shop closure check
            var employee = await _context.ShopEmployees.FindAsync(employeeId);
            if (employee != null)
            {
                var isShopClosed = await _context.ShopClosureDates
                    .AnyAsync(c => c.ShopId == employee.ShopId && c.ClosureDate.Date == date.Date);
                if (isShopClosed)
                    return new EmployeeAvailabilityDto { IsWorking = false, IsShopClosed = true };

                // Weekly off day check
                var shopForOffDays = await _context.Shops.FindAsync(employee.ShopId);
                if (shopForOffDays != null && !string.IsNullOrWhiteSpace(shopForOffDays.WeeklyOffDays))
                {
                    var offDays = shopForOffDays.WeeklyOffDays.Split(',').Select(int.Parse);
                    if (offDays.Contains((int)date.DayOfWeek))
                        return new EmployeeAvailabilityDto { IsWorking = false, IsShopClosed = true };
                }

                // Employee leave date check
                var isOnLeave = await _context.EmployeeLeaveDates
                    .AnyAsync(l => l.ShopEmployeeId == employeeId && l.LeaveDate.Date == date.Date);
                if (isOnLeave)
                    return new EmployeeAvailabilityDto { IsWorking = false, IsOnLeave = true };
            }

            // 1. Get Schedule for the specific day
            var dayOfWeek = date.DayOfWeek;
            var schedule = await _context.EmployeeSchedules
                .FirstOrDefaultAsync(es => es.ShopEmployeeId == employeeId && es.DayOfWeek == dayOfWeek);

            if (schedule == null || !schedule.IsWorking)
            {
                return new EmployeeAvailabilityDto { IsWorking = false };
            }

            // 2. Get Appointments for that day (Turkey Time logic is important here)
            // We need to query appointments where the StartTime falls on this date.
            // Since we store UTC, we need to be careful. 
            // The 'date' parameter is expected to be Midnight of the day in Turkey Time (or Client Local Time).
            
            // Let's assume 'date' is just the date part.
            // We'll define the range in UTC because DB 'StartTime' is likely UTC (or whatever we standardized to).
            // Actually, we standardized to 'ToTurkeyTime' in Create, but EF stores what we give it. 
            // If we standardized, we should have standardized the STORAGE or the INPUT. 
            // Let's assume the DB stores what DateTimeService.ToTurkeyTime returns (which is Unspecified or Local kind with shifted ticks if we just did ConvertTime).
            // Wait, DateTimeService.ToTurkeyTime returns a DateTime with the ticks of Turkey time. 
            // If we save this to Postgres 'timestamp without time zone', it saves those ticks. 
            // So queries should straightforwardly match the date components.

            // DB'de Turkey time saklanır (DateTimeKind.Unspecified).
            // Gelen 'date' parametresinin sadece tarih kısmını alıp gün sınırlarını belirliyoruz.
            var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var endOfDay   = startOfDay.AddDays(1);

            // O gün içinde BAŞLAYAN veya o güne UZANAN randevuları getir
            var appointments = await _context.Appointments
                .Where(a =>
                    a.ShopEmployeeId == employeeId &&
                    a.Status != AppointmentStatus.Cancelled &&
                    a.Status != AppointmentStatus.Rejected &&
                    a.StartTime < endOfDay &&
                    a.EndTime   > startOfDay)
                .Select(a => new TimeSlotDto
                {
                    StartTime = a.StartTime,
                    EndTime   = a.EndTime
                })
                .ToListAsync();

            return new EmployeeAvailabilityDto
            {
                IsWorking = true,
                WorkStartTime = schedule.StartTime,
                WorkEndTime = schedule.EndTime,
                BreakStartTime = schedule.BreakStartTime,
                BreakEndTime = schedule.BreakEndTime,
                BookedSlots = appointments
            };

        }

        public async Task<AppointmentDto> GetReviewableAppointmentAsync(string userId, Guid shopId)
        {
            // Find the most recent COMPLETED appointment for this shop that has NOT been reviewed
            var appointment = await _context.Appointments
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Where(a => 
                    a.UserId == userId && 
                    a.ShopId == shopId && 
                    a.Status == AppointmentStatus.Completed)
                .OrderByDescending(a => a.EndTime) // Most recent first
                .FirstOrDefaultAsync();

            if (appointment == null) return null;

            // Check if it already has a review
            var hasReview = await _context.Reviews.AnyAsync(r => r.AppointmentId == appointment.Id);

            if (hasReview)
            {
                // If the most recent one is reviewed, maybe check others? 
                // Requirement says "allow adding review". Usually user reviews their latest experience.
                // If they have 5 unreviewed past appointments, which one do we pick?
                // Let's iterate or query for the first without review.
                
                // Better query:
                // We can't easily do !Any() in a complex query with join in EF Core sometimes depending on version, 
                // but let's try a subquery approach or just fetch top 5 and filter in memory if needed.
                // Actually, let's try a direct query.
                
                appointment = await _context.Appointments
                    .Include(a => a.Shop)
                    .Include(a => a.ShopService)
                    .Include(a => a.ShopEmployee)
                        .ThenInclude(e => e.User)
                    .Where(a => 
                        a.UserId == userId && 
                        a.ShopId == shopId && 
                        a.Status == AppointmentStatus.Completed &&
                        !_context.Reviews.Any(r => r.AppointmentId == a.Id)) // Subquery
                    .OrderByDescending(a => a.EndTime)
                    .FirstOrDefaultAsync();
            }

            if (appointment == null) return null;

            return MapToDto(appointment, false);
        }

        public async Task<List<AppointmentDto>> GetAssignedAppointmentsAsync(string employeeUserId, DateTime? from = null, DateTime? to = null)
        {
            // Varsayılan: son 30 gün + önümüzdeki 90 gün (takvim görünümü için yeterli)
            var effectiveFrom = from ?? _dateTimeService.Now.AddDays(-30).Date;
            var effectiveTo   = to   ?? _dateTimeService.Now.AddDays(90).Date.AddDays(1);

            var assignedAppointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Include(a => a.User)
                .Where(a => a.ShopEmployee.UserId == employeeUserId
                         && a.StartTime >= effectiveFrom
                         && a.StartTime < effectiveTo)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            return assignedAppointments.Select(a => MapToDto(a)).ToList();
        }

        public async Task<PagedResult<AppointmentDto>> GetAssignedAppointmentsPagedAsync(string employeeUserId, AppointmentStatus? status = null, int page = 1, int pageSize = 10, string? searchTerm = null, DateTime? date = null, Guid? serviceId = null)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Include(a => a.User)
                .Where(a => a.ShopEmployee.UserId == employeeUserId);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(a =>
                    (a.User != null && (a.User.FirstName + " " + a.User.LastName).ToLower().Contains(lower)) ||
                    a.ShopService.Name.ToLower().Contains(lower));
            }

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                query = query.Where(a => a.StartTime.Date == targetDate);
            }

            if (serviceId.HasValue)
                query = query.Where(a => a.ShopServiceId == serviceId.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AppointmentDto>(items.Select(a => MapToDto(a)).ToList(), totalCount, page, pageSize);
        }

        public async Task<NoShowResultDto?> UpdateStatusByEmployeeAsync(string employeeUserId, Guid appointmentId, UpdateAppointmentStatusDto request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.ShopEmployee)
                .Include(a => a.Shop)
                .Include(a => a.User)
                .Include(a => a.ShopService)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) throw new ValidationException("Appointment not found.");

            if (appointment.ShopEmployee.UserId != employeeUserId)
                throw new ValidationException("You are not authorized to manage this appointment.");

            var allowedForEmployee = new[] { AppointmentStatus.Confirmed, AppointmentStatus.Completed, AppointmentStatus.Rejected, AppointmentStatus.Cancelled, AppointmentStatus.NoShow };
            if (!allowedForEmployee.Contains(request.Status))
                throw new ValidationException("Geçersiz durum geçişi.");

            ValidateStatusTransition(appointment.Status, request.Status);

            if (request.Status == AppointmentStatus.Completed && appointment.StartTime > _dateTimeService.Now)
                throw new ValidationException("Randevu henüz başlamadığı için bu işlem yapılamaz.");
            
            if (request.Status == AppointmentStatus.NoShow)
            {
                if (appointment.Status == AppointmentStatus.Completed)
                {
                    if (!appointment.Shop.IsAutoProcessEnabled)
                        throw new ValidationException("Manuel onaylı salonlarda tamamlanmış randevular 'Gelmedi' durumuna çevrilemez.");
                    if (_dateTimeService.Now > appointment.EndTime.AddHours(3))
                        throw new ValidationException("Otomatik tamamlanan randevular, bitiş saatinin üzerinden 3 saat geçtikten sonra 'Gelmedi' durumuna çevrilemez.");
                }
                else if (appointment.StartTime > _dateTimeService.Now)
                {
                    throw new ValidationException("Randevu henüz başlamadığı için bu işlem yapılamaz.");
                }
            }

            if (appointment.GroupId.HasValue)
            {
                var groupAppointments = await _context.Appointments
                    .Include(a => a.ShopEmployee)
                    .Where(a => a.GroupId == appointment.GroupId && a.ShopEmployee.UserId == employeeUserId)
                    .ToListAsync();

                foreach (var apt in groupAppointments)
                {
                    if (apt.Status == request.Status) continue;
                    if (!IsValidTransition(apt.Status, request.Status)) continue;
                    if (request.Status == AppointmentStatus.Completed && apt.StartTime > _dateTimeService.Now) continue;
                    if (request.Status == AppointmentStatus.NoShow)
                    {
                        if (apt.Status == AppointmentStatus.Completed)
                        {
                            if (!apt.Shop.IsAutoProcessEnabled || _dateTimeService.Now > apt.EndTime.AddHours(3)) continue;
                        }
                        else if (apt.StartTime > _dateTimeService.Now) continue;
                    }
                    apt.Status = request.Status;
                }
            }
            else
            {
                appointment.Status = request.Status;
            }

            await _context.SaveChangesAsync();

            NoShowResultDto? noShowResult = null;
            if (request.Status == AppointmentStatus.NoShow && appointment.UserId != null)
            {
                var count = await _context.Appointments
                    .CountAsync(a => a.UserId == appointment.UserId && a.ShopId == appointment.ShopId && a.Status == AppointmentStatus.NoShow);
                noShowResult = new NoShowResultDto
                {
                    NoShowCount = count,
                    CustomerId = appointment.UserId,
                    CustomerName = appointment.User != null
                        ? $"{appointment.User.FirstName} {appointment.User.LastName}"
                        : null
                };
            }

            try
            {
                var phone = appointment.User?.PhoneNumber;
                if (phone != null)
                {
                    var msg = request.Status switch
                    {
                        AppointmentStatus.Confirmed => SmsTemplates.AppointmentConfirmed(appointment.Shop.Name, appointment.StartTime),
                        AppointmentStatus.Completed => SmsTemplates.AppointmentCompleted(appointment.Shop.Name),
                        _ => null
                    };
                    if (msg != null)
                        await _smsService.SendSmsAsync(phone, msg);
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }

            return noShowResult;
        }

        public async Task CancelGroupAsync(string userId, Guid groupId, string? reason = null)
        {
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Where(a => a.GroupId == groupId && a.UserId == userId)
                .ToListAsync();

            if (!appointments.Any()) throw new ValidationException("Grup randevusu bulunamadı.");

            var now = _dateTimeService.Now;
            var first = appointments.OrderBy(a => a.StartTime).First();

            var shop = await _context.Shops.FindAsync(first.ShopId);
            var cancellationHours = shop?.CancellationHours ?? 2;

            if ((first.StartTime - now).TotalHours < cancellationHours)
                throw new ValidationException($"Randevuya {cancellationHours} saatten az kaldığı için iptal edilemez.");

            foreach (var apt in appointments)
            {
                if (apt.Status != AppointmentStatus.Pending && apt.Status != AppointmentStatus.Confirmed)
                    continue;
                apt.Status = AppointmentStatus.Cancelled;
                if (!string.IsNullOrWhiteSpace(reason))
                    apt.CancellationReason = reason;
            }

            await _context.SaveChangesAsync();

            try
            {
                var customer = first.User;
                var customerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Müşteri";
                var serviceName = first.ShopService?.Name ?? "";

                if (shop?.PhoneNumber != null)
                {
                    await _smsService.SendSmsAsync(
                        shop.PhoneNumber,
                        SmsTemplates.AppointmentCancelledByCustomer(customerName, serviceName, first.StartTime));
                }

                var notifiedEmployees = new HashSet<string>();
                foreach (var apt in appointments)
                {
                    var empPhone = apt.ShopEmployee?.User?.PhoneNumber;
                    if (empPhone != null && notifiedEmployees.Add(empPhone))
                    {
                        await _smsService.SendSmsAsync(
                            empPhone,
                            SmsTemplates.AppointmentCancelledByCustomerToEmployee(customerName, serviceName, first.StartTime));
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }
        }

        public async Task<NoShowResultDto?> UpdateGroupStatusAsync(string ownerId, Guid groupId, UpdateAppointmentStatusDto request)
        {
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == ownerId);
            if (shop == null) throw new ValidationException("Yetkisiz erişim.");

            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Where(a => a.GroupId == groupId && a.ShopId == shop.Id)
                .ToListAsync();

            if (!appointments.Any()) throw new ValidationException("Grup randevusu bulunamadı.");

            foreach (var apt in appointments)
            {
                if (apt.Status == request.Status) continue;
                if (!IsValidTransition(apt.Status, request.Status)) continue;
                if (request.Status == AppointmentStatus.Completed && apt.StartTime > _dateTimeService.Now) continue;
                if (request.Status == AppointmentStatus.NoShow)
                {
                    if (apt.Status == AppointmentStatus.Completed)
                    {
                        if (!shop.IsAutoProcessEnabled || _dateTimeService.Now > apt.EndTime.AddHours(3)) continue;
                    }
                    else if (apt.StartTime > _dateTimeService.Now) continue;
                }

                apt.Status = request.Status;
                if ((request.Status == AppointmentStatus.Cancelled || request.Status == AppointmentStatus.Rejected)
                    && !string.IsNullOrWhiteSpace(request.Reason))
                {
                    apt.CancellationReason = request.Reason;
                }
            }

            await _context.SaveChangesAsync();

            NoShowResultDto? noShowResult = null;
            if (request.Status == AppointmentStatus.NoShow)
            {
                var first = appointments.OrderBy(a => a.StartTime).FirstOrDefault();
                if (first?.UserId != null)
                {
                    var count = await _context.Appointments
                        .CountAsync(a => a.UserId == first.UserId && a.ShopId == shop.Id && a.Status == AppointmentStatus.NoShow);
                    noShowResult = new NoShowResultDto
                    {
                        NoShowCount = count,
                        CustomerId = first.UserId,
                        CustomerName = first.User != null
                            ? $"{first.User.FirstName} {first.User.LastName}"
                            : null
                    };
                }
            }

            try
            {
                var first = appointments.OrderBy(a => a.StartTime).FirstOrDefault();
                var phone = first?.User?.PhoneNumber;
                if (phone != null)
                {
                    var msg = request.Status switch
                    {
                        AppointmentStatus.Confirmed => SmsTemplates.AppointmentConfirmed(shop.Name, first.StartTime),
                        AppointmentStatus.Rejected  => SmsTemplates.AppointmentRejected(shop.Name, first.StartTime, request.Reason),
                        AppointmentStatus.Cancelled => SmsTemplates.AppointmentCancelledByShop(shop.Name, first.StartTime, request.Reason),
                        AppointmentStatus.Completed => SmsTemplates.AppointmentCompleted(shop.Name),
                        _ => null
                    };
                    if (msg != null)
                        await _smsService.SendSmsAsync(phone, msg);
                }

                var empPhone = first?.ShopEmployee?.User?.PhoneNumber;
                if (empPhone != null && first != null)
                {
                    var empMsg = request.Status switch
                    {
                        AppointmentStatus.Rejected  => SmsTemplates.AppointmentRejectedToEmployee(shop.Name, first.StartTime),
                        AppointmentStatus.Cancelled => SmsTemplates.AppointmentCancelledByShopToEmployee(shop.Name, first.StartTime),
                        _ => null
                    };
                    if (empMsg != null)
                        await _smsService.SendSmsAsync(empPhone, empMsg);
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }

            return noShowResult;
        }

        public async Task CancelByCustomerAsync(string userId, Guid appointmentId, string? reason = null)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) throw new ValidationException("Randevu bulunamadı.");

            if (appointment.UserId != userId) throw new ValidationException("Bu randevuyu iptal etme yetkiniz yok.");

            if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Confirmed)
            {
                throw new ValidationException("Yalnızca bekleyen veya onaylanmış randevular iptal edilebilir.");
            }

            var shop = await _context.Shops.FindAsync(appointment.ShopId);
            var cancellationHours = shop?.CancellationHours ?? 2;

            var now = _dateTimeService.Now;
            if ((appointment.StartTime - now).TotalHours < cancellationHours)
            {
                throw new ValidationException($"Randevuya {cancellationHours} saatten az kaldığı için iptal edilemez.");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                appointment.CancellationReason = reason;
            }
            await _context.SaveChangesAsync();

            try
            {
                var customerName = appointment.User != null
                    ? $"{appointment.User.FirstName} {appointment.User.LastName}"
                    : "Müşteri";
                var serviceName = appointment.ShopService?.Name ?? "";

                if (shop?.PhoneNumber != null)
                {
                    await _smsService.SendSmsAsync(
                        shop.PhoneNumber,
                        SmsTemplates.AppointmentCancelledByCustomer(customerName, serviceName, appointment.StartTime));
                }

                if (appointment.ShopEmployee?.User?.PhoneNumber != null)
                {
                    await _smsService.SendSmsAsync(
                        appointment.ShopEmployee.User.PhoneNumber,
                        SmsTemplates.AppointmentCancelledByCustomerToEmployee(customerName, serviceName, appointment.StartTime));
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }
        }
    }
}
