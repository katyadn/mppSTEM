using Asp.Versioning;

using FluentValidation;

using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Rooms;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Управління приміщеннями (кімнатами) системи моніторингу.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rooms")]
public sealed class RoomsController(
    IRoomRepository rooms,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    IValidator<CreateRoomRequest> createValidator,
    IValidator<UpdateRoomRequest> updateValidator) : ControllerBase
{
    /// <summary>
    /// Отримання пагінованого списку кімнат.
    /// </summary>
    [Authorize(Roles = "Viewer,Operator,Admin")]
    [HttpGet]
    [ProducesResponseType<PagedResponse<RoomResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RoomResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var pageRequest = new PageRequest(page, pageSize);
        var result = await rooms.ListAsync(pageRequest, cancellationToken);

        var responses = result.Items.Select(MapToResponse).ToList();
        return Ok(new PagedResponse<RoomResponse>(responses, result.Page, result.PageSize, result.TotalCount));
    }

    /// <summary>
    /// Отримання інформації про конкретну кімнату за її ID.
    /// </summary>
    [Authorize(Roles = "Viewer,Operator,Admin")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetByIdAsync(id, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", id);
        }

        return Ok(MapToResponse(room));
    }

    /// <summary>
    /// Створення нового приміщення (лише адміністратор).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomResponse>> Create(
        [FromBody] CreateRoomRequest request,
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

        if (await rooms.ExistsByNameAsync(request.Name, cancellationToken))
        {
            throw new ConflictException($"Кімната з назвою '{request.Name}' вже існує.");
        }

        var roomType = ParseRoomType(request.Type);
        var room = Room.Create(
            request.Name,
            request.Floor,
            request.AreaSqM.HasValue ? (decimal)request.AreaSqM.Value : null,
            roomType,
            time.GetUtcNow());

        rooms.Add(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = room.Id, version = "1.0" }, MapToResponse(room));
    }

    /// <summary>
    /// Оновлення параметрів кімнати (лише адміністратор).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomResponse>> Update(
        Guid id,
        [FromBody] UpdateRoomRequest request,
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

        var room = await rooms.GetByIdAsync(id, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", id);
        }

        if (!room.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase) &&
            await rooms.ExistsByNameAsync(request.Name, cancellationToken))
        {
            throw new ConflictException($"Кімната з назвою '{request.Name}' вже існує.");
        }

        var roomType = ParseRoomType(request.Type);
        room.Update(
            request.Name,
            request.Floor,
            request.AreaSqM.HasValue ? (decimal)request.AreaSqM.Value : null,
            roomType);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(MapToResponse(room));
    }

    /// <summary>
    /// Видалення кімнати (лише адміністратор). Заборонено, якщо у кімнаті є датчики.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetByIdAsync(id, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", id);
        }

        if (await rooms.HasSensorsAsync(id, cancellationToken))
        {
            throw new ConflictException("Неможливо видалити кімнату, оскільки в ній зареєстровано датчики.");
        }

        rooms.Remove(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static RoomType ParseRoomType(string? type) =>
        Enum.TryParse<RoomType>(type, true, out var result) ? result : RoomType.Office;

    private static RoomResponse MapToResponse(Room room) =>
        new(
            room.Id,
            Guid.Empty,
            room.Name,
            room.Floor,
            room.AreaM2.HasValue ? (double)room.AreaM2.Value : null,
            room.Type.ToString(),
            room.CreatedAt);
}
