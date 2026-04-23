using FluentValidation;
using KuaforumAPI.Application.DTOs.Shop;

namespace KuaforumAPI.Application.Validators
{
    public class CreateShopValidator : AbstractValidator<CreateShopDto>
    {
        public CreateShopValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Shop name is required.")
                .MaximumLength(150).WithMessage("Shop name must not exceed 150 characters.");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(50).WithMessage("City must not exceed 50 characters.");

            RuleFor(x => x.District)
                .NotEmpty().WithMessage("District is required.")
                .MaximumLength(50).WithMessage("District must not exceed 50 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Telefon numarası zorunludur.")
                .Matches(@"^05\d{9}$").WithMessage("Telefon numarası 05XXXXXXXXX formatında olmalıdır. (Örn: 05321234567)");

            RuleFor(x => x.CategoryIds)
                .NotEmpty().WithMessage("En az bir kategori seçimi zorunludur.");
        }
    }
}
