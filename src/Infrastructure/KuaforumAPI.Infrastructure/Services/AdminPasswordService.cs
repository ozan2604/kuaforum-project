using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KuaforumAPI.Application.DTOs.Admin;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using BCrypt.Net;

namespace KuaforumAPI.Infrastructure.Services
{
    public class AdminPasswordService : IAdminPasswordService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeService _dateTimeService;

        public AdminPasswordService(ApplicationDbContext context, IDateTimeService dateTimeService)
        {
            _context = context;
            _dateTimeService = dateTimeService;
        }

        public async Task<List<AdminPasswordStatusDto>> GetAllStatusesAsync()
        {
            var passwords = await _context.AdminPasswords.ToListAsync();
            
            // Expected keys
            var expectedKeys = new List<string> { "Şifre 1", "Şifre 2" };
            
            var result = new List<AdminPasswordStatusDto>();
            
            foreach (var key in expectedKeys)
            {
                var existing = passwords.FirstOrDefault(p => p.Key == key);
                result.Add(new AdminPasswordStatusDto
                {
                    Key = key,
                    IsSet = existing != null,
                    UpdatedAt = existing?.UpdatedAt ?? existing?.CreatedAt
                });
            }

            return result;
        }

        public async Task<bool> SetPasswordAsync(SetAdminPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Password))
                return false;

            var existing = await _context.AdminPasswords.FirstOrDefaultAsync(p => p.Key == request.Key);
            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            
            if (existing != null)
            {
                existing.PasswordHash = hash;
                existing.UpdatedAt = _dateTimeService.Now;
                _context.AdminPasswords.Update(existing);
            }
            else
            {
                var newPassword = new AdminPassword
                {
                    Key = request.Key,
                    PasswordHash = hash,
                    CreatedAt = _dateTimeService.Now,
                    UpdatedAt = _dateTimeService.Now
                };
                _context.AdminPasswords.Add(newPassword);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePasswordAsync(string key)
        {
            var existing = await _context.AdminPasswords.FirstOrDefaultAsync(p => p.Key == key);
            if (existing == null)
                return false;

            _context.AdminPasswords.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
