using FluentValidation;

using Microclimate.Contracts.Users;
using Microclimate.Domain.Users;

namespace Microclimate.Api.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обов'язковий.")
            .EmailAddress().WithMessage("Некоректний формат email.")
            .MaximumLength(User.EmailMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обов'язковий.")
            .MinimumLength(8).WithMessage("Пароль має містити щонайменше 8 символів.")
            .MaximumLength(128);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Вказано невідому роль.");
    }
}
