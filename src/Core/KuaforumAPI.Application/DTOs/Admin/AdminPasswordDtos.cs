namespace KuaforumAPI.Application.DTOs.Admin
{
    public class SetAdminPasswordRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AdminPasswordStatusDto
    {
        public string Key { get; set; } = string.Empty;
        public bool IsSet { get; set; }
        public System.DateTime? UpdatedAt { get; set; }
    }
}
