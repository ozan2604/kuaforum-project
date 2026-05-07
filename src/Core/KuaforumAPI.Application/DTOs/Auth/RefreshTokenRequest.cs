using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token zorunludur.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
