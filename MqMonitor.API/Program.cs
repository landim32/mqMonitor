using Microsoft.EntityFrameworkCore;
using MqMonitor.Application;
using MqMonitor.Infra.Context;
using MqMonitor.Infra.RabbitMq;
using MqMonitor.API.Consumers;
using MqMonitor.API.Hubs;
using MqMonitor.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Register all layers via the Application initializer
builder.Services.AddMqMonitor(builder.Configuration);

// Register API-specific hosted services
builder.Services.AddHostedService<ProcessEventConsumer>();
builder.Services.AddHostedService<QueueStatsBackgroundService>();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS for frontend dev server and Docker
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Process Monitor API",
        Version = "v1",
        Description = "API de Observabilidade para monitoramento de execucoes de processos"
    });
});

var app = builder.Build();

// Configure topology and auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var topology = scope.ServiceProvider.GetRequiredService<RabbitMqTopologySetup>();
    topology.Configure();

    var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
    db.Database.Migrate();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapHub<MonitorHub>("/hubs/monitor");
app.MapControllers();

app.Run();
