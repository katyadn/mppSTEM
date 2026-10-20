using System.Text;

using Asp.Versioning;

using FluentValidation;

using Microclimate.Api.Authentication;
using Microclimate.Api.Middleware;
using Microclimate.Api.Services;
using Microclimate.Application;
using Microclimate.Contracts.Auth;
using Microclimate.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ── Versioning ────────────────────────────────────────────────────────────────
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
var issuer = jwtSection["Issuer"] ?? "microclimate-api";
var audience = jwtSection["Audience"] ?? "microclimate-client";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero,
        };
    })
    .AddScheme<AuthenticationSchemeOptions, DeviceKeyAuthHandler>(DeviceKeyAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<Microclimate.Application.Abstractions.Notifications.IAlertNotifier, Microclimate.Api.Services.SignalRAlertNotifier>();
builder.Services.AddScoped<Microclimate.Application.Measurements.IMeasurementIngestionService, Microclimate.Api.Services.MeasurementIngestionService>();

// Аналітика й індекс комфорту — шар Application.
builder.Services.AddApplication();

// ── Persistence ───────────────────────────────────────────────────────────────
// Якщо задано Database:Provider (профілі запуску, Docker), дані зберігаються в БД через Infrastructure.
// Без нього (інтеграційні тести) лишається сховище в пам'яті.
var useDatabase = !string.IsNullOrWhiteSpace(builder.Configuration[DatabaseSettings.ProviderKey]);
if (useDatabase)
{
    builder.Services.AddInfrastructure(builder.Configuration);
}
else
{
    builder.Services.AddSingleton<Microclimate.Api.Storage.InMemoryDataStore>();
    builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IUserRepository, Microclimate.Api.Storage.InMemoryUserRepository>();
    builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.ISensorRepository, Microclimate.Api.Storage.InMemorySensorRepository>();
    builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IRoomRepository, Microclimate.Api.Storage.InMemoryRoomRepository>();
    builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IThresholdRepository, Microclimate.Api.Storage.InMemoryThresholdRepository>();
    builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IAlertRepository, Microclimate.Api.Storage.InMemoryAlertRepository>();
    builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IMeasurementRepository, Microclimate.Api.Storage.InMemoryMeasurementRepository>();
    builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IUnitOfWork, Microclimate.Api.Storage.InMemoryUnitOfWork>();
}

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

builder.Services.AddHealthChecks();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Default", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Microclimate API v1"));
}

app.UseHttpsRedirection();
app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<Microclimate.Api.Hubs.AlertsHub>("/hubs/alerts");

if (useDatabase)
{
    await Microclimate.Api.Storage.DatabaseSeeder.InitializeAsync(app.Services, seedDemoData: app.Environment.IsDevelopment());
}

await app.RunAsync();

public partial class Program;
