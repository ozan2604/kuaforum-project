using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName, int? width = null, int? height = null);
        Task DeleteImageAsync(string imageUrl);
    }
}
