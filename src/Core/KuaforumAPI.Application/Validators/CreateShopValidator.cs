using FluentValidation;
using KuaforumAPI.Application.DTOs.Shop;

namespace KuaforumAPI.Application.Validators
{
    public class CreateShopValidator : AbstractValidator<CreateShopDto>
    {
        public CreateShopValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Salon adı zorunludur.")
                .MaximumLength(100).WithMessage("Salon adı en fazla 100 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Adres zorunludur.")
                .MaximumLength(250).WithMessage("Adres en fazla 250 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("İl zorunludur.")
                .MaximumLength(50).WithMessage("İl en fazla 50 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.District)
                .NotEmpty().WithMessage("İlçe zorunludur.")
                .MaximumLength(50).WithMessage("İlçe en fazla 50 karakter olabilir.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Telefon numarası zorunludur.")
                .Matches(@"^05\d{9}$").WithMessage("Telefon numarası 05XXXXXXXXX formatında olmalıdır.");

            RuleFor(x => x.CategoryIds)
                .NotEmpty().WithMessage("En az bir kategori seçimi zorunludur.");
        }
    }
}
