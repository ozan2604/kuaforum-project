using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Infrastructure.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace KuaforumAPI.UnitTests.Infrastructure;

/// <summary>
/// Test ortamindaki OTP teslimat kanali.
///
/// Buradaki en kritik test <see cref="ProductionOrtamindaOrneklenemez"/>:
/// bu sinif canliya sizarsa SMS'ler sessizce yutulur ve hicbir kullanici
/// giris yapamaz — ustelik hata da gorunmez. Sessiz basarisizlik yerine
/// acik patlama tercih ediliyor.
/// </summary>
public class CapturedSmsServiceTests
{
    private static CapturedSmsService Servis(ICapturedSmsStore store, string ortam = "Development") =>
        new(store, new SahteOrtam(ortam), NullLogger<CapturedSmsService>.Instance);

    [Fact]
    public void ProductionOrtamindaOrneklenemez()
    {
        var hata = Assert.Throws<InvalidOperationException>(
            () => Servis(new InMemoryCapturedSmsStore(), "Production"));

        Assert.Contains("Production", hata.Message);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Staging")]
    public void ProductionDisiOrtamlardaCalisir(string ortam)
    {
        var istisna = Record.Exception(() => Servis(new InMemoryCapturedSmsStore(), ortam));
        Assert.Null(istisna);
    }

    [Fact]
    public async Task MesajGonderilmezYakalanir()
    {
        var depo = new InMemoryCapturedSmsStore();

        await Servis(depo).SendSmsAsync("05551112233", "Salonbir dogrulama kodunuz: 483920");

        var kayit = Assert.Single(depo.Recent());
        Assert.Equal("05551112233", kayit.PhoneNumber);
        Assert.Equal("483920", kayit.Code);
    }

    [Theory]
    [InlineData("Kodunuz: 123456", "123456")]
    [InlineData("123456 kodu ile giris yapin", "123456")]
    [InlineData("Kod yok bu mesajda", null)]
    [InlineData("Randevunuz 15:30'da onaylandi", null)]      // saat kod sanilmamali
    [InlineData("Siparis 1234567 hazir", null)]              // 7 hane kod degil
    public async Task AltiHaneliKodAyiklanir(string mesaj, string? beklenen)
    {
        var depo = new InMemoryCapturedSmsStore();

        await Servis(depo).SendSmsAsync("05551112233", mesaj);

        Assert.Equal(beklenen, depo.Recent()[0].Code);
    }

    /// <summary>Test icin en basit IHostEnvironment.</summary>
    private sealed class SahteOrtam(string ortam) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = ortam;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
