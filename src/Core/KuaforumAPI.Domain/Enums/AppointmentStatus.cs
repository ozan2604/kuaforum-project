namespace KuaforumAPI.Domain.Enums
{
    public enum AppointmentStatus
    {
        Pending = 0,    // Onay bekliyor
        Confirmed = 1,  // Onaylandı
        Completed = 2,  // İşlem tamamlandı
        Cancelled = 3,  // Müşteri iptal etti
        Rejected = 4,   // Dükkan sahibi reddetti
        NoShow = 5      // Müşteri gelmedi (terminal)
    }
}
