using System.Security.Cryptography;
using System.Text;

using Asp.Versioning;

using FluentValidation;

using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Users;
using Microclimate.Domain.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Управління користувачами системи та їхніми ролями (лише адміністратор).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    IValidator<CreateUserRequest> createValidator) : ControllerBase
{
    /// <summary>
    /// Отримання списку всіх зареєстрованих користувачів.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken)
    {
        var allUsers = await users.ListAsync(cancellationToken);
        var responses = allUsers.Select(MapToResponse).ToList();
        return Ok(responses);
    }

    /// <summary>
    /// Створення нового користувача системи.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
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

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"Користувач з email '{request.Email}' вже існує.");
        }

        var domainRole = Enum.TryParse<Domain.Enums.UserRole>(request.Role.ToString(), out var dr)
            ? dr
            : Domain.Enums.UserRole.Viewer;

        var passwordHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(request.Password)));
        var displayName = request.Email.Split('@')[0];

        var user = Microclimate.Domain.Users.User.Create(normalizedEmail, displayName, passwordHash, domainRole, time.GetUtcNow());
        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = MapToResponse(user);
        return Created($"/api/v1/users/{user.Id}", response);
    }

    /// <summary>
    /// Зміна ролі користувача.
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> ChangeRole(
        Guid id,
        [FromBody] ChangeRoleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw NotFoundException.For("Користувач", id);
        }

        var domainRole = Enum.TryParse<Domain.Enums.UserRole>(request.Role.ToString(), out var dr)
            ? dr
            : Domain.Enums.UserRole.Viewer;

        user.ChangeRole(domainRole);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(user));
    }

    /// <summary>
    /// Видалення (деактивація) користувача.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw NotFoundException.For("Користувач", id);
        }

        user.Deactivate();
        users.Remove(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static UserResponse MapToResponse(User user)
    {
        var role = Enum.TryParse<Contracts.Common.UserRole>(user.Role.ToString(), out var r)
            ? r
            : Contracts.Common.UserRole.Viewer;

        return new UserResponse(user.Id, user.Email, role, user.CreatedAt);
    }
}
