using System.Text;

using Asp.Versioning;

using FluentValidation;

using Microclimate.Api.Authentication;
using Microclimate.Api.Middleware;
using Microclimate.Api.Services;
using Microclimate.Contracts.Auth;

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

builder.Services.ConfigureAll<Microsoft.AspNetCore.OpenApi.OpenApiOptions>(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Components ??= new();
        var scheme = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token (without 'Bearer ' prefix)",
        };

        doc.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        doc.Components.SecuritySchemes["Bearer"] = scheme;

        var req = new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer")] = [],
        };

        doc.Security ??= [];
        doc.Security.Add(req);

        return Task.CompletedTask;
    });
});

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
builder.Services.AddSingleton<Microclimate.Application.Analytics.IComfortIndexCalculator, Microclimate.Api.Services.ComfortIndexCalculator>();
builder.Services.AddScoped<Microclimate.Application.Analytics.IAnalyticsService, Microclimate.Api.Services.AnalyticsService>();

// ── Persistence & Repositories (Fallback In-Memory) ───────────────────────────
builder.Services.AddSingleton<Microclimate.Api.Storage.InMemoryDataStore>();
builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IUserRepository, Microclimate.Api.Storage.InMemoryUserRepository>();
builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.ISensorRepository, Microclimate.Api.Storage.InMemorySensorRepository>();
builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IRoomRepository, Microclimate.Api.Storage.InMemoryRoomRepository>();
builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IThresholdRepository, Microclimate.Api.Storage.InMemoryThresholdRepository>();
builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IAlertRepository, Microclimate.Api.Storage.InMemoryAlertRepository>();
builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IMeasurementRepository, Microclimate.Api.Storage.InMemoryMeasurementRepository>();
builder.Services.AddScoped<Microclimate.Application.Abstractions.Persistence.IUnitOfWork, Microclimate.Api.Storage.InMemoryUnitOfWork>();

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

app.Run();

public partial class Program;
