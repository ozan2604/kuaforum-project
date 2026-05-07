using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Employee
{
    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "Ad zorunludur.")]
        [MaxLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [MaxLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon numarası 05XXXXXXXXX formatında olmalıdır.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Ünvan zorunludur.")]
        [MaxLength(100, ErrorMessage = "Ünvan en fazla 100 karakter olabilir.")]
        public string Title { get; set; }
    }
}
