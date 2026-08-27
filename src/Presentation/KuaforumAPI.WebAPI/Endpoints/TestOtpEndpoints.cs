using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.WebAPI.Endpoints
{
    /// <summary>
    /// Test ortaminda yakalanan OTP kodlarini gosteren uclar.
    ///
    /// Production'da HIC map edilmez — orada bu adresler 404 doner. Kayit
    /// <see cref="MapTestOtpEndpoints"/> cagrisinin icinde ortam kontrolune
    /// bagli oldugu icin, yanlislikla acik kalmasi mumkun degil.
    ///
    /// Erisim ayrica Cloudflare Access ile e-posta listesine kisitli.
    /// </summary>
    public static class TestOtpEndpoints
    {
        public static void MapTestOtpEndpoints(this WebApplication app)
        {
            if (app.Environment.IsProduction())
                return;

            var group = app.MapGroup("/test/otp").ExcludeFromDescription();

            // Insan icin: tarayicida acilan, kendini yenileyen basit liste.
            group.MapGet("/", (ICapturedSmsStore store) =>
                Results.Content(BuildPage(store.Recent(20)), "text/html; charset=utf-8"));

            // Otomasyon icin: son mesajlar.
            group.MapGet("/json", (ICapturedSmsStore store) =>
                Results.Ok(store.Recent(20)));

            // Otomasyon icin: belirli numaranin son kodu.
            group.MapGet("/{phone}", (string phone, ICapturedSmsStore store) =>
                store.LastFor(phone) is { } sms
                    ? Results.Ok(sms)
                    : Results.NotFound(new { message = "Bu numaraya ait kayit yok." }));
        }

        private static string BuildPage(IReadOnlyList<CapturedSms> messages)
        {
            var rows = messages.Count == 0
                ? "<tr><td colspan='4' class='bos'>Henüz kod yok. Giriş ekranından kod isteyin.</td></tr>"
                : string.Join("\n", messages.Select(m => $"""
                    <tr>
                      <td class="zaman">{m.CapturedAt:HH:mm:ss}</td>
                      <td class="tel">{Escape(m.PhoneNumber)}</td>
                      <td class="kod">{Escape(m.Code ?? "—")}</td>
                      <td class="mesaj">{Escape(m.Message)}</td>
                    </tr>
                    """));

            return $$"""
                <!doctype html>
                <html lang="tr">
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <meta name="robots" content="noindex,nofollow">
                <meta http-equiv="refresh" content="5">
                <title>Test OTP kodları</title>
                <style>
                  :root { color-scheme: light dark; }
                  body { font: 15px/1.5 system-ui, sans-serif; margin: 0; padding: 24px; }
                  h1 { font-size: 18px; margin: 0 0 4px; }
                  p.alt { margin: 0 0 20px; opacity: .65; font-size: 13px; }
                  table { border-collapse: collapse; width: 100%; max-width: 900px; }
                  th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid rgba(128,128,128,.3); }
                  th { font-size: 12px; text-transform: uppercase; letter-spacing: .04em; opacity: .6; }
                  .kod { font-family: ui-monospace, monospace; font-size: 20px; font-weight: 600; letter-spacing: .08em; }
                  .zaman, .tel { font-family: ui-monospace, monospace; white-space: nowrap; }
                  .mesaj { opacity: .7; font-size: 13px; }
                  .bos { text-align: center; padding: 40px; opacity: .5; }
                </style>
                <h1>Test OTP kodları</h1>
                <p class="alt">SMS gönderilmiyor — kodlar burada görünür. 5 saniyede bir yenilenir.</p>
                <table>
                  <tr><th>Saat</th><th>Numara</th><th>Kod</th><th>Mesaj</th></tr>
                  {{rows}}
                </table>
                """;
        }

        private static string Escape(string value) =>
            value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
