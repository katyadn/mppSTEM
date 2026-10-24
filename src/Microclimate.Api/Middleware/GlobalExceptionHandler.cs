using System.Diagnostics;

using Microclimate.Application.Common;
using Microclimate.Domain.Common;

using Microsoft.AspNetCore.Diagnostics;

namespace Microclimate.Api.Middleware;

/// <summary>
/// Глобальний обробник неперехоплених винятків.
/// Повертає RFC 9457 ProblemDetails із traceId, маскуючи деталі помилки у Production.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, type, title, detail) = exception switch
        {
            NotFoundException nfe => (
                StatusCodes.Status404NotFound,
                "https://api.microclimate.local/errors/not_found",
                "Resource Not Found",
                nfe.Message),
            ConflictException ce => (
                StatusCodes.Status409Conflict,
                "https://api.microclimate.local/errors/conflict",
                "Conflict",
                ce.Message),
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "https://api.microclimate.local/errors/validation_error",
                "Validation Failed",
                ve.Message),
            DomainException de => (
                StatusCodes.Status400BadRequest,
                "https://api.microclimate.local/errors/domain_rule_violation",
                "Domain Rule Violation",
                de.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "https://api.microclimate.local/errors/internal_error",
                "Internal Server Error",
                environment.IsDevelopment()
                    ? exception.Message
                    : "Сталася внутрішня помилка сервера. Будь ласка, зверніться до адміністратора."),
        };

        if (statusCode >= 500)
        {
            logger.LogError(
                exception,
                "Неперехоплений виняток під час обробки запиту {Path}. TraceId: {TraceId}",
                httpContext.Request.Path,
                traceId);
        }
        else
        {
            logger.LogWarning(
                "Бізнес-помилка під час обробки запиту {Path}: {Message}. TraceId: {TraceId}",
                httpContext.Request.Path,
                exception.Message,
                traceId);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId,
            },
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
        });
    }
}
