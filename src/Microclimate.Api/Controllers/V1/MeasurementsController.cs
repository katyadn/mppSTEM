using Asp.Versioning;

using FluentValidation;

using Microclimate.Api.Authentication;
using Microclimate.Application.Abstractions.Files;
using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Application.Measurements;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Measurements;
using Microclimate.Contracts.Rooms;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.ValueObjects;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Прийом вимірів мікроклімату від сенсорів, отримання історії та експорт даних.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
public sealed class MeasurementsController(
    IMeasurementIngestionService ingestionService,
    IMeasurementRepository measurements,
    IRoomRepository rooms,
    IMeasurementExporter exporter,
    IValidator<MeasurementBatchRequest> batchValidator) : ControllerBase
{
    /// <summary>
    /// Прийом пакета вимірів від сенсора (до 500 записів).
    /// Доступно як датчикам за X-Device-Key, так і операторам/адміністраторам за JWT.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/measurements")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{DeviceKeyAuthHandler.SchemeName}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ingest(
        [FromBody] MeasurementBatchRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await batchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in validation.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(modelState);
        }

        // Якщо автентифіковано через DeviceKey, перевіряємо, що ID датчика у клеймі співпадає з пакетом
        var sensorClaim = User.FindFirst("sensor_id")?.Value;
        if (sensorClaim is not null && Guid.TryParse(sensorClaim, out var authSensorId) && authSensorId != request.SensorId)
        {
            return Forbid();
        }

        var inputs = request.Items.Select(item =>
        {
            var domainMetric = Enum.TryParse<Domain.Enums.MetricType>(item.Metric.ToString(), out var m)
                ? m
                : Domain.Enums.MetricType.Temperature;
            return new MeasurementInput(domainMetric, item.Value, item.MeasuredAt);
        }).ToList();

        var result = await ingestionService.IngestAsync(request.SensorId, inputs, cancellationToken);

        return Accepted(new
        {
            result.Accepted,
            result.AlertsRaised,
            result.AlertsResolved,
        });
    }

    /// <summary>
    /// Отримання пагінованої історії вимірів кімнати за період.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:guid}/measurements")]
    [Authorize(Roles = "Viewer,Operator,Admin")]
    [ProducesResponseType<PagedResponse<MeasurementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<MeasurementResponse>>> GetByRoom(
        Guid roomId,
        [FromQuery] MetricType metric,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        var domainMetric = Enum.TryParse<Domain.Enums.MetricType>(metric.ToString(), out var m)
            ? m
            : Domain.Enums.MetricType.Temperature;

        var start = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var end = to ?? DateTimeOffset.UtcNow;
        var range = DateRange.Create(start, end);
        var pageRequest = new PageRequest(page, pageSize);

        var paged = await measurements.GetByRoomAsync(roomId, domainMetric, range, pageRequest, cancellationToken);

        var responses = paged.Items.Select(item => new MeasurementResponse(
            Guid.NewGuid(),
            roomId,
            item.SensorId,
            metric,
            item.Value,
            item.MeasuredAt,
            item.MeasuredAt)).ToList();

        return Ok(new PagedResponse<MeasurementResponse>(responses, paged.Page, paged.PageSize, paged.TotalCount));
    }

    /// <summary>
    /// Отримання останніх зафіксованих показників мікроклімату для кімнати.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:guid}/measurements/latest")]
    [Authorize(Roles = "Viewer,Operator,Admin")]
    [ProducesResponseType<IReadOnlyList<LatestMetricResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LatestMetricResponse>>> GetLatest(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        var latestList = await measurements.GetLatestByRoomAsync(roomId, cancellationToken);

        var responses = latestList.Select(item =>
        {
            var metricType = Enum.TryParse<MetricType>(item.Metric.ToString(), out var mt) ? mt : MetricType.Temperature;
            return new LatestMetricResponse(metricType, item.Value, item.MeasuredAt);
        }).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Експорт історії вимірів приміщення у CSV або JSON. Файл формується потоково під час читання з БД.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:guid}/measurements/export")]
    [Authorize(Roles = "Operator,Admin")]
    [Produces("text/csv", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Export(
        Guid roomId,
        [FromQuery] MetricType? metric,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] ExportFormat format = ExportFormat.Csv,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(format))
        {
            throw new Application.Common.ValidationException(nameof(format), "Допустимі формати: csv, json.");
        }

        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        Domain.Enums.MetricType? domainMetric = metric.HasValue && Enum.TryParse<Domain.Enums.MetricType>(metric.Value.ToString(), out var dm)
            ? dm
            : null;

        var start = from ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var end = to ?? DateTimeOffset.UtcNow;
        var range = DateRange.Create(start, end);

        Response.ContentType = ExportFormats.ContentType(format);
        Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = $"room-{roomId}-measurements.{ExportFormats.FileExtension(format)}",
        }.ToString();

        await exporter.ExportAsync(measurements.StreamByRoomAsync(roomId, domainMetric, range, cancellationToken), Response.Body, format, cancellationToken);
        return new EmptyResult();
    }
}
