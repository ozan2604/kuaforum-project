using KuaforumAPI.Application.DTOs.Auth;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;
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

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

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
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _authService.ChangePasswordAsync(userId, request);
            return NoContent();
        }

        [Authorize]
        [HttpPut("profile-image")]
        public async Task<IActionResult> UpdateProfileImage(IFormFile image)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var imageUrl = await _authService.UpdateProfileImageAsync(userId, image);
            return Ok(new { imageUrl });
        }

        [Authorize]
        [HttpDelete("profile-image")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _authService.DeleteProfileImageAsync(userId);
            return NoContent();
        }

    }
}
