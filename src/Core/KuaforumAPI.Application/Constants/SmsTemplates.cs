using System;
using System.Collections.Generic;
using System.Linq;

namespace KuaforumAPI.Application.Constants
{
    public static class SmsTemplates
    {
        // Tarih formatı: 10.05.2026 14:00
        private static string Fmt(DateTime dt) => dt.ToString("dd.MM.yyyy HH:mm");
        private static string FmtDate(DateTime dt) => dt.ToString("dd.MM.yyyy");

        // ── Müşteriye giden randevu SMS'leri ──────────────────────────────────

        public static string AppointmentCreated(string shopName, DateTime startTime) =>
            $"Randevu talebiniz alındı. {shopName} - {Fmt(startTime)}. Onaylanınca bilgilendirileceksiniz.";

        public static string AppointmentAutoConfirmed(string shopName, DateTime startTime) =>
            $"Randevunuz onaylandı. {shopName} - {Fmt(startTime)}. Görüşmek üzere!";

        public static string AppointmentConfirmed(string shopName, DateTime startTime) =>
            $"Randevunuz onaylandı. {shopName} - {Fmt(startTime)}. Görüşmek üzere!";

        public static string AppointmentRejected(string shopName, DateTime startTime, string? reason = null)
        {
            var msg = $"{FmtDate(startTime)} tarihindeki {shopName} randevu talebiniz reddedildi.";
            if (!string.IsNullOrWhiteSpace(reason)) msg += $" Sebep: {reason}";
            return msg;
        }

        public static string AppointmentCancelledByShop(string shopName, DateTime startTime, string? reason = null)
        {
            var msg = $"{FmtDate(startTime)} tarihindeki {shopName} randevunuz iptal edildi.";
            if (!string.IsNullOrWhiteSpace(reason)) msg += $" Sebep: {reason}";
            return msg;
        }

        public static string AppointmentCompleted(string shopName) =>
            $"Randevunuz tamamlandı. Bizi tercih ettiğiniz için teşekkürler! Aldığınız hizmeti profilinizden değerlendirmeyi unutmayın.";

        public static string AppointmentReminder48h(string shopName, DateTime startTime) =>
            $"Yarın randevunuz var! {shopName} - {startTime:HH:mm}. Görüşürüz.";

        public static string AppointmentReminder2h(string shopName, DateTime startTime) =>
            $"Randevunuz {startTime:HH:mm}'de başlıyor. {shopName} sizi bekliyor!";

        // ── Salon sahibine giden SMS'ler ─────────────────────────────────────

        public static string AppointmentCancelledByCustomer(string customerName, string serviceName, DateTime startTime) =>
            $"{FmtDate(startTime)} tarihindeki {customerName} adlı müşterinin {serviceName} randevusu iptal edildi.";

        public static string NewAppointmentSummaryForShop(IList<(string Name, int Count)> summary)
        {
            var details = string.Join(", ", summary.Select(s => $"{s.Name} {s.Count}"));
            var msg = $"Son 30 dak. yeni randevular: {details}. Detay için salon panelini kontrol edin.";
            if (msg.Length <= 155) return msg;
            var total = summary.Sum(s => s.Count);
            return $"Son 30 dak. {total} yeni randevu oluşturuldu. Detay için salon panelini kontrol edin.";
        }

        // ── Çalışana giden randevu SMS'leri ──────────────────────────────────

        public static string NewAppointmentForEmployee(string customerName, string serviceName, DateTime startTime) =>
            $"Yeni randevunuz: {Fmt(startTime)}, {serviceName}. Müşteri: {customerName}.";

        public static string AppointmentCancelledByCustomerToEmployee(string customerName, string serviceName, DateTime startTime) =>
            $"Randevunuz iptal edildi: {Fmt(startTime)}, {serviceName}. İptal eden: {customerName}.";

        public static string AppointmentRejectedToEmployee(string shopName, DateTime startTime) =>
            $"{Fmt(startTime)} tarihindeki randevunuz salon tarafından reddedildi. ({shopName})";

        public static string AppointmentCancelledByShopToEmployee(string shopName, DateTime startTime) =>
            $"{Fmt(startTime)} tarihindeki randevunuz salon tarafından iptal edildi. ({shopName})";

        // ── Salon başvuru SMS'leri ───────────────────────────────────────────

        public static string SalonApplicationSubmitted() =>
            "Kuaforum: Salon başvurunuz alındı. Değerlendirme sonucunda SMS ile bilgilendirileceksiniz.";

        public static string SalonApplicationApproved(string shopName) =>
            $"Tebrikler! {shopName} başvurunuz onaylandı. Salon panelinize erişebilirsiniz.";

        public static string SalonApplicationRejected() =>
            "Salon başvurunuz reddedildi. Bilgi için destek hattımızla iletişime geçin.";

        public static string NewSalonApplicationToAdmin(string shopName) =>
            $"Sisteme yeni bir dükkan başvurusu geldi: {shopName}. Lütfen onay için admin panelini kontrol edin.";

        // ── Çalışan yönetimi SMS'leri ────────────────────────────────────────

        public static string EmployeeAdded(string shopName, string tempPassword) =>
            $"{shopName} salonuna çalışan olarak eklendiniz. Geçici şifreniz: {tempPassword}";

        public static string EmployeeAddedExisting(string shopName) =>
            $"{shopName} salonuna çalışan olarak eklendiniz. Mevcut şifrenizle giriş yapabilirsiniz.";

        public static string EmployeeRemoved(string shopName) =>
            $"{shopName} salonundaki çalışan kaydınız sonlandırıldı.";

        public static string EmployeeRestored(string shopName) =>
            $"{shopName} salonuna yeniden aktif edildiniz. Panelinize erişebilirsiniz.";

        // ── Kimlik / güvenlik SMS'leri ───────────────────────────────────────

        public static string PasswordChanged() =>
            "Şifreniz değiştirildi. Bu işlemi siz yapmadıysanız destek hattımızla iletişime geçin.";

        public static string GuestAccountCreated(string tempPassword) =>
            $"Kuaforum hesabınız oluşturuldu. Kullanıcı adınız telefon numaranız, geçici şifreniz: {tempPassword} — salonbir.com adresinden giriş yapabilirsiniz.";
    }
}
