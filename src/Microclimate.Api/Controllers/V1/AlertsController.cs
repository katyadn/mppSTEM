using System.Security.Claims;

using Asp.Versioning;

using Microclimate.Application.Abstractions.Notifications;
using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Contracts.Alerts;
using Microclimate.Contracts.Common;
using Microclimate.Domain.Alerts;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Перегляд журналу сповіщень про інциденти та їх квитування оператором.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/alerts")]
public sealed class AlertsController(
    IAlertRepository alerts,
    IAlertNotifier alertNotifier,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ControllerBase
{
    /// <summary>
    /// Отримання фільтрованого списку сповіщень із пагінацією.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Viewer,Operator,Admin")]
    [ProducesResponseType<PagedResponse<AlertResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AlertResponse>>> List(
        [FromQuery] Guid? roomId,
        [FromQuery] AlertLevel? level,
        [FromQuery] AlertStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        Domain.Enums.AlertLevel? domainLevel = level.HasValue && Enum.TryParse<Domain.Enums.AlertLevel>(level.Value.ToString(), out var dl)
            ? dl
            : null;

        Domain.Enums.AlertStatus? domainStatus = status.HasValue && Enum.TryParse<Domain.Enums.AlertStatus>(status.Value.ToString(), out var ds)
            ? ds
            : null;

        var filter = new AlertFilter(roomId, domainLevel, domainStatus);
        var pageRequest = new PageRequest(page, pageSize);

        var paged = await alerts.ListAsync(filter, pageRequest, cancellationToken);
        var responses = paged.Items.Select(MapToResponse).ToList();

        return Ok(new PagedResponse<AlertResponse>(responses, paged.Page, paged.PageSize, paged.TotalCount));
    }

    /// <summary>
    /// Квитування (підтвердження перегляду) відкритого сповіщення оператором або адміністратором.
    /// </summary>
    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Roles = "Operator,Admin")]
    [ProducesResponseType<AlertResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> Acknowledge(
        Guid id,
        CancellationToken cancellationToken)
    {
        var alert = await alerts.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            throw NotFoundException.For("Сповіщення", id);
        }

        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var userId))
        {
            userId = Guid.NewGuid();
        }

        alert.Acknowledge(userId, time.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await alertNotifier.AlertAcknowledgedAsync(alert, cancellationToken);

        return Ok(MapToResponse(alert));
    }

    private static AlertResponse MapToResponse(Alert alert)
    {
        var metric = Enum.TryParse<MetricType>(alert.Metric.ToString(), out var m) ? m : MetricType.Temperature;
        var level = Enum.TryParse<AlertLevel>(alert.Level.ToString(), out var l) ? l : AlertLevel.Warning;
        var status = Enum.TryParse<AlertStatus>(alert.Status.ToString(), out var s) ? s : AlertStatus.Open;

        return new AlertResponse(
            alert.Id,
            alert.RoomId,
            alert.SensorId,
            metric,
            level,
            status,
            alert.Value,
            alert.ThresholdValue,
            alert.Message,
            alert.RaisedAt,
            alert.AcknowledgedAt,
            alert.AcknowledgedByUserId,
            alert.ResolvedAt);
    }
}
