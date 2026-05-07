using System;

namespace KuaforumAPI.Application.Constants
{
    public static class SmsTemplates
    {
        public static string AppointmentCreated(string shopName, DateTime startTime) =>
            $"Randevu talebiniz alındı. Salon: {shopName}, Tarih: {startTime:dd.MM.yyyy HH:mm}. Onaylandıktan sonra bilgilendirileceksiniz.";

        public static string AppointmentAutoConfirmed(string shopName, DateTime startTime) =>
            $"Randevunuz onaylandı! Salon: {shopName}, Tarih: {startTime:dd.MM.yyyy HH:mm}. Sizi bekliyoruz.";

        public static string AppointmentConfirmed(string shopName, DateTime startTime) =>
            $"Randevunuz onaylandı! Salon: {shopName}, Tarih: {startTime:dd.MM.yyyy HH:mm}. Sizi bekliyoruz.";

        public static string AppointmentRejected(string shopName, string? reason = null)
        {
            var msg = $"Randevu talebiniz reddedildi. Salon: {shopName}.";
            if (!string.IsNullOrWhiteSpace(reason)) msg += $" Neden: {reason}";
            return msg;
        }

        public static string AppointmentCancelledByShop(string shopName, DateTime startTime, string? reason = null)
        {
            var msg = $"Randevunuz iptal edildi. Salon: {shopName}, Tarih: {startTime:dd.MM.yyyy HH:mm}.";
            if (!string.IsNullOrWhiteSpace(reason)) msg += $" Neden: {reason}";
            return msg;
        }

        public static string AppointmentCancelledByCustomer(string customerName, string serviceName, DateTime startTime) =>
            $"Müşteri randevusunu iptal etti. Müşteri: {customerName}, Hizmet: {serviceName}, Tarih: {startTime:dd.MM.yyyy HH:mm}.";

        public static string AppointmentCompleted(string shopName, string serviceName) =>
            $"Hizmetiniz tamamlandı. Salon: {shopName}, Hizmet: {serviceName}. Teşekkür ederiz! Deneyiminizi değerlendirmeyi unutmayın.";

        public static string SalonApplicationApproved(string shopName) =>
            $"Tebrikler! '{shopName}' salon başvurunuz onaylandı. Artık salon panelinize erişebilirsiniz.";

        public static string SalonApplicationRejected() =>
            "Salon başvurunuz değerlendirildi ve uygun bulunmadı. Detaylı bilgi için destek ekibimizle iletişime geçebilirsiniz.";

        public static string EmployeeAdded(string shopName, string tempPassword) =>
            $"'{shopName}' salonuna çalışan olarak eklendiniz. Geçici şifreniz: {tempPassword}. İlk girişinizde şifrenizi değiştirmeniz önerilir.";

        public static string EmployeeAddedExisting(string shopName) =>
            $"'{shopName}' salonuna çalışan olarak eklendiniz. Mevcut şifrenizle giriş yapabilirsiniz.";

        public static string EmployeeRemoved(string shopName) =>
            $"'{shopName}' salonundaki çalışan kaydınız sonlandırıldı.";

        public static string EmployeeRestored(string shopName) =>
            $"'{shopName}' salonuna çalışan olarak yeniden aktif edildiniz. Panelinize erişebilirsiniz.";

        public static string PasswordChanged() =>
            "Hesabınızın şifresi başarıyla değiştirildi. Bu işlemi siz yapmadıysanız lütfen destek ekibimizle iletişime geçin.";
    }
}
