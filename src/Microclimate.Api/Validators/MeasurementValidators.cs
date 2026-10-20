using FluentValidation;

using Microclimate.Application.Measurements;
using Microclimate.Contracts.Measurements;

namespace Microclimate.Api.Validators;

public sealed class MeasurementBatchRequestValidator : AbstractValidator<MeasurementBatchRequest>
{
    public MeasurementBatchRequestValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty().WithMessage("Ідентифікатор сенсора обов'язковий.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Пакет вимірів не може бути порожнім.")
            .Must(items => items.Count <= IMeasurementIngestionService.MaxBatchSize)
            .WithMessage($"Кількість вимірів у пакеті не може перевищувати {IMeasurementIngestionService.MaxBatchSize}.");
    }
}
