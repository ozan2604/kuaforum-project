using FluentValidation;
using KuaforumAPI.Application.DTOs.Service;

namespace KuaforumAPI.Application.Validators
{
    public class CreateServiceCategoryValidator : AbstractValidator<CreateServiceCategoryDto>
    {
        public CreateServiceCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(50).WithMessage("Category name must not exceed 50 characters.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.Description)
                .MaximumLength(250).WithMessage("Description must not exceed 250 characters.")
                .Must(x => x == null || (!x.Contains('<') && !x.Contains('>'))).WithMessage("Geçersiz karakter içeriyor.");
        }
    }

    public class CreateShopServiceValidator : AbstractValidator<CreateShopServiceDto>
    {
        public CreateShopServiceValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Service name is required.")
                .MaximumLength(100).WithMessage("Service name must not exceed 100 characters.")
                .Must(x => !x.Contains('<') && !x.Contains('>')).WithMessage("Geçersiz karakter içeriyor.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.");

            RuleFor(x => x.Duration)
                .GreaterThan(0).WithMessage("Duration must be greater than 0.");
        }
    }
}
