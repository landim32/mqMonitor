using Test.Infrastructure;
using Test.Infrastructure.RabbitMq;
using Test.Worker.Handlers;
using Test.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add infrastructure (RabbitMQ)
builder.Services.AddInfrastructure(builder.Configuration);

// Worker services
builder.Services.AddSingleton<CancellationTokenManager>();
builder.Services.AddScoped<TestExecutorService>();

// Background handlers
builder.Services.AddHostedService<TestCreatedHandler>();
builder.Services.AddHostedService<CancelCommandHandler>();

var host = builder.Build();

// Configure RabbitMQ topology
using (var scope = host.Services.CreateScope())
{
    var topology = scope.ServiceProvider.GetRequiredService<RabbitMqTopologySetup>();
    topology.Configure();
}

host.Run();
