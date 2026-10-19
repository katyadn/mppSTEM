using FluentValidation;

using Microclimate.Contracts.Thresholds;

namespace Microclimate.Api.Validators;

public sealed class UpdateThresholdRequestValidator : AbstractValidator<UpdateThresholdRequest>
{
    public UpdateThresholdRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.CriticalMin.HasValue || x.WarningMin.HasValue || x.WarningMax.HasValue || x.CriticalMax.HasValue)
            .WithMessage("Поріг має містити хоча б одну межу.")
            .Must(x => !(x.CriticalMin.HasValue && x.WarningMin.HasValue && x.CriticalMin.Value > x.WarningMin.Value))
            .WithMessage("Критична нижня межа не може бути вищою за нижню межу попередження.")
            .Must(x => !(x.WarningMax.HasValue && x.CriticalMax.HasValue && x.WarningMax.Value > x.CriticalMax.Value))
            .WithMessage("Верхня межа попередження не може бути вищою за критичну верхню межу.");
    }
}
