using System.Net;
using System.Net.Http.Json;

using Microclimate.Contracts.Measurements;

namespace Microclimate.SensorSimulator;

/// <summary>Результат відправлення пакета (docs/simulator.md, розділ 6).</summary>
public enum SendResult
{
    Accepted = 1,
    Rejected = 2,
    Unauthorized = 3,
    Unavailable = 4,
}

/// <summary>Відправлення пакета вимірів в API.</summary>
public interface IMeasurementSender
{
    Task<SendResult> SendAsync(MeasurementBatchRequest batch, string deviceKey, CancellationToken cancellationToken);
}

/// <summary>
/// HTTP-клієнт API вимірів. HttpClient береться з IHttpClientFactory на кожен запит,
/// тому клас можна тримати як Singleton без застарілих з'єднань.
/// </summary>
public sealed partial class MeasurementApiClient(IHttpClientFactory httpClientFactory, ILogger<MeasurementApiClient> logger) : IMeasurementSender
{
    public const string HttpClientName = "MeasurementsApi";
    public const string DeviceKeyHeader = "X-Device-Key";
    public const string MeasurementsPath = "api/v1/measurements";

    public async Task<SendResult> SendAsync(MeasurementBatchRequest batch, string deviceKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(batch);
        using var request = new HttpRequestMessage(HttpMethod.Post, MeasurementsPath) { Content = JsonContent.Create(batch) };
        request.Headers.Add(DeviceKeyHeader, deviceKey);

        try
        {
            using var response = await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return SendResult.Accepted;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return SendResult.Unauthorized;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            LogRejected(batch.SensorId, (int)response.StatusCode, body);
            return SendResult.Rejected;
        }
        catch (HttpRequestException ex)
        {
            LogUnavailable(batch.SensorId, ex.Message);
            return SendResult.Unavailable;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Тайм-аут HttpClient, а не зупинка симулятора.
            LogUnavailable(batch.SensorId, ex.Message);
            return SendResult.Unavailable;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Пакет датчика {SensorId} відхилено: {StatusCode} {Body}")]
    private partial void LogRejected(Guid sensorId, int statusCode, string body);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API недоступне для датчика {SensorId}: {Reason}. Наступна спроба на наступному кроці.")]
    private partial void LogUnavailable(Guid sensorId, string reason);
}
