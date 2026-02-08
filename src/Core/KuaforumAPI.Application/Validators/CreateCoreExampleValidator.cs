using FluentValidation;
using KuaforumAPI.Application.DTOs;

namespace KuaforumAPI.Application.Validators
{
    public class CreateCoreExampleValidator : AbstractValidator<CreateCoreExampleDto>
    {
        public CreateCoreExampleValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(200).WithMessage("Description must not exceed 200 characters.");
        }
    }
}
