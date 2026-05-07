using KuaforumAPI.Application.DTOs.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> RefreshAsync(string refreshToken);
        Task<AuthResponse> UpdateProfileAsync(string userId, UpdateProfileDto request);
        Task ChangePasswordAsync(string userId, ChangePasswordDto request);
        Task DeleteAccountAsync(string userId);
        Task<string> UpdateProfileImageAsync(string userId, Microsoft.AspNetCore.Http.IFormFile image);
        Task DeleteProfileImageAsync(string userId);

        // OTP (SMS doğrulama) akışları
        Task<SendOtpResponse> SendLoginOtpAsync(SendLoginOtpRequest request);
        Task<AuthResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest request);
        Task<SendOtpResponse> SendRegisterOtpAsync(SendRegisterOtpRequest request);
        Task<AuthResponse> VerifyRegisterOtpAsync(VerifyRegisterOtpRequest request);
        Task<SendOtpResponse> SendForgotPasswordOtpAsync(SendForgotPasswordOtpRequest request);
        Task ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request);
        Task LogoutAsync(string userId);
    }
}
