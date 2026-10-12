using Asp.Versioning;
using Microclimate.Contracts.Service;
using Microsoft.AspNetCore.Mvc;

namespace Microclimate.Api.Controllers.V1;

/// <summary>
/// Ендпоінт діагностики стану сервісу та поточної версії API.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ping")]
public sealed class PingController(TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Повертає статус працездатності, версію API та поточний системний час сервера.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PingResponse>(StatusCodes.Status200OK)]
    public ActionResult<PingResponse> Get() =>
        Ok(new PingResponse("ok", "1.0", timeProvider.GetUtcNow()));
}
