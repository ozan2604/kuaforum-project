using System;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class AuthResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty; // Added PhoneNumber
        public string? ProfileImageUrl { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
