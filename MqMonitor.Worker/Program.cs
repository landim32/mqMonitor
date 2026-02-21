using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MqMonitor.Application;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.RabbitMq;
using MqMonitor.Worker.Handlers;
using MqMonitor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register all layers via the Application initializer
builder.Services.AddMqMonitor(builder.Configuration);

// Worker-specific services
builder.Services.AddSingleton<CancellationTokenManager>();
builder.Services.AddScoped<ProcessExecutorService>();

// Background handlers — existing
builder.Services.AddHostedService<ProcessCreatedHandler>();
builder.Services.AddHostedService<CancelCommandHandler>();

// We need to resolve pipeline settings early to register handlers
var pipelineSettings = builder.Configuration
    .GetSection(PipelineSettings.SectionName)
    .Get<PipelineSettings>() ?? new PipelineSettings();

// Dynamic pipeline stage handlers — one per configured stage
foreach (var stage in pipelineSettings.Stages)
{
    var capturedStage = stage; // capture for closure
    builder.Services.AddSingleton<IHostedService>(sp =>
        new PipelineStageHandler(
            sp.GetRequiredService<RabbitMqConnectionFactory>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IMessagePublisher>(),
            capturedStage,
            sp.GetRequiredService<ILogger<PipelineStageHandler>>()));
}

var host = builder.Build();

// Configure RabbitMQ topology
using (var scope = host.Services.CreateScope())
{
    var topology = scope.ServiceProvider.GetRequiredService<RabbitMqTopologySetup>();
    topology.Configure();
}

host.Run();
