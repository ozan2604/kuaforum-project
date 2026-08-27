using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.Infrastructure.Services
{
    /// <summary>
    /// Yakalanan SMS'leri bellekte tutar — sinirli sayida, en yeniler kalir.
    ///
    /// Bellek bilincli bir tercih: uygulama yeniden baslayinca kayitlar
    /// silinir, yani test kodlari kalici bir yerde birikmez. Tek ornekli
    /// (single instance) calisma icin yeterli; olceklenirse her ornek kendi
    /// listesini tutar, bu da test ortami icin kabul edilebilir.
    /// </summary>
    public sealed class InMemoryCapturedSmsStore : ICapturedSmsStore
    {
        private const int Capacity = 50;

        private readonly LinkedList<CapturedSms> _items = new();
        private readonly Lock _gate = new();

        public void Add(CapturedSms sms)
        {
            lock (_gate)
            {
                _items.AddFirst(sms);
                while (_items.Count > Capacity)
                    _items.RemoveLast();
            }
        }

        public IReadOnlyList<CapturedSms> Recent(int count = 20)
        {
            lock (_gate)
            {
                return _items.Take(Math.Clamp(count, 1, Capacity)).ToList();
            }
        }

        public CapturedSms? LastFor(string phoneNumber)
        {
            var normalized = Normalize(phoneNumber);

            lock (_gate)
            {
                return _items.FirstOrDefault(x => Normalize(x.PhoneNumber) == normalized);
            }
        }

        /// <summary>
        /// Numarayi son 10 haneye indirger; boylece "05551112233",
        /// "5551112233" ve "+905551112233" ayni kayda denk gelir.
        /// </summary>
        private static string Normalize(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length > 10 ? digits[^10..] : digits;
        }
    }
}
