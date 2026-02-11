using FluentValidation;
using KuaforumAPI.Application.DTOs.Auth;

namespace KuaforumAPI.Application.Validators
{
    public class UpdateAddressValidator : AbstractValidator<UpdateAddressDto>
    {
        public UpdateAddressValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(50);
            RuleFor(x => x.City).NotEmpty().MaximumLength(50);
            RuleFor(x => x.District).NotEmpty().MaximumLength(50);
            RuleFor(x => x.OpenAddress).NotEmpty().MaximumLength(200);
        }
    }
}
