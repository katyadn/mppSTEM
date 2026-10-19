using Asp.Versioning;

using Microclimate.Application.Analytics;
using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Common;
using Microclimate.Domain.ValueObjects;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Аналітичні показники приміщень: статистичні зведення, часові ряди та індекс комфорту.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rooms/{roomId:guid}/analytics")]
[Authorize(Roles = "Viewer,Operator,Admin")]
public sealed class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    /// <summary>
    /// Отримання зведених статистичних показників (min, max, avg, count) за період.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType<IReadOnlyList<MetricSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MetricSummaryResponse>>> GetSummary(
        Guid roomId,
        [FromQuery] MetricType? metric,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var start = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var end = to ?? DateTimeOffset.UtcNow;
        var range = DateRange.Create(start, end, AnalyticsLimits.MaxPeriod);

        Domain.Enums.MetricType? domainMetric = metric.HasValue && Enum.TryParse<Domain.Enums.MetricType>(metric.Value.ToString(), out var dm)
            ? dm
            : null;

        var summaries = await analytics.GetSummaryAsync(roomId, domainMetric, range, cancellationToken);

        var responses = summaries.Select(s =>
        {
            var m = Enum.TryParse<MetricType>(s.Metric.ToString(), out var mt) ? mt : MetricType.Temperature;
            return new MetricSummaryResponse(m, s.Count, s.Min, s.Max, s.Avg, null);
        }).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Отримання часового ряду агрегацій (погодинно або подобово) за період.
    /// </summary>
    [HttpGet("series")]
    [ProducesResponseType<IReadOnlyList<SeriesPointResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SeriesPointResponse>>> GetSeries(
        Guid roomId,
        [FromQuery] MetricType metric,
        [FromQuery] Contracts.Common.AggregationInterval interval = Contracts.Common.AggregationInterval.Hour,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var start = from ?? DateTimeOffset.UtcNow.AddDays(-1);
        var end = to ?? DateTimeOffset.UtcNow;

        var maxPeriod = interval == Contracts.Common.AggregationInterval.Hour
            ? AnalyticsLimits.MaxHourlyPeriod
            : AnalyticsLimits.MaxPeriod;

        var range = DateRange.Create(start, end, maxPeriod);

        var domainMetric = Enum.TryParse<Domain.Enums.MetricType>(metric.ToString(), out var dm)
            ? dm
            : Domain.Enums.MetricType.Temperature;

        var appInterval = interval == Contracts.Common.AggregationInterval.Hour
            ? Application.Analytics.AggregationInterval.Hour
            : Application.Analytics.AggregationInterval.Day;

        var points = await analytics.GetSeriesAsync(roomId, domainMetric, range, appInterval, cancellationToken);

        var responses = points.Select(p => new SeriesPointResponse(
            p.BucketStart,
            p.Count,
            p.Avg,
            p.Min,
            p.Max)).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Обчислення комплексного індексу комфорту мікроклімату приміщення (за останні 15 хв).
    /// </summary>
    [HttpGet("comfort")]
    [ProducesResponseType<ComfortIndexResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComfortIndexResponse>> GetComfortIndex(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var result = await analytics.GetComfortIndexAsync(roomId, cancellationToken);

        var category = Enum.TryParse<Contracts.Analytics.ComfortCategory>(result.Category.ToString(), out var cat)
            ? cat
            : Contracts.Analytics.ComfortCategory.NoData;

        var components = result.Components.Select(c =>
        {
            var m = Enum.TryParse<MetricType>(c.Metric.ToString(), out var mt) ? mt : MetricType.Temperature;
            return new ComfortScoreComponentResponse(m, c.Value, c.Score, c.Weight);
        }).ToList();

        return Ok(new ComfortIndexResponse(result.Index, category, components));
    }
}
