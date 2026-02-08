using FluentValidation;
using KuaforumAPI.Application.DTOs.Appointment;
using KuaforumAPI.Application.DTOs.Service;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IShopRepository _shopRepository;

        public AppointmentService(ApplicationDbContext context, IShopRepository shopRepository)
        {
            _context = context;
            _shopRepository = shopRepository;
        }

        public async Task CreateAsync(string userId, CreateAppointmentDto request)
        {
            // 1. Basic Validations
            var shop = await _context.Shops.FindAsync(request.ShopId);
            if (shop == null) throw new ValidationException("Shop not found.");

            var service = await _context.ShopServices.FindAsync(request.ShopServiceId);
            if (service == null || service.ShopId != request.ShopId) throw new ValidationException("Service not found in this shop.");

            var employee = await _context.ShopEmployees.FindAsync(request.ShopEmployeeId);
            if (employee == null || employee.ShopId != request.ShopId) throw new ValidationException("Employee not found in this shop.");

            // 2. Skill Check
            var hasSkill = await _context.ShopEmployeeServices
                .AnyAsync(ses => ses.ShopEmployeeId == request.ShopEmployeeId && ses.ShopServiceId == request.ShopServiceId);
            if (!hasSkill) throw new ValidationException("This employee cannot perform this service.");

            // 3. Time Calculations
            var appointmentStart = request.StartTime; // Assuming UTC or correct local time from client
            var appointmentEnd = appointmentStart.AddMinutes(service.Duration);

            // 4. Schedule Check
            var dayOfWeek = appointmentStart.DayOfWeek;
            var schedule = await _context.EmployeeSchedules
                .FirstOrDefaultAsync(es => es.ShopEmployeeId == request.ShopEmployeeId && es.DayOfWeek == dayOfWeek);

            if (schedule == null || !schedule.IsWorking)
            {
                throw new ValidationException("Employee is not working on this day.");
            }

            // Convert DateTime to TimeSpan for daily comparison
            var timeStart = appointmentStart.TimeOfDay;
            var timeEnd = appointmentEnd.TimeOfDay;

            if (timeStart < schedule.StartTime || timeEnd > schedule.EndTime)
            {
                throw new ValidationException($"Appointment is outside working hours ({schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}).");
            }

            // Break Time Check
            if (schedule.BreakStartTime.HasValue && schedule.BreakEndTime.HasValue)
            {
                // Overlap logic: (Start1 < End2) && (End1 > Start2)
                if (timeStart < schedule.BreakEndTime.Value && timeEnd > schedule.BreakStartTime.Value)
                {
                    throw new ValidationException("Appointment conflicts with break time.");
                }
            }

            // 5. Conflict Check (Existing Appointments)
            var hasConflict = await _context.Appointments
                .AnyAsync(a => 
                    a.ShopEmployeeId == request.ShopEmployeeId &&
                    a.Status != AppointmentStatus.Cancelled &&
                    a.Status != AppointmentStatus.Rejected &&
                    a.StartTime < appointmentEnd && 
                    a.EndTime > appointmentStart);

            if (hasConflict)
            {
                throw new ValidationException("The selected time slot is already booked.");
            }

            // 6. Create Appointment
            var appointment = new Appointment
            {
                ShopId = request.ShopId,
                ShopServiceId = request.ShopServiceId,
                ShopEmployeeId = request.ShopEmployeeId,
                UserId = userId,
                StartTime = appointmentStart,
                EndTime = appointmentEnd,
                Status = AppointmentStatus.Pending,
                Note = request.Note
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AppointmentDto>> GetMyAppointmentsAsync(string userId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            return MapToDto(appointments);
        }

        public async Task<List<AppointmentDto>> GetShopAppointmentsAsync(string ownerId, Guid shopId)
        {
             var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
             if (shop == null || shop.Id != shopId) throw new ValidationException("Unauthorized or Shop not found.");

             var appointments = await _context.Appointments
                .Include(a => a.Shop)
                .Include(a => a.ShopService)
                .Include(a => a.ShopEmployee)
                    .ThenInclude(e => e.User)
                .Include(a => a.User)
                .Where(a => a.ShopId == shopId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            return MapToDto(appointments);
        }

        public async Task UpdateStatusAsync(string ownerId, Guid appointmentId, UpdateAppointmentStatusDto request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Shop)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            
            if (appointment == null) throw new ValidationException("Appointment not found.");

            // Verify Ownership
             var shop = await _shopRepository.GetByOwnerIdAsync(ownerId);
             if (shop == null || shop.Id != appointment.ShopId) throw new ValidationException("Unauthorized.");

             appointment.Status = request.Status;
             await _context.SaveChangesAsync();
        }

        private List<AppointmentDto> MapToDto(List<Appointment> appointments)
        {
            return appointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                ShopId = a.ShopId,
                ShopName = a.Shop.Name,
                ShopServiceId = a.ShopServiceId,
                ServiceName = a.ShopService.Name,
                Price = a.ShopService.Price,
                Duration = a.ShopService.Duration,
                ShopEmployeeId = a.ShopEmployeeId,
                EmployeeName = a.ShopEmployee.Title + " " + a.ShopEmployee.User?.FirstName, // Need to include User logic better if null
                UserId = a.UserId,
                CustomerName = a.User?.FirstName + " " + a.User?.LastName,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status,
                Note = a.Note
            }).ToList();
        }
    }
}
