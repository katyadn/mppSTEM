using System.Diagnostics;
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

        logger.LogError(
            exception,
            "Неперехоплений виняток під час обробки запиту {Path}. TraceId: {TraceId}",
            httpContext.Request.Path,
            traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var detail = environment.IsDevelopment()
            ? exception.Message
            : "Сталася внутрішня помилка сервера. Будь ласка, зверніться до адміністратора.";

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Type = "https://api.microclimate.local/errors/internal_error",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = detail,
                Instance = httpContext.Request.Path,
                Extensions =
                {
                    ["traceId"] = traceId,
                },
            },
        });
    }
}
