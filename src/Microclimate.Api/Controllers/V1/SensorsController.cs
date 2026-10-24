using System.Security.Cryptography;
using System.Text;

using Asp.Versioning;

using FluentValidation;

using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Sensors;
using Microclimate.Domain.Sensors;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Управління апаратними датчиками (сенсорами) мікроклімату та ротацією ключів X-Device-Key.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
public sealed class SensorsController(
    ISensorRepository sensors,
    IRoomRepository rooms,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    IValidator<CreateSensorRequest> createValidator) : ControllerBase
{
    private static readonly MetricType[] DefaultCapabilities =
    [
        MetricType.Temperature,
        MetricType.Humidity,
        MetricType.Co2,
        MetricType.Pm25,
    ];

    /// <summary>
    /// Отримання списку всіх датчиків у приміщенні.
    /// </summary>
    [Authorize(Roles = "Viewer,Operator,Admin")]
    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:guid}/sensors")]
    [ProducesResponseType<IReadOnlyList<SensorResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SensorResponse>>> ListByRoom(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        var list = await sensors.ListByRoomAsync(roomId, cancellationToken);
        var responses = list.Select(s => MapToResponse(s, null)).ToList();
        return Ok(responses);
    }

    /// <summary>
    /// Отримання інформації про конкретний датчик за його ID.
    /// </summary>
    [Authorize(Roles = "Viewer,Operator,Admin")]
    [HttpGet("api/v{version:apiVersion}/sensors/{id:guid}")]
    [ProducesResponseType<SensorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SensorResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sensor = await sensors.GetByIdAsync(id, cancellationToken);
        if (sensor is null)
        {
            throw NotFoundException.For("Датчик", id);
        }

        return Ok(MapToResponse(sensor));
    }

    /// <summary>
    /// Реєстрація нового датчика адміністратором. Повертає згенерований X-Device-Key.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("api/v{version:apiVersion}/sensors")]
    [ProducesResponseType<SensorCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SensorCreatedResponse>> Create(
        [FromBody] CreateSensorRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in validation.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(modelState);
        }

        var room = await rooms.GetByIdAsync(request.RoomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", request.RoomId);
        }

        if (await sensors.ExistsBySerialNumberAsync(request.SerialNumber, cancellationToken))
        {
            throw new ConflictException($"Датчик із серійним номером '{request.SerialNumber}' вже зареєстровано.");
        }

        var deviceKey = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var keyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(deviceKey)));

        var sensor = Sensor.Create(
            request.RoomId,
            request.SerialNumber,
            request.Model,
            keyHash,
            time.GetUtcNow());

        sensors.Add(sensor);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var sensorResponse = MapToResponse(sensor, request.Capabilities);
        var createdResponse = new SensorCreatedResponse(sensorResponse, deviceKey);

        return CreatedAtAction(nameof(GetById), new { id = sensor.Id, version = "1.0" }, createdResponse);
    }

    /// <summary>
    /// Деактивація датчика адміністратором.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("api/v{version:apiVersion}/sensors/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sensor = await sensors.GetByIdAsync(id, cancellationToken);
        if (sensor is null)
        {
            throw NotFoundException.For("Датчик", id);
        }

        sensor.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Ротація секретного ключа пристрою X-Device-Key (лише адміністратор).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("api/v{version:apiVersion}/sensors/{id:guid}/rotate-key")]
    [ProducesResponseType<SensorCreatedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SensorCreatedResponse>> RotateKey(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sensor = await sensors.GetByIdAsync(id, cancellationToken);
        if (sensor is null)
        {
            throw NotFoundException.For("Датчик", id);
        }

        var newDeviceKey = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var newKeyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(newDeviceKey)));

        sensor.RotateDeviceKey(newKeyHash);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var sensorResponse = MapToResponse(sensor);
        return Ok(new SensorCreatedResponse(sensorResponse, newDeviceKey));
    }

    private static SensorResponse MapToResponse(Sensor sensor, IReadOnlyList<MetricType>? capabilities = null) =>
        new(
            sensor.Id,
            sensor.RoomId,
            sensor.SerialNumber,
            sensor.Model,
            sensor.IsActive,
            capabilities ?? DefaultCapabilities,
            sensor.InstalledAt,
            sensor.LastSeenAt);
}
