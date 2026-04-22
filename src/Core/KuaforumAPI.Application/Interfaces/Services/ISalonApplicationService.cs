using KuaforumAPI.Application.DTOs.SalonApplication;
using KuaforumAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface ISalonApplicationService
    {
        Task ApplyAsync(string userId, CreateSalonApplicationDto request);
        Task<List<SalonApplicationListDto>> GetPendingApplicationsAsync();
        Task<List<SalonApplicationListDto>> GetRejectedApplicationsAsync();
        Task<SalonOwnerApplication> GetApplicationByUserIdAsync(string userId);
        Task ApproveApplicationAsync(Guid applicationId);
        Task RejectApplicationAsync(Guid applicationId);
    }
}
