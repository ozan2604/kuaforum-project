using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.WebAPI.Endpoints
{
    /// <summary>
    /// Mobil hattin urettigi test APK'larini listeleyen uclar.
    ///
    /// Production'da HIC map edilmez — orada bu adresler 404 doner. Kayit
    /// <see cref="MapTestApkEndpoints"/> cagrisinin icinde ortam kontrolune
    /// bagli oldugu icin, yanlislikla acik kalmasi mumkun degil.
    ///
    /// Erisim ayrica Cloudflare Access ile e-posta listesine kisitli — /test/otp
    /// ile ayni desen.
    /// </summary>
    public static class TestApkEndpoints
    {
        public static void MapTestApkEndpoints(this WebApplication app)
        {
            if (app.Environment.IsProduction())
                return;

            var group = app.MapGroup("/test/apk").ExcludeFromDescription();

            // Insan icin: tarayicida acilan liste.
            group.MapGet("/", async (IApkKatalogu katalog, CancellationToken iptal) =>
            {
                try
                {
                    var dosyalar = await katalog.SonlariGetirAsync(20, iptal);
                    return Results.Content(SayfaOlustur(dosyalar), "text/html; charset=utf-8");
                }
                catch (Exception hata)
                {
                    // Depoya ulasilamamasi beklenen bir durum degil ama sayfayi
                    // bos bir hata ekranina cevirmek yerine sebebini yaziyoruz:
                    // burasi zaten yalnizca ekibin gordugu bir test ekrani.
                    return Results.Content(HataSayfasi(hata.Message), "text/html; charset=utf-8");
                }
            });

            // Otomasyon icin.
            group.MapGet("/json", async (IApkKatalogu katalog, CancellationToken iptal) =>
                Results.Ok(await katalog.SonlariGetirAsync(20, iptal)));

            // En son APK'ya dogrudan yonlendirme — telefondan tek adres yeter,
            // liste taramaya gerek kalmaz.
            group.MapGet("/son", async (IApkKatalogu katalog, CancellationToken iptal) =>
            {
                var dosyalar = await katalog.SonlariGetirAsync(1, iptal);
                return dosyalar.Count > 0
                    ? Results.Redirect(dosyalar[0].IndirmeUrl)
                    : Results.NotFound(new { message = "Henüz APK yüklenmemiş." });
            });
        }

        private static string SayfaOlustur(IReadOnlyList<ApkDosyasi> dosyalar)
        {
            var satirlar = dosyalar.Count == 0
                ? "<tr><td colspan='4' class='bos'>Henüz APK yok. Mobil depoda main'e merge edildiğinde burada görünür.</td></tr>"
                : string.Join("\n", dosyalar.Select((d, sira) => $"""
                    <tr>
                      <td class="zaman">{d.YuklenmeZamani:dd.MM.yyyy HH:mm}</td>
                      <td class="surum">
                        {(sira == 0 ? "<span class='rozet'>en yeni</span>" : "")}
                        <span class="commit">{Kacir(d.CommitKisa ?? "—")}</span>
                        {(d.CalistirmaNo is null ? "" : $"<span class='calistirma'>#{Kacir(d.CalistirmaNo)}</span>")}
                      </td>
                      <td class="boyut">{BoyutMetni(d.Boyut)}</td>
                      <td><a class="indir" href="{Kacir(d.IndirmeUrl)}">İndir</a></td>
                    </tr>
                    """));

            return $$"""
                <!doctype html>
                <html lang="tr">
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <meta name="robots" content="noindex,nofollow">
                <title>Test APK'ları</title>
                <style>
                  :root { color-scheme: light dark; }
                  body { font: 15px/1.5 system-ui, sans-serif; margin: 0; padding: 24px; }
                  h1 { font-size: 18px; margin: 0 0 4px; }
                  p.alt { margin: 0 0 20px; opacity: .65; font-size: 13px; }
                  table { border-collapse: collapse; width: 100%; max-width: 900px; }
                  th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid rgba(128,128,128,.3); }
                  th { font-size: 12px; text-transform: uppercase; letter-spacing: .04em; opacity: .6; }
                  .zaman, .boyut { font-family: ui-monospace, monospace; white-space: nowrap; }
                  .commit { font-family: ui-monospace, monospace; }
                  .calistirma { opacity: .5; font-size: 13px; margin-left: 6px; }
                  .rozet { font-size: 11px; background: #10b981; color: #fff; border-radius: 999px; padding: 2px 8px; margin-right: 6px; }
                  .indir { display: inline-block; background: #1e3a5f; color: #fff; text-decoration: none;
                           border-radius: 8px; padding: 8px 16px; font-weight: 600; font-size: 13px; }
                  .bos { text-align: center; padding: 40px; opacity: .5; }
                  .not { margin-top: 24px; max-width: 900px; font-size: 13px; opacity: .65; }
                  .hata { color: #e11d48; }
                </style>
                <h1>Test APK'ları</h1>
                <p class="alt">Mobil depoda main'e merge edilen her değişiklik burada belirir.</p>
                <table>
                  <tr><th>Tarih</th><th>Sürüm</th><th>Boyut</th><th></th></tr>
                  {{satirlar}}
                </table>
                <p class="not">
                  Telefondan indirip kurun. Kurulum sırasında “bilinmeyen kaynaklardan yükleme”
                  izni istenirse verin. Uygulama <strong>Salonbir Test</strong> adıyla, canlı
                  sürümün yanına ayrı kurulur.
                  <br><br>
                  Her zaman en yeniyi indirmek için: <code>/test/apk/son</code>
                </p>
                """;
        }

        private static string HataSayfasi(string mesaj) => $"""
            <!doctype html>
            <html lang="tr">
            <meta charset="utf-8">
            <meta name="robots" content="noindex,nofollow">
            <title>Test APK'ları</title>
            <body style="font: 15px/1.5 system-ui, sans-serif; padding: 24px;">
            <h1 style="font-size:18px">Test APK'ları</h1>
            <p style="color:#e11d48">Liste alınamadı: {Kacir(mesaj)}</p>
            """;

        private static string BoyutMetni(long bayt)
        {
            const long mb = 1024 * 1024;
            return bayt >= mb
                ? $"{bayt / (double)mb:0.#} MB"
                : $"{bayt / 1024d:0.#} KB";
        }

        private static string Kacir(string deger) =>
            deger.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
