using KuaforumAPI.Application.DTOs.Appointment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IAppointmentService
    {
        Task CreateAsync(string userId, CreateAppointmentDto request);
        Task<List<AppointmentDto>> GetMyAppointmentsAsync(string userId);
        Task<List<AppointmentDto>> GetShopAppointmentsAsync(string ownerId, Guid shopId);
        Task UpdateStatusAsync(string ownerId, Guid appointmentId, UpdateAppointmentStatusDto request);
    }
}
