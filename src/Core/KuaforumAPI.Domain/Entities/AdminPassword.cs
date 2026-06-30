using System;

namespace KuaforumAPI.Domain.Entities
{
    public class AdminPassword
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Şifrenin ne amaçla kullanılacağını veya adını tutar (Örn: "Password1", "Password2").
        /// </summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// BCrypt ile hashlenmiş şifre.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
