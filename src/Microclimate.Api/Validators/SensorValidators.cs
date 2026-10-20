using FluentValidation;

using Microclimate.Contracts.Sensors;
using Microclimate.Domain.Sensors;

namespace Microclimate.Api.Validators;

public sealed class CreateSensorRequestValidator : AbstractValidator<CreateSensorRequest>
{
    public CreateSensorRequestValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Кімната встановлення обов'язкова.");

        RuleFor(x => x.SerialNumber)
            .NotEmpty().WithMessage("Серійний номер обов'язковий.")
            .MaximumLength(Sensor.SerialNumberMaxLength).WithMessage($"Серійний номер не може перевищувати {Sensor.SerialNumberMaxLength} символів.");

        RuleFor(x => x.Model)
            .MaximumLength(Sensor.ModelMaxLength).When(x => !string.IsNullOrEmpty(x.Model))
            .WithMessage($"Модель не може перевищувати {Sensor.ModelMaxLength} символів.");
    }
}

public sealed class UpdateSensorRequestValidator : AbstractValidator<UpdateSensorRequest>
{
    public UpdateSensorRequestValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Кімната встановлення обов'язкова.");

        RuleFor(x => x.Model)
            .MaximumLength(Sensor.ModelMaxLength).When(x => !string.IsNullOrEmpty(x.Model))
            .WithMessage($"Модель не може перевищувати {Sensor.ModelMaxLength} символів.");
    }
}
