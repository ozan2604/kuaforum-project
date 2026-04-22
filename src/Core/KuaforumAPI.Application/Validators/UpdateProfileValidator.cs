using FluentValidation;
using KuaforumAPI.Application.DTOs.Auth;

namespace KuaforumAPI.Application.Validators
{
    public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
    {
        public UpdateProfileValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^0[0-9]{10}$")
                .WithMessage("Telefon numarası 05XXXXXXXXX formatında olmalıdır.");
        }
    }
}
