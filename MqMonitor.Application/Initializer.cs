using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Context;
using MqMonitor.Infra.Interfaces.Repository;
using MqMonitor.Infra.Mapping.Profiles;
using MqMonitor.Infra.RabbitMq;
using MqMonitor.Infra.Repository;
using MqMonitor.Infra.Services;

namespace MqMonitor.Application;

public static class Initializer
{
    public static IServiceCollection AddMqMonitor(
        this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL
        services.AddDbContext<MonitorDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("MonitorDb"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(MonitorDbContext).Assembly.FullName)));

        // RabbitMQ configuration
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

        // Pipeline configuration
        services.Configure<PipelineSettings>(
            configuration.GetSection(PipelineSettings.SectionName));

        // RabbitMQ infrastructure
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<RabbitMqTopologySetup>();

        // Message publisher (interface-based)
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        // Repositories
        services.AddScoped<IProcessExecutionRepository<IProcessExecutionModel>, ProcessExecutionRepository>();
        services.AddScoped<IEventLogRepository<IEventLogModel>, EventLogRepository>();
        services.AddScoped<ISagaStepRepository<ISagaStepModel>, SagaStepRepository>();

        // AutoMapper profiles
        services.AddAutoMapper(typeof(ProcessExecutionProfile).Assembly);

        // Domain services
        services.AddScoped<IProcessQueryService, ProcessQueryService>();
        services.AddScoped<IProcessCreationService, ProcessCreationService>();
        services.AddScoped<IEventProjectionService, EventProjectionService>();

        // RabbitMQ Management API client
        services.AddHttpClient<IRabbitMqManagementService, RabbitMqManagementService>();

        return services;
    }
}
