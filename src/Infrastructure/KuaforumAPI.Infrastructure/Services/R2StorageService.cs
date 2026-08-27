using Amazon.S3;
using Amazon.S3.Model;
using ImageMagick;
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
    /// <summary>
    /// Medyayi Cloudflare R2'ye yukler.
    ///
    /// Cloudinary'den farkli olarak R2 duz depolama: teslimat sirasinda
    /// donusum yapmaz. Bu yuzden Cloudinary'nin ustlendigi isler yukleme
    /// aninda burada yapiliyor — boyutlandirma, format cevirme ve dogru
    /// Content-Type etiketi.
    ///
    /// Donen deger TAM ADRES DEGIL, depolama anahtaridir ("shops/covers/ab12.jpg").
    /// Tam adresi <c>MediaUrlConverter</c> okuma sirasinda Media:BaseUrl ile
    /// uretiyor. Boylece saglayici bir daha degistiginde veritabanina ve
    /// uygulama katmanlarina dokunmak gerekmiyor.
    /// </summary>
    public sealed class R2StorageService : IImageService
    {
        private readonly IAmazonS3 _s3;
        private readonly R2Settings _r2;
        private readonly string _mediaBaseUrl;
        private readonly ILogger<R2StorageService> _logger;

        public R2StorageService(
            IOptions<R2Settings> r2,
            IOptions<MediaSettings> media,
            ILogger<R2StorageService> logger)
        {
            _r2 = r2.Value;
            _mediaBaseUrl = (media.Value.BaseUrl ?? string.Empty).TrimEnd('/');
            _logger = logger;

            if (!_r2.IsConfigured)
                throw new InvalidOperationException(
                    "R2 ayarlari eksik. AccountId, AccessKey, SecretKey ve Bucket doldurulmali.");

            _s3 = new AmazonS3Client(
                _r2.AccessKey,
                _r2.SecretKey,
                new AmazonS3Config
                {
                    ServiceURL = $"https://{_r2.AccountId}.r2.cloudflarestorage.com",
                    // R2 yol tabanli adresleme kullaniyor; alt alan adi bicimi calismaz.
                    ForcePathStyle = true,
                    AuthenticationRegion = "auto"
                });
        }

        // ── Sinirlar (Cloudinary surumundeki kurallarla ayni) ────────────────
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/webp", "image/heic", "image/heif"
        };
        private static readonly HashSet<string> AllowedVideoMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm"
        };

        private const long MaxFileSizeBytes = 15 * 1024 * 1024;        // 15 MB
        private const long MaxVideoFileSizeBytes = 100 * 1024 * 1024;  // 100 MB

        /// <summary>Boyutsuz cagrilarda uzun kenar bu degere indirilir.</summary>
        private const int DefaultMaxWidth = 1200;

        private const int JpegQuality = 82;

        private const string CacheControl = "public, max-age=31536000, immutable";

        // ── Yukleme ─────────────────────────────────────────────────────────

        public async Task<string> UploadImageAsync(IFormFile file, string folderName, int? width = null, int? height = null)
        {
            if (file == null || file.Length == 0)
                return null;

            if (!AllowedMimeTypes.Contains(file.ContentType))
                throw new ArgumentException("Yalnızca JPEG, PNG, WebP veya HEIC formatında görsel yüklenebilir.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException("Dosya boyutu 15 MB'ı geçemez.");

            using var input = new MemoryStream();
            await file.CopyToAsync(input);
            input.Position = 0;

            var (bytes, extension, contentType) = ProcessImage(input, width, height);
            var key = BuildKey(folderName, extension);

            await PutAsync(key, bytes, contentType);

            _logger.LogInformation("Gorsel yuklendi. Anahtar: {Key}, Boyut: {Bytes} bayt", key, bytes.Length);
            return key;
        }

        public async Task<string> UploadVideoAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            if (!AllowedVideoMimeTypes.Contains(file.ContentType))
                throw new ArgumentException("Yalnızca MP4, MOV, AVI, MKV veya WEBM formatında video yüklenebilir.");

            if (file.Length > MaxVideoFileSizeBytes)
                throw new ArgumentException("Video boyutu 100 MB'ı geçemez.");

            var extension = NormalizeExtension(Path.GetExtension(file.FileName));
            var key = BuildKey(folderName, extension);

            using var stream = file.OpenReadStream();
            await PutStreamAsync(key, stream, VideoContentType(extension));

            _logger.LogInformation("Video yuklendi. Anahtar: {Key}, Boyut: {Bytes} bayt", key, file.Length);
            return key;
        }

        // ── Silme ───────────────────────────────────────────────────────────

        public Task DeleteImageAsync(string imageUrl) => DeleteAsync(imageUrl);

        public Task DeleteVideoAsync(string videoUrl) => DeleteAsync(videoUrl);

        private async Task DeleteAsync(string storedValue)
        {
            var key = ToKey(storedValue);
            if (key == null)
                return;

            try
            {
                await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _r2.Bucket, Key = key });
                _logger.LogInformation("Medya silindi. Anahtar: {Key}", key);
            }
            catch (Exception ex)
            {
                // Silme basarisiz olsa da cagiran islem devam etmeli; yetim dosya
                // kalmasi, kullanicinin islemini bastan kesmekten iyi.
                _logger.LogWarning(ex, "Medya silinemedi. Anahtar: {Key}", key);
            }
        }

        /// <summary>
        /// Varlikta duran degeri depolama anahtarina cevirir.
        ///
        /// Deger okuma sirasinda donusturucuden gectigi icin genelde tam adres
        /// olur ("https://cdn.salonbir.com/shops/covers/ab12.jpg"). Baska bir
        /// saglayiciya ait eski kayitlar bu kovada olmadigi icin atlanir.
        /// </summary>
        private string ToKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (!string.IsNullOrEmpty(_mediaBaseUrl) &&
                value.StartsWith(_mediaBaseUrl, StringComparison.OrdinalIgnoreCase))
                return value.Substring(_mediaBaseUrl.Length).TrimStart('/');

            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Bu kovaya ait olmayan adres silinmedi: {Url}", value);
                return null;
            }

            return value.TrimStart('/');   // zaten anahtar
        }

        // ── Gorsel isleme ───────────────────────────────────────────────────

        /// <summary>
        /// Gorseli tarayiciya uygun hale getirir: EXIF donuklugunu duzeltir,
        /// boyutlandirir, HEIC gibi tarayicilarin okuyamadigi formatlari
        /// JPEG'e cevirir ve konum bilgisi dahil tum ustveriyi temizler.
        ///
        /// Cloudinary bunlari teslimat sirasinda yapiyordu; R2 yapmadigi icin
        /// bir kez burada, yukleme aninda yapiliyor.
        /// </summary>
        private static (byte[] Bytes, string Extension, string ContentType) ProcessImage(
            Stream input, int? width, int? height)
        {
            using var image = new MagickImage(input);

            // Telefon fotograflari donukluk bilgisini EXIF'te tutar; duzeltilmezse
            // yan yatmis gorunurler.
            image.AutoOrient();

            if (width.HasValue && height.HasValue)
            {
                // Kutuyu tamamen doldur, tasan kismi ortadan kirp.
                image.Resize(new MagickGeometry((uint)width.Value, (uint)height.Value)
                {
                    FillArea = true
                });
                image.Extent(
                    new MagickGeometry((uint)width.Value, (uint)height.Value),
                    Gravity.Center);
            }
            else if (image.Width > DefaultMaxWidth)
            {
                // Yalnizca kucult; en-boy oranini koru, kucuk gorseli buyutme.
                image.Resize(new MagickGeometry((uint)DefaultMaxWidth, 0));
            }

            // Konum ve cihaz bilgisi tasiyan ustveriyi at.
            image.Strip();

            var browserSafe = image.Format is MagickFormat.Jpeg or MagickFormat.Jpg
                or MagickFormat.Png or MagickFormat.WebP;

            if (!browserSafe)
                image.Format = MagickFormat.Jpeg;   // HEIC/HEIF ve digerleri

            if (image.Format is MagickFormat.Jpeg or MagickFormat.Jpg)
                image.Quality = JpegQuality;

            return image.Format switch
            {
                MagickFormat.Png => (image.ToByteArray(), ".png", "image/png"),
                MagickFormat.WebP => (image.ToByteArray(), ".webp", "image/webp"),
                _ => (image.ToByteArray(), ".jpg", "image/jpeg"),
            };
        }

        // ── Yardimcilar ─────────────────────────────────────────────────────

        /// <summary>
        /// Cakismasi mumkun olmayan anahtar uretir. Dosya adi kullanilmiyor:
        /// kullanicidan gelen ad hem cakisabilir hem de yol kacisi riski tasir.
        ///
        /// Yapilandirilmissa basa ortam oneki eklenir ("test/shops/covers/...").
        /// Canlida onek bos oldugu icin bu ek hicbir sey degistirmez.
        /// </summary>
        private string BuildKey(string folderName, string extension)
        {
            var key = $"{folderName.Trim('/')}/{Guid.NewGuid():N}{extension}";
            var onek = _r2.KeyPrefix.Trim('/');

            return string.IsNullOrEmpty(onek) ? key : $"{onek}/{key}";
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return ".mp4";

            var ext = extension.ToLowerInvariant();
            return ext.StartsWith('.') ? ext : "." + ext;
        }

        /// <summary>
        /// Videonun Content-Type'i.
        ///
        /// .mov icin video/quicktime DEGIL video/mp4 donuyoruz: Chrome ve
        /// Firefox video/quicktime'i &lt;video&gt; icinde oynatmayi reddedip
        /// siyah kare gosteriyor. Telefondan cikan .mov dosyalari pratikte
        /// H.264+AAC ve konteyner ISO-BMFF oldugu icin video/mp4 etiketiyle
        /// sorunsuz oynuyorlar.
        /// </summary>
        private static string VideoContentType(string extension) => extension switch
        {
            ".mp4" or ".m4v" or ".mov" => "video/mp4",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            ".avi" => "video/x-msvideo",
            _ => "video/mp4",
        };

        // await sart: Task donduren bir metotta `using` kullanmak akisi
        // yukleme bitmeden kapatir.
        private async Task PutAsync(string key, byte[] bytes, string contentType)
        {
            using var stream = new MemoryStream(bytes);
            await PutStreamAsync(key, stream, contentType);
        }

        private Task PutStreamAsync(string key, Stream stream, string contentType) =>
            _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _r2.Bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                // Anahtarlar benzersiz oldugu icin icerik hicbir zaman degismez.
                Headers = { CacheControl = CacheControl },
                DisablePayloadSigning = true   // R2 streaming imzali govdeyi desteklemiyor
            });
    }
}
