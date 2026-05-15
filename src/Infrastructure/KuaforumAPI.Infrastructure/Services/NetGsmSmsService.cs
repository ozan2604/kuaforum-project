using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace KuaforumAPI.Infrastructure.Services
{
    public class NetGsmSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly NetGsmSettings _settings;
        private readonly ILogger<NetGsmSmsService> _logger;

        // NetGSM SMS POST/XML endpoint
        private const string NetGsmPostUrl = "https://api.netgsm.com.tr/sms/send/xml";

        public NetGsmSmsService(IHttpClientFactory httpClientFactory, IOptions<NetGsmSettings> settings, ILogger<NetGsmSmsService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("netgsm");
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            string targetPhone;
            string finalMessage;

            if (!string.IsNullOrWhiteSpace(_settings.TestPhoneOverride))
            {
                targetPhone = _settings.TestPhoneOverride;
                finalMessage = message;
            }
            else
            {
                targetPhone = phoneNumber;
                finalMessage = message;
            }

            // NetGSM telefon formatı: 5XXXXXXXXX (10 hane, başında 0 veya 90 YOK)
            // DB'de 05XXXXXXXXX → başındaki 0'ı kaldır → 5XXXXXXXXX
            // Eğer 905XXXXXXXXX gelirse → baştaki 90'ı kaldır → 5XXXXXXXXX
            var netgsmPhone = targetPhone.TrimStart();
            if (netgsmPhone.StartsWith("90"))
                netgsmPhone = netgsmPhone[2..];         // 905... → 5...
            else if (netgsmPhone.StartsWith("0"))
                netgsmPhone = netgsmPhone[1..];         // 05...  → 5...

            if (finalMessage.Length > 155)
                _logger.LogWarning("SMS metni {Len} karakter — 160 karakter sınırına yakın veya üzerinde! NetGSM hata 20 dönebilir.", finalMessage.Length);

            _logger.LogInformation("SMS gönderiliyor -> Hedef: {Phone}, Test: {IsTest}, Header: {Header}, Karakter: {Len}, Metin: {Msg}",
                netgsmPhone,
                !string.IsNullOrWhiteSpace(_settings.TestPhoneOverride),
                _settings.MessageHeader,
                finalMessage.Length,
                finalMessage);

            // NetGSM XML formatı (1:n = tek mesaj, çok/tek alıcı):
            //   - <company dil="TR"> : zorunlu
            //   - <type>1:n</type>   : zorunlu
            //   - <msg> CDATA içinde : Türkçe/özel karakterler için zorunlu
            //   - <no> formatı       : 5XXXXXXXXX (10 hane, başında 0 veya 90 YOK)
            var xmlBody = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <mainbody>
                  <header>
                    <company dil="TR">Netgsm</company>
                    <usercode>{_settings.UserCode}</usercode>
                    <password>{_settings.Password}</password>
                    <type>1:n</type>
                    <msgheader>{_settings.MessageHeader}</msgheader>
                  </header>
                  <body>
                    <msg><![CDATA[{finalMessage}]]></msg>
                    <no>{netgsmPhone}</no>
                  </body>
                </mainbody>
                """;

            // UTF-8 ile encode et (XML declaration'da UTF-8 belirtiyoruz)
            var bodyBytes = Encoding.UTF8.GetBytes(xmlBody);

            using var content = new ByteArrayContent(bodyBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml")
            {
                CharSet = "UTF-8"
            };

            _logger.LogInformation("NetGSM POST isteği: {Url}\nXML:\n{Xml}", NetGsmPostUrl, xmlBody);

            var response = await _httpClient.PostAsync(NetGsmPostUrl, content);
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("NetGSM yanıtı: [{Content}]", responseText.Trim());

            if (!responseText.TrimStart().StartsWith("00"))
            {
                _logger.LogError("SMS gönderilemedi. Hedef: {Phone}, Karakter: {Len}, Yanıt: {Content}",
                    netgsmPhone, finalMessage.Length, responseText.Trim());
                throw new InvalidOperationException($"SMS gönderilemedi. NetGSM yanıtı: {responseText.Trim()}");
            }
        }
    }
}
