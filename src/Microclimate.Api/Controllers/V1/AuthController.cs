using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Asp.Versioning;

using FluentValidation;

using Microclimate.Api.Services;
using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Contracts.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Автентифікація та управління сесіями користувачів.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    IUserRepository users,
    IJwtTokenService jwtTokenService,
    IValidator<LoginRequest> loginValidator) : ControllerBase
{
    /// <summary>
    /// Автентифікація за email і паролем. Повертає access + refresh токени.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in validation.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(modelState);
        }

        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Невірний email або пароль.",
                Status = StatusCodes.Status401Unauthorized,
            });
        }

        var (accessToken, refreshToken, expiresIn) = jwtTokenService.GenerateTokens(user);
        return Ok(new TokenResponse(accessToken, refreshToken, expiresIn));
    }

    /// <summary>
    /// Оновлення access-токена за допомогою refresh-токена.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var userId = jwtTokenService.ValidateRefreshToken(request.RefreshToken);
        if (userId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Refresh-токен недійсний або прострочений.",
                Status = StatusCodes.Status401Unauthorized,
            });
        }

        var user = await users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Користувача не знайдено.",
                Status = StatusCodes.Status401Unauthorized,
            });
        }

        var (accessToken, refreshToken, expiresIn) = jwtTokenService.GenerateTokens(user);
        return Ok(new TokenResponse(accessToken, refreshToken, expiresIn));
    }

    /// <summary>
    /// Профіль поточного автентифікованого користувача.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;

        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Unauthorized();
        }

        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var role = Enum.TryParse<Contracts.Common.UserRole>(user.Role.ToString(), out var r)
            ? r
            : Contracts.Common.UserRole.Viewer;

        return Ok(new UserResponse(user.Id, user.Email, role, user.CreatedAt));
    }

    private static bool VerifyPassword(string plainPassword, string storedHash)
    {
        var bytes = Encoding.UTF8.GetBytes(plainPassword);
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        return hash == storedHash;
    }
}
