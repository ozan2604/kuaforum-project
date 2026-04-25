using FluentValidation;
using KuaforumAPI.Application.DTOs.Appointment;
using KuaforumAPI.Application.DTOs.Common;
using KuaforumAPI.Application.DTOs.Service;
using KuaforumAPI.Application.Interfaces.Repositories;
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

namespace KuaforumAPI.Infrastructure.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IShopRepository _shopRepository;
        private readonly IDateTimeService _dateTimeService;

        public AppointmentService(ApplicationDbContext context, IShopRepository shopRepository, IDateTimeService dateTimeService)
        {
            _context = context;
            _shopRepository = shopRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task CreateAsync(string userId, CreateAppointmentDto request)
        {
            // 1. Basic Validations
            var shop = await _context.Shops.FindAsync(request.ShopId);
            if (shop == null || !shop.IsActive) throw new ValidationException("Shop not found or inactive.");

            var employee = await _context.ShopEmployees.FindAsync(request.ShopEmployeeId);
            if (employee == null || employee.ShopId != request.ShopId || employee.IsDeleted || !employee.IsActive) throw new ValidationException("Employee not found or inactive in this shop.");

            if (request.ServiceIds == null || !request.ServiceIds.Any())
                throw new ValidationException("En az bir hizmet seçilmelidir.");

            var appointmentStart = _dateTimeService.ToTurkeyTime(request.StartTime);

            // 4. Past date check
            if (appointmentStart < _dateTimeService.Now)
            {
                throw new ValidationException("Geçmiş bir tarihe randevu oluşturamazsınız.");
            }

            // 4b. Shop closure check
            var isClosed = await _context.ShopClosureDates
                .AnyAsync(c => c.ShopId == request.ShopId && c.ClosureDate.Date == appointmentStart.Date);
            if (isClosed)
                throw new ValidationException("Salon bu tarihte kapalıdır.");

            // 5. Schedule Check
            var dayOfWeek = appointmentStart.DayOfWeek;
            var schedule = await _context.EmployeeSchedules
                .FirstOrDefaultAsync(es => es.ShopEmployeeId == request.ShopEmployeeId && es.DayOfWeek == dayOfWeek);

            if (schedule == null || !schedule.IsWorking)
            {
                throw new ValidationException("Employee is not working on this day.");
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
                        throw new ValidationException("Seçili saat aralığı başka bir randevu ile çakışıyor.");
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
                        GroupId = request.ServiceIds.Count > 1 ? groupId : null
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
        }

        public async Task<List<AppointmentDto>> GetMyAppointmentsAsync(string userId)
        {
            var query = _context.Appointments
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.StartTime);

            var items = await query.Select(a => new 
            { 
                Appointment = a, 
                HasReview = _context.Reviews.Any(r => r.AppointmentId == a.Id) 
            }).ToListAsync();

            return items.Select(i => MapToDto(i.Appointment, i.HasReview)).ToList();
        }

        public async Task<PagedResult<AppointmentDto>> GetShopAppointmentsAsync(string ownerId, Guid shopId, AppointmentStatus? status = null, int page = 1, int pageSize = 10)
        {
             var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
             if (shop == null || shop.Id != shopId) throw new ValidationException("Unauthorized or Shop not found.");

             var query = _context.Appointments
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

            var totalCount = await query.CountAsync();

            var appointments = await query
                .OrderByDescending(a => a.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var appointmentDtos = appointments.Select(a => MapToDto(a)).ToList();

            return new PagedResult<AppointmentDto>(appointmentDtos, totalCount, page, pageSize);
        }

        public async Task UpdateStatusAsync(string ownerId, Guid appointmentId, UpdateAppointmentStatusDto request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Shop)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) throw new ValidationException("Appointment not found.");

            var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
            if (shop == null || shop.Id != appointment.ShopId) throw new ValidationException("Unauthorized.");

            ValidateStatusTransition(appointment.Status, request.Status);

            appointment.Status = request.Status;
            await _context.SaveChangesAsync();
        }

        private static void ValidateStatusTransition(AppointmentStatus current, AppointmentStatus next)
        {
            var allowed = current switch
            {
                AppointmentStatus.Pending    => new[] { AppointmentStatus.Confirmed, AppointmentStatus.Rejected, AppointmentStatus.Cancelled },
                AppointmentStatus.Confirmed  => new[] { AppointmentStatus.Completed, AppointmentStatus.Cancelled, AppointmentStatus.Rejected },
                _ => Array.Empty<AppointmentStatus>() // Completed, Cancelled, Rejected terminal
            };

            if (!allowed.Contains(next))
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
                CustomerName = (a.User != null ? a.User.FirstName + " " + a.User.LastName : ""),
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status,
                Note = a.Note,
                GroupId = a.GroupId,
                HasReview = hasReview
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

            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            var appointments = await _context.Appointments
                .Where(a => 
                    a.ShopEmployeeId == employeeId &&
                    a.Status != AppointmentStatus.Cancelled &&
                    a.Status != AppointmentStatus.Rejected &&
                    a.StartTime >= startOfDay &&
                    a.StartTime < endOfDay)
                .Select(a => new TimeSlotDto
                {
                    StartTime = a.StartTime,
                    EndTime = a.EndTime
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

        public async Task<List<AppointmentDto>> GetAssignedAppointmentsAsync(string employeeUserId)
        {
            // 1. Find the Employee record for this user
            // Note: A user could potentially be an employee in multiple shops (unlikely in this domain, but possible in schema).
            // But usually, one User is linked to one ShopEmployee per shop.
            // Let's assume we want ALL appointments across all shops if they are employee in multiple.

            var assignedAppointments = await _context.Appointments
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Include(a => a.User) // Customer
                .Where(a => a.ShopEmployee.UserId == employeeUserId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            return assignedAppointments.Select(a => MapToDto(a)).ToList();
        }

        public async Task UpdateStatusByEmployeeAsync(string employeeUserId, Guid appointmentId, UpdateAppointmentStatusDto request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.ShopEmployee)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) throw new ValidationException("Appointment not found.");

            if (appointment.ShopEmployee.UserId != employeeUserId)
                throw new ValidationException("You are not authorized to manage this appointment.");

            ValidateStatusTransition(appointment.Status, request.Status);

            appointment.Status = request.Status;
            await _context.SaveChangesAsync();
        }

        public async Task CancelByCustomerAsync(string userId, Guid appointmentId, string? reason = null)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) throw new ValidationException("Randevu bulunamadı.");

            if (appointment.UserId != userId) throw new ValidationException("Bu randevuyu iptal etme yetkiniz yok.");

            if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Confirmed)
            {
                throw new ValidationException("Yalnızca bekleyen veya onaylanmış randevular iptal edilebilir.");
            }

            var now = _dateTimeService.Now;
            if ((appointment.StartTime - now).TotalHours < 2)
            {
                throw new ValidationException("Randevuya 2 saatten az kaldığı için iptal edilemez.");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                appointment.CancellationReason = reason;
            }
            await _context.SaveChangesAsync();
        }
    }
}
