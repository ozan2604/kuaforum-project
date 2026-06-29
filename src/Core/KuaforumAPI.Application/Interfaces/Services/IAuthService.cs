using KuaforumAPI.Application.DTOs.Auth;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RefreshAsync(string refreshToken);
        Task<AuthResponse> UpdateProfileAsync(string userId, UpdateProfileDto request);
        Task DeleteAccountAsync(string userId);
        Task<string> UpdateProfileImageAsync(string userId, Microsoft.AspNetCore.Http.IFormFile image);
        Task DeleteProfileImageAsync(string userId);
        Task LogoutAsync(string userId);

        // OTP: Giriş (telefon numarası ile)
        Task<SendOtpResponse> SendLoginOtpAsync(SendLoginOtpRequest request);
        Task<AuthResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest request);

        // OTP: Kayıt (ad/soyad + telefon)
        Task<SendOtpResponse> SendRegisterOtpAsync(SendRegisterOtpRequest request);
        Task<AuthResponse> VerifyRegisterOtpAsync(VerifyRegisterOtpRequest request);

        // Misafir randevu kimlik doğrulama (telefon OTP ile — yeni veya mevcut hesap)
        Task<SendOtpResponse> SendGuestAuthOtpAsync(SendGuestAuthOtpRequest request);
        Task<AuthResponse> VerifyGuestAuthOtpAsync(VerifyGuestAuthOtpRequest request);
    }
}
