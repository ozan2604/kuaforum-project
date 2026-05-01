using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Application.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Web;

namespace KuaforumAPI.Infrastructure.Services
{
    public class NetGsmSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly NetGsmSettings _settings;

        public NetGsmSmsService(IHttpClientFactory httpClientFactory, IOptions<NetGsmSettings> settings)
        {
            _httpClient = httpClientFactory.CreateClient("netgsm");
            _settings = settings.Value;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            // DB'de 05XXXXXXXXX formatında, NetGSM 905XXXXXXXXX ister
            var netgsmPhone = phoneNumber.StartsWith("0")
                ? "9" + phoneNumber
                : phoneNumber;

            var encodedText = HttpUtility.UrlEncode(message);
            var url = $"https://api.netgsm.com.tr/sms/send/get/?usercode={_settings.UserCode}&password={_settings.Password}&gsmno={netgsmPhone}&text={encodedText}&msgheader={_settings.MessageHeader}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // NetGSM başarılı yanıt "00" ile başlar
            if (!content.TrimStart().StartsWith("00"))
                throw new InvalidOperationException($"SMS gönderilemedi. NetGSM yanıtı: {content}");
        }
    }
}
