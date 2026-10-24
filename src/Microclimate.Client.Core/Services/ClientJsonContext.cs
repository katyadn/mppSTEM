using System.Text.Json.Serialization;
using Microclimate.Contracts.Alerts;
using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

namespace Microclimate.Client.Core.Services;

// Поля помилок валідації з серверного ProblemDetails.
public sealed record ClientProblem(Dictionary<string, string[]>? Errors);

// Метадані JSON, згенеровані під час компіляції для клієнтських DTO, із camelCase та рядковими enum.
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(StoredSession))]
[JsonSerializable(typeof(PagedResponse<RoomResponse>))]
[JsonSerializable(typeof(PagedResponse<AlertResponse>))]
[JsonSerializable(typeof(RoomResponse))]
[JsonSerializable(typeof(List<MetricSummaryResponse>))]
[JsonSerializable(typeof(List<SeriesPointResponse>))]
[JsonSerializable(typeof(ComfortIndexResponse))]
[JsonSerializable(typeof(ClientProblem))]
public partial class ClientJsonContext : JsonSerializerContext;
