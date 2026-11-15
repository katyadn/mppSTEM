using Microclimate.SensorSimulator;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder
    .Services.AddOptions<SimulatorOptions>()
    .Bind(builder.Configuration.GetSection(SimulatorOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<SimulatorOptions>, SimulatorOptionsValidator>();

builder.Services.AddMeasurementApiClient();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IMeasurementSender, MeasurementApiClient>();
builder.Services.AddHostedService<SimulatorWorker>();

await builder.Build().RunAsync();
