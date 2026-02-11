using KuaforumAPI.Application.DTOs.Auth;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpPost("login")]
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
        [HttpGet("addresses")]
        public async Task<ActionResult<IEnumerable<AddressDto>>> GetAddresses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addresses = await _authService.GetAddressesAsync(userId);
            return Ok(addresses);
        }

        [Authorize]
        [HttpPost("addresses")]
        public async Task<ActionResult<AddressDto>> AddAddress(CreateAddressDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _authService.AddAddressAsync(userId, request);
            return CreatedAtAction(nameof(GetAddresses), new { }, address);
        }

        [Authorize]
        [HttpDelete("addresses/{id}")]
        public async Task<IActionResult> DeleteAddress(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _authService.DeleteAddressAsync(userId, id);
            return NoContent();
        }


    }
}
