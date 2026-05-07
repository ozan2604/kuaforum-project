using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/sms-test")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class SmsTestController : ControllerBase
    {
        private readonly ISmsService _smsService;

        public SmsTestController(ISmsService smsService)
        {
            _smsService = smsService;
        }

        /// <summary>
        /// Tüm SMS şablonlarını örnek içeriklerle listeler.
        /// TestPhoneOverride dolu ise o numaraya gönderilir; boşsa verilen phone parametresi kullanılır.
        /// </summary>
        [HttpGet("templates")]
        public IActionResult GetTemplates()
        {
            var now = DateTime.Now;
            var templates = new List<object>
            {
                new { name = "AppointmentCreated",           preview = SmsTemplates.AppointmentCreated("Vefa Kuaförü", now.AddDays(2)) },
                new { name = "AppointmentAutoConfirmed",     preview = SmsTemplates.AppointmentAutoConfirmed("Vefa Kuaförü", now.AddDays(2)) },
                new { name = "AppointmentConfirmed",         preview = SmsTemplates.AppointmentConfirmed("Vefa Kuaförü", now.AddDays(2)) },
                new { name = "AppointmentRejected",          preview = SmsTemplates.AppointmentRejected("Vefa Kuaförü", "Uygun personel yok.") },
                new { name = "AppointmentCancelledByShop",   preview = SmsTemplates.AppointmentCancelledByShop("Vefa Kuaförü", now.AddDays(2), "Acil kapanış.") },
                new { name = "AppointmentCancelledByCustomer", preview = SmsTemplates.AppointmentCancelledByCustomer("Ahmet Yılmaz", "Saç Kesimi", now.AddDays(1)) },
                new { name = "AppointmentCompleted",         preview = SmsTemplates.AppointmentCompleted("Vefa Kuaförü", "Saç Kesimi") },
                new { name = "SalonApplicationApproved",     preview = SmsTemplates.SalonApplicationApproved("Vefa Kuaförü") },
                new { name = "SalonApplicationRejected",     preview = SmsTemplates.SalonApplicationRejected() },
                new { name = "EmployeeAdded",                preview = SmsTemplates.EmployeeAdded("Vefa Kuaförü", "Ab3!xY7@") },
                new { name = "EmployeeAddedExisting",        preview = SmsTemplates.EmployeeAddedExisting("Vefa Kuaförü") },
                new { name = "EmployeeRemoved",              preview = SmsTemplates.EmployeeRemoved("Vefa Kuaförü") },
                new { name = "EmployeeRestored",             preview = SmsTemplates.EmployeeRestored("Vefa Kuaförü") },
                new { name = "PasswordChanged",              preview = SmsTemplates.PasswordChanged() },
            };

            return Ok(templates);
        }

        /// <summary>
        /// Belirtilen şablonu gönderir.
        /// phone: TestPhoneOverride boşsa bu numaraya gönderilir (ör: 05xxxxxxxxx).
        /// </summary>
        [HttpPost("send/{templateName}")]
        public async Task<IActionResult> Send(string templateName, [FromQuery] string phone = "05000000000")
        {
            var now = DateTime.Now;

            var message = templateName switch
            {
                "AppointmentCreated"            => SmsTemplates.AppointmentCreated("Vefa Kuaförü", now.AddDays(2)),
                "AppointmentAutoConfirmed"      => SmsTemplates.AppointmentAutoConfirmed("Vefa Kuaförü", now.AddDays(2)),
                "AppointmentConfirmed"          => SmsTemplates.AppointmentConfirmed("Vefa Kuaförü", now.AddDays(2)),
                "AppointmentRejected"           => SmsTemplates.AppointmentRejected("Vefa Kuaförü", "Uygun personel yok."),
                "AppointmentCancelledByShop"    => SmsTemplates.AppointmentCancelledByShop("Vefa Kuaförü", now.AddDays(2), "Acil kapanış."),
                "AppointmentCancelledByCustomer"=> SmsTemplates.AppointmentCancelledByCustomer("Ahmet Yılmaz", "Saç Kesimi", now.AddDays(1)),
                "AppointmentCompleted"          => SmsTemplates.AppointmentCompleted("Vefa Kuaförü", "Saç Kesimi"),
                "SalonApplicationApproved"      => SmsTemplates.SalonApplicationApproved("Vefa Kuaförü"),
                "SalonApplicationRejected"      => SmsTemplates.SalonApplicationRejected(),
                "EmployeeAdded"                 => SmsTemplates.EmployeeAdded("Vefa Kuaförü", "Ab3!xY7@"),
                "EmployeeAddedExisting"         => SmsTemplates.EmployeeAddedExisting("Vefa Kuaförü"),
                "EmployeeRemoved"               => SmsTemplates.EmployeeRemoved("Vefa Kuaförü"),
                "EmployeeRestored"              => SmsTemplates.EmployeeRestored("Vefa Kuaförü"),
                "PasswordChanged"               => SmsTemplates.PasswordChanged(),
                _ => null
            };

            if (message == null)
                return BadRequest(new { error = $"'{templateName}' şablonu bulunamadı. GET /api/sms-test/templates ile listeye bakın." });

            try
            {
                await _smsService.SendSmsAsync(phone, message);
                return Ok(new { success = true, sentTo = phone, template = templateName, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
