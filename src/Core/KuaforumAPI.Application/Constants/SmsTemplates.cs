using System;

namespace KuaforumAPI.Application.Constants
{
    public static class SmsTemplates
    {
        public static string AppointmentCreated(string shopName, DateTime startTime) =>
            $"Randevu talebiniz alındı. {shopName}, {startTime:dd.MM.yyyy HH:mm}. Onay için bekleyiniz.";

        public static string AppointmentAutoConfirmed(string shopName, DateTime startTime) =>
            $"Randevunuz onaylandı! {shopName}, {startTime:dd.MM.yyyy HH:mm}. Sizi bekliyoruz.";

        public static string AppointmentConfirmed(string shopName, DateTime startTime) =>
            $"Randevunuz onaylandı! {shopName}, {startTime:dd.MM.yyyy HH:mm}. Sizi bekliyoruz.";

        public static string AppointmentRejected(string shopName, string? reason = null)
        {
            var msg = $"Randevu talebiniz reddedildi. {shopName}.";
            if (!string.IsNullOrWhiteSpace(reason)) msg += $" {reason}";
            return msg;
        }

        public static string AppointmentCancelledByShop(string shopName, DateTime startTime, string? reason = null)
        {
            var msg = $"Randevunuz iptal edildi. {shopName}, {startTime:dd.MM.yyyy HH:mm}.";
            if (!string.IsNullOrWhiteSpace(reason)) msg += $" {reason}";
            return msg;
        }

        public static string AppointmentCancelledByCustomer(string customerName, string serviceName, DateTime startTime) =>
            $"Randevu iptal edildi. Müşteri: {customerName}, {serviceName}, {startTime:dd.MM.yyyy HH:mm}.";

        public static string AppointmentCompleted(string shopName) =>
            $"Hizmetiniz tamamlandı. {shopName}. Teşekkür ederiz!";

        public static string AppointmentReminder48h(string shopName, DateTime startTime) =>
            $"Yarın randevunuz var! {shopName}, {startTime:HH:mm}. Görüşürüz.";

        public static string AppointmentReminder2h(string shopName, DateTime startTime) =>
            $"Randevunuz {startTime:HH:mm}'de başlıyor. {shopName} sizi bekliyor!";

        public static string SalonApplicationApproved(string shopName) =>
            $"Tebrikler! {shopName} başvurunuz onaylandı. Salon panelinize erişebilirsiniz.";

        public static string SalonApplicationRejected() =>
            "Salon başvurunuz reddedildi. Bilgi için destek hattımızla iletişime geçin.";

        public static string EmployeeAdded(string shopName, string tempPassword) =>
            $"{shopName} salonuna çalışan olarak eklendiniz. Şifreniz: {tempPassword}";

        public static string EmployeeAddedExisting(string shopName) =>
            $"{shopName} salonuna çalışan olarak eklendiniz. Mevcut şifrenizle giriş yapabilirsiniz.";

        public static string EmployeeRemoved(string shopName) =>
            $"{shopName} salonundaki çalışan kaydınız sonlandırıldı.";

        public static string EmployeeRestored(string shopName) =>
            $"{shopName} salonuna yeniden aktif edildiniz. Panelinize erişebilirsiniz.";

        public static string PasswordChanged() =>
            "Şifreniz değiştirildi. Bu işlemi siz yapmadıysanız destek hattımızla iletişime geçin.";
    }
}
