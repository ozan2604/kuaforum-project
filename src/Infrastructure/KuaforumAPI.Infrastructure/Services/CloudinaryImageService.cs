using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class CloudinaryImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryImageService> _logger;

        public CloudinaryImageService(IOptions<CloudinarySettings> config, ILogger<CloudinaryImageService> logger)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
            _logger = logger;
        }

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/webp", "image/heic", "image/heif"
        };
        private static readonly HashSet<string> AllowedVideoMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm"
        };

        private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15 MB
        private const long MaxVideoFileSizeBytes = 100 * 1024 * 1024; // 100 MB

        public async Task<string> UploadImageAsync(IFormFile file, string folderName, int? width = null, int? height = null)
        {
            if (file == null || file.Length == 0)
                return null;

            if (!AllowedMimeTypes.Contains(file.ContentType))
                throw new ArgumentException("Yalnızca JPEG, PNG, WebP veya HEIC formatında görsel yüklenebilir.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException("Dosya boyutu 15 MB'ı geçemez.");

            using var stream = file.OpenReadStream();

            var transformation = new Transformation().Quality("auto").FetchFormat("auto");

            if (width.HasValue && height.HasValue)
            {
                transformation.Width(width.Value).Height(height.Value).Crop("fill").Gravity("auto");
            }
            else
            {
                // Limit to 1200px wide max while preserving aspect ratio
                transformation.Width(1200).Crop("limit");
            }

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName,
                Transformation = transformation
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary yükleme başarısız. Klasör: {Folder}, Hata: {Error}", folderName, uploadResult.Error.Message);
                throw new InvalidOperationException($"Görsel yüklenemedi: {uploadResult.Error.Message}");
            }

            _logger.LogInformation("Görsel yüklendi. Klasör: {Folder}, URL: {Url}", folderName, uploadResult.SecureUrl);
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> UploadVideoAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            if (!AllowedVideoMimeTypes.Contains(file.ContentType))
                throw new ArgumentException("Yalnızca MP4, MOV, AVI, MKV veya WEBM formatında video yüklenebilir.");

            if (file.Length > MaxVideoFileSizeBytes)
                throw new ArgumentException("Video boyutu 100 MB'ı geçemez.");

            using var stream = file.OpenReadStream();

            // Cloudinary'e orijinal formatıyla yükle (MP4 dönüşümü asenkron olduğu için 404 hatasına sebep oluyordu)
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary video yükleme başarısız. Klasör: {Folder}, Hata: {Error}", folderName, uploadResult.Error.Message);
                throw new InvalidOperationException($"Video yüklenemedi: {uploadResult.Error.Message}");
            }
            // URL-based transformation: Cloudinary ilk istek geldiğinde anında MP4/H.264'e dönüştürür ve cache'ler.
            // Eager (arka plan) transformation kullanmıyoruz — o asenkron olduğu için yükleme sonrası 404 veriyordu.
            // Bu yöntemde ise dönüşüm on-demand olur, asla 404 olmaz; ilk oynatma birkaç sn yavaş olabilir.
            string rawUrl = uploadResult.SecureUrl.ToString();
            string finalUrl = rawUrl.Replace("/upload/", "/upload/f_mp4,vc_h264,q_auto/");

            _logger.LogInformation("Video yüklendi. Klasör: {Folder}, URL: {Url}", folderName, finalUrl);
            return finalUrl;
        }

        public async Task DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            var publicId = GetPublicIdFromUrl(imageUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("Cloudinary public ID çözümlenemedi. URL: {Url}", imageUrl);
                return;
            }

            var deletionParams = new DeletionParams(publicId)
            {
                Invalidate = true
            };
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result != "ok")
                _logger.LogWarning("Cloudinary silme başarısız. PublicId: {PublicId}, Sonuç: {Result}", publicId, result.Result);
        }

        public async Task DeleteVideoAsync(string videoUrl)
        {
            if (string.IsNullOrEmpty(videoUrl)) return;

            var publicId = GetPublicIdFromUrl(videoUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("Cloudinary public ID çözümlenemedi (Video). URL: {Url}", videoUrl);
                return;
            }

            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Video,
                Invalidate = true
            };
            
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result != "ok")
                _logger.LogWarning("Cloudinary video silme başarısız. PublicId: {PublicId}, Sonuç: {Result}", publicId, result.Result);
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
