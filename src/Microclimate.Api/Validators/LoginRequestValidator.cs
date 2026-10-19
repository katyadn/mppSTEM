using FluentValidation;

using Microclimate.Contracts.Auth;

namespace Microclimate.Api.Validators;

/// <summary>Валідація запиту на логін.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обов'язковий.")
            .EmailAddress().WithMessage("Некоректний формат email.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обов'язковий.")
            .MinimumLength(6).WithMessage("Пароль має містити мінімум 6 символів.")
            .MaximumLength(128);
    }
}
