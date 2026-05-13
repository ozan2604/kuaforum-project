using FluentValidation;
using KuaforumAPI.Application.DTOs.Employee;

namespace KuaforumAPI.Application.Validators
{
    public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
    {
        public CreateEmployeeValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad zorunludur.")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad zorunludur.")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Telefon numarası zorunludur.")
                .Matches(@"^05\d{9}$").WithMessage("Telefon numarası 05XXXXXXXXX formatında olmalıdır.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Unvan zorunludur.")
                .MaximumLength(100).WithMessage("Unvan en fazla 100 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");
        }
    }
}
