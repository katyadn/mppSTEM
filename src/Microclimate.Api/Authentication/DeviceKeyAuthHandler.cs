using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microclimate.Application.Abstractions.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Microclimate.Api.Authentication;

/// <summary>
/// Обробник X-Device-Key автентифікації для датчиків.
/// Схема: "DeviceKey". Заголовок: X-Device-Key: &lt;plaintext-key&gt;.
/// Хеш зберігається в Sensor.DeviceKeyHash (SHA-256, hex).
/// </summary>
internal sealed class DeviceKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    ISensorRepository sensors,
    IUnitOfWork unitOfWork,
    TimeProvider time)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DeviceKey";
    public const string HeaderName = "X-Device-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            return AuthenticateResult.NoResult();
        }

        var rawKey = keyValues.First()!;
        var keyHash = ComputeHash(rawKey);

        var sensor = await sensors.GetByDeviceKeyHashAsync(keyHash, Context.RequestAborted);
        if (sensor is null)
        {
            return AuthenticateResult.Fail("Invalid X-Device-Key.");
        }

        sensor.MarkSeen(time.GetUtcNow());
        await unitOfWork.SaveChangesAsync(Context.RequestAborted);

        var claims = new[]
        {
            new Claim("sensor_id", sensor.Id.ToString()),
            new Claim("room_id", sensor.RoomId.ToString()),
            new Claim(ClaimTypes.Role, "Device"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    private static string ComputeHash(string rawKey)
    {
        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
