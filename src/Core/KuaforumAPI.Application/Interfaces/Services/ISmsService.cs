namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }
}
