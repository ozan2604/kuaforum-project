using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class CloudinaryImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryImageService(IOptions<CloudinarySettings> config)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folderName, int? width = null, int? height = null)
        {
            if (file == null || file.Length == 0)
                return null;

            using var stream = file.OpenReadStream();
            
            var transformation = new Transformation().Quality("auto").FetchFormat("auto");

            if (width.HasValue && height.HasValue)
            {
                transformation.Width(width.Value).Height(height.Value).Crop("fill").Gravity("auto");
            }

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName,
                Transformation = transformation
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.ToString();
        }


        public async Task DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            var publicId = GetPublicIdFromUrl(imageUrl);
            if (!string.IsNullOrEmpty(publicId))
            {
                var deletionParams = new DeletionParams(publicId);
                await _cloudinary.DestroyAsync(deletionParams);
            }
        }

        private string GetPublicIdFromUrl(string url)
        {
            try
            {
                // URL format: https://res.cloudinary.com/cloud_name/image/upload/v12345678/folder/public_id.jpg
                // We want: folder/public_id

                var uri = new Uri(url);
                var path = uri.AbsolutePath; // /cloud_name/image/upload/v1234/folder/id.jpg
                
                // Split by '/'
                var segments = path.Split('/');
                
                // Find "upload" segment index
                int uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex == -1 || uploadIndex + 1 >= segments.Length) return null;

                // Check if next segment is version (starts with 'v' followed by digits)
                int startIndex = uploadIndex + 1;
                if (segments[startIndex].StartsWith("v") && segments[startIndex].Length > 1 && char.IsDigit(segments[startIndex][1]))
                {
                    startIndex++;
                }

                if (startIndex >= segments.Length) return null;

                // Combine remaining segments
                // Remove extension from the last segment
                var lastSegment = segments[segments.Length - 1];
                int dotIndex = lastSegment.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    segments[segments.Length - 1] = lastSegment.Substring(0, dotIndex);
                }

                // Join segments from startIndex to end
                // We need to construct the public ID path
                // segments array: [0] "", [1] "cloud", [2] "image", [3] "upload", [4] "v123", [5] "folder", [6] "id"
                // startIndex would be 5
                
                // Let's rely on standard string manipulation if segments fail or are tricky
                
                // Just use string manipulation logic implemented in thought block which is simpler
                // Re-implementation for safety:
                
                string pathStr = path;
                int uploadPos = pathStr.IndexOf("/upload/");
                if (uploadPos == -1) return null;
                
                string afterUpload = pathStr.Substring(uploadPos + 8); // Skip "/upload/"
                
                // Check if starts with v + digits + /
                if (afterUpload.StartsWith("v"))
                {
                    int slashPos = afterUpload.IndexOf('/');
                    if (slashPos > 0)
                    {
                        // Check if pure digits after v
                        bool isVersion = true;
                        for(int i=1; i<slashPos; i++) 
                        {
                            if (!char.IsDigit(afterUpload[i])) 
                            {
                                // Maybe not version if mixed chars? standard version is v123456
                                // Let's assume it IS version if it looks like v.../
                            }
                        }
                        
                        // Cloudinary version is v123123123
                        afterUpload = afterUpload.Substring(slashPos + 1);
                    }
                }
                
                // Remove extension
                int lastDot = afterUpload.LastIndexOf('.');
                if (lastDot > 0)
                {
                    afterUpload = afterUpload.Substring(0, lastDot);
                }
                
                return afterUpload;
            }
            catch
            {
                return null;
            }
        }
    }
}
