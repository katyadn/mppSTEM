using Asp.Versioning;

using FluentValidation;

using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Thresholds;
using Microclimate.Domain.Metrics;
using Microclimate.Domain.Thresholds;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Управління граничними порогами мікроклімату для приміщень.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rooms/{roomId:guid}/thresholds")]
public sealed class ThresholdsController(
    IThresholdRepository thresholds,
    IRoomRepository rooms,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    IValidator<UpdateThresholdRequest> updateValidator) : ControllerBase
{
    private static readonly MetricType[] AllMetrics =
    [
        MetricType.Temperature,
        MetricType.Humidity,
        MetricType.Co2,
        MetricType.Pm25,
        MetricType.Noise,
        MetricType.Light,
    ];

    /// <summary>
    /// Отримання всіх діючих порогів для кімнати (власні або за замовчуванням).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Viewer,Operator,Admin")]
    [ProducesResponseType<IReadOnlyList<ThresholdResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ThresholdResponse>>> GetByRoom(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        var customList = await thresholds.ListByRoomAsync(roomId, cancellationToken);
        var customMap = customList.ToDictionary(t => t.Metric);

        var responses = new List<ThresholdResponse>();
        foreach (var metric in AllMetrics)
        {
            var domainMetric = Enum.TryParse<Domain.Enums.MetricType>(metric.ToString(), out var dm)
                ? dm
                : Domain.Enums.MetricType.Temperature;

            if (customMap.TryGetValue(domainMetric, out var custom))
            {
                responses.Add(MapToResponse(custom));
            }
            else
            {
                var defaultLimits = DefaultThresholds.For(room.Type, domainMetric);
                responses.Add(new ThresholdResponse(
                    Guid.Empty,
                    roomId,
                    metric,
                    defaultLimits.WarningMin,
                    defaultLimits.WarningMax,
                    defaultLimits.CriticalMin,
                    defaultLimits.CriticalMax));
            }
        }

        return Ok(responses);
    }

    /// <summary>
    /// Встановлення або зміна користувацьких порогів для конкретної метрики кімнати.
    /// </summary>
    [HttpPut("{metric}")]
    [Authorize(Roles = "Operator,Admin")]
    [ProducesResponseType<ThresholdResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ThresholdResponse>> Update(
        Guid roomId,
        MetricType metric,
        [FromBody] UpdateThresholdRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in validation.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(modelState);
        }

        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        var domainMetric = Enum.TryParse<Domain.Enums.MetricType>(metric.ToString(), out var dm)
            ? dm
            : Domain.Enums.MetricType.Temperature;

        var limits = new ThresholdLimits(request.CriticalMin, request.WarningMin, request.WarningMax, request.CriticalMax);

        var existing = await thresholds.GetAsync(roomId, domainMetric, cancellationToken);
        if (existing is null)
        {
            existing = Threshold.Create(roomId, domainMetric, limits, time.GetUtcNow());
            thresholds.Add(existing);
        }
        else
        {
            existing.Update(limits);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(MapToResponse(existing));
    }

    private static ThresholdResponse MapToResponse(Threshold threshold)
    {
        var metricType = Enum.TryParse<MetricType>(threshold.Metric.ToString(), out var mt)
            ? mt
            : MetricType.Temperature;

        return new ThresholdResponse(
            threshold.Id,
            threshold.RoomId,
            metricType,
            threshold.WarningMin,
            threshold.WarningMax,
            threshold.CriticalMin,
            threshold.CriticalMax);
    }
}
