using Amazon.S3;
using Amazon.S3.Model;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Application.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    /// <summary>
    /// Test APK'larini R2'den listeler.
    ///
    /// Dosyalari buraya mobil deponun surekli tumlestirme hatti birakiyor;
    /// bu sinif yalnizca OKUYOR. Indirme adresi herkese acik alan adindan
    /// uretiliyor (cdn.salonbir.com) — boylece APK'yi indiren kisinin GitHub
    /// hesabina ihtiyaci olmuyor. Kod deposunun artifact baglantisi giris
    /// istiyordu ve test yapacak herkesin depoya erisimi yok.
    ///
    /// Listeleme ucu Cloudflare Access arkasinda; indirme adresi degil.
    /// Bunun sakincasi sinirli: test APK'si yalnizca api-test'e bakiyor ve
    /// orasi da Access ile korunuyor, yani dosyayi ele geciren biri
    /// uygulamayi kullanamiyor.
    /// </summary>
    public sealed class R2ApkKatalogu : IApkKatalogu
    {
        private readonly IAmazonS3 _s3;
        private readonly R2Settings _r2;
        private readonly string _mediaBaseUrl;

        public R2ApkKatalogu(IOptions<R2Settings> r2, IOptions<MediaSettings> media)
        {
            _r2 = r2.Value;
            _mediaBaseUrl = (media.Value.BaseUrl ?? string.Empty).TrimEnd('/');

            if (!_r2.IsConfigured)
                throw new InvalidOperationException(
                    "R2 ayarlari eksik. AccountId, AccessKey, SecretKey ve Bucket doldurulmali.");

            _s3 = new AmazonS3Client(
                _r2.AccessKey,
                _r2.SecretKey,
                new AmazonS3Config
                {
                    ServiceURL = $"https://{_r2.AccountId}.r2.cloudflarestorage.com",
                    ForcePathStyle = true,
                    AuthenticationRegion = "auto"
                });
        }

        public async Task<IReadOnlyList<ApkDosyasi>> SonlariGetirAsync(
            int adet,
            CancellationToken iptal = default)
        {
            var onek = _r2.ApkPrefix.TrimStart('/');

            var yanit = await _s3.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = _r2.Bucket,
                    Prefix = onek,
                    // Liste zamana gore degil ada gore geliyor; en yenileri
                    // bulmak icin hepsini alip kendimiz siralamamiz gerekiyor.
                    // Sayfa basi 1000 kayit, test APK'lari icin fazlasiyla yeterli.
                    MaxKeys = 1000
                },
                iptal);

            return yanit.S3Objects
                .Where(o => o.Key.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                // Boyut ve tarih SDK'da nullable; tarihi olmayan bir nesne
                // beklenmiyor ama gelirse listenin sonuna dussun, patlamasin.
                .OrderByDescending(o => o.LastModified ?? DateTime.MinValue)
                .Take(adet)
                .Select(Cevir)
                .ToList();
        }

        private ApkDosyasi Cevir(S3Object nesne)
        {
            var dosyaAdi = nesne.Key.Split('/').Last();
            var (calistirmaNo, commit) = ApkDosyasi.AdiCozumle(dosyaAdi);

            return new ApkDosyasi(
                dosyaAdi,
                $"{_mediaBaseUrl}/{nesne.Key}",
                nesne.Size ?? 0,
                nesne.LastModified ?? DateTime.MinValue,
                calistirmaNo,
                commit);
        }

    }
}
