using FluentValidation;
using KuaforumAPI.Application.DTOs.Auth;

namespace KuaforumAPI.Application.Validators
{
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Mevcut şifre zorunludur.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Yeni şifre zorunludur.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
                .MaximumLength(100).WithMessage("Şifre en fazla 100 karakter olabilir.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Şifre tekrarı zorunludur.")
                .Equal(x => x.NewPassword).WithMessage("Şifreler eşleşmiyor.");
        }
    }
}
