using KuaforumAPI.Application.DTOs.Auth;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ─── OTP: Giriş ──────────────────────────────────────────────────────────

        [HttpPost("login/send-otp")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<SendOtpResponse>> SendLoginOtp(SendLoginOtpRequest request)
        {
            var result = await _authService.SendLoginOtpAsync(request);
            return Ok(result);
        }

        [HttpPost("login/verify-otp")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponse>> VerifyLoginOtp(VerifyLoginOtpRequest request)
        {
            var result = await _authService.VerifyLoginOtpAsync(request);
            return Ok(result);
        }

        // ─── OTP: Kayıt ──────────────────────────────────────────────────────────

        [HttpPost("register/send-otp")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<SendOtpResponse>> SendRegisterOtp(SendRegisterOtpRequest request)
        {
            var result = await _authService.SendRegisterOtpAsync(request);
            return Ok(result);
        }

        [HttpPost("register/verify-otp")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponse>> VerifyRegisterOtp(VerifyRegisterOtpRequest request)
        {
            var result = await _authService.VerifyRegisterOtpAsync(request);
            return Ok(result);
        }

        // ─── Token Yenileme & Oturum ─────────────────────────────────────────────

        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshAsync(request.RefreshToken);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
                await _authService.LogoutAsync(userId);
            return NoContent();
        }

        // ─── Profil ──────────────────────────────────────────────────────────────

        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult<AuthResponse>> UpdateProfile(UpdateProfileDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _authService.UpdateProfileAsync(userId, request);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _authService.DeleteAccountAsync(userId);
            return NoContent();
        }

        [Authorize]
        [HttpPut("profile-image")]
        [EnableRateLimiting("upload")]
        public async Task<IActionResult> UpdateProfileImage(IFormFile image)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var imageUrl = await _authService.UpdateProfileImageAsync(userId, image);
            return Ok(new { imageUrl });
        }

        [Authorize]
        [HttpDelete("profile-image")]
        [EnableRateLimiting("upload")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _authService.DeleteProfileImageAsync(userId);
            return NoContent();
        }

        // ─── OTP: Misafir Kimlik Doğrulama ───────────────────────────────────────

        [HttpPost("guest/send-otp")]
        [AllowAnonymous]
        [EnableRateLimiting("send-otp")]
        public async Task<ActionResult<SendOtpResponse>> SendGuestAuthOtp(SendGuestAuthOtpRequest request)
        {
            var result = await _authService.SendGuestAuthOtpAsync(request);
            return Ok(result);
        }

        [HttpPost("guest/verify-otp")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponse>> VerifyGuestAuthOtp(VerifyGuestAuthOtpRequest request)
        {
            var result = await _authService.VerifyGuestAuthOtpAsync(request);
            return Ok(result);
        }
    }
}
