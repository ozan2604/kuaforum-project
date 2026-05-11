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
            $"Randevu talebiniz alindı. {shopName} - {Fmt(startTime)}. Onaylaninca bilgilendirileceksiniz.";

        public static string AppointmentAutoConfirmed(string shopName, DateTime startTime) =>
            $"Randevunuz onaylandi. {shopName} - {Fmt(startTime)}. Görüşmek üzere!";

        public static string AppointmentConfirmed(string shopName, DateTime startTime) =>
            $"Randevunuz onaylandi. {shopName} - {Fmt(startTime)}. Görüşmek üzere!";

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
            $"Randevunuz tamamlandi. {shopName}'yi tercih ettiginiz icin tesekkurler!";

        public static string AppointmentReminder48h(string shopName, DateTime startTime) =>
            $"Yarin randevunuz var! {shopName} - {startTime:HH:mm}. Görüşürüz.";

        public static string AppointmentReminder2h(string shopName, DateTime startTime) =>
            $"Randevunuz {startTime:HH:mm}'de basliyor. {shopName} sizi bekliyor!";

        // ── Salon sahibine giden SMS'ler ─────────────────────────────────────

        public static string AppointmentCancelledByCustomer(string customerName, string serviceName, DateTime startTime) =>
            $"{FmtDate(startTime)} tarihindeki {customerName} adli musterinin {serviceName} randevusu iptal edildi.";

        public static string NewAppointmentSummaryForShop(IList<(string Name, int Count)> summary)
        {
            var details = string.Join(", ", summary.Select(s => $"{s.Name} {s.Count}"));
            var msg = $"Son 30 dak. yeni randevular: {details}. Detay icin salon panelini kontrol edin.";
            if (msg.Length <= 155) return msg;
            var total = summary.Sum(s => s.Count);
            return $"Son 30 dak. {total} yeni randevu olusturuldu. Detay icin salon panelini kontrol edin.";
        }

        // ── Calısana giden randevu SMS'leri ──────────────────────────────────

        public static string NewAppointmentForEmployee(string customerName, string serviceName, DateTime startTime) =>
            $"Yeni randevunuz: {Fmt(startTime)}, {serviceName}. Musteri: {customerName}.";

        public static string AppointmentCancelledByCustomerToEmployee(string customerName, string serviceName, DateTime startTime) =>
            $"Randevunuz iptal edildi: {Fmt(startTime)}, {serviceName}. Iptal eden: {customerName}.";

        // ── Salon basvuru SMS'leri ───────────────────────────────────────────

        public static string SalonApplicationApproved(string shopName) =>
            $"Tebrikler! {shopName} basvurunuz onaylandi. Salon panelinize erisebilirsiniz.";

        public static string SalonApplicationRejected() =>
            "Salon basvurunuz reddedildi. Bilgi icin destek hattimizla iletisime gecin.";

        // ── Calisán yönetimi SMS'leri ────────────────────────────────────────

        public static string EmployeeAdded(string shopName, string tempPassword) =>
            $"{shopName} salonuna calisan olarak eklendiniz. Gecici sifreniz: {tempPassword}";

        public static string EmployeeAddedExisting(string shopName) =>
            $"{shopName} salonuna calisan olarak eklendiniz. Mevcut sifrenizle giris yapabilirsiniz.";

        public static string EmployeeRemoved(string shopName) =>
            $"{shopName} salonundaki calisan kaydiniz sonlandirildi.";

        public static string EmployeeRestored(string shopName) =>
            $"{shopName} salonuna yeniden aktif edildiniz. Panelinize erisebilirsiniz.";

        // ── Kimlik / guvenlik SMS'leri ───────────────────────────────────────

        public static string PasswordChanged() =>
            "Sifreniz degistirildi. Bu islemi siz yapmadiysaniz destek hattimizla iletisime gecin.";
    }
}
