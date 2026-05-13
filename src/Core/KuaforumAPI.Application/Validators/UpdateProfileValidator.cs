using FluentValidation;
using KuaforumAPI.Application.DTOs.Auth;

namespace KuaforumAPI.Application.Validators
{
    public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
    {
        public UpdateProfileValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad zorunludur.")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad zorunludur.")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Telefon numarası zorunludur.")
                .Matches(@"^05[0-9]{9}$").WithMessage("Telefon numarası 05XXXXXXXXX formatında olmalıdır.");
        }
    }
}
