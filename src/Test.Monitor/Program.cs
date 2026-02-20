using Microsoft.EntityFrameworkCore;
using Test.Infrastructure;
using Test.Infrastructure.Database;
using Test.Infrastructure.RabbitMq;
using Test.Monitor.Consumers;
using Test.Monitor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add infrastructure (RabbitMQ + PostgreSQL)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Monitor services
builder.Services.AddScoped<EventProjectionService>();
builder.Services.AddScoped<TestQueryService>();
builder.Services.AddHostedService<TestEventConsumer>();

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Test Monitor API",
        Version = "v1",
        Description = "API de Observabilidade para monitoramento de execuções de testes"
    });
});

var app = builder.Build();

// Configure topology
using (var scope = app.Services.CreateScope())
{
    var topology = scope.ServiceProvider.GetRequiredService<RabbitMqTopologySetup>();
    topology.Configure();

    // Auto-migrate database
    var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
    db.Database.Migrate();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
