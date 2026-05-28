namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IShopCodeGeneratorService
    {
        Task<string> GenerateAsync(string city);
    }
}
