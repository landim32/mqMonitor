using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Test.Contracts.Configuration;
using Test.Infrastructure.Database;
using Test.Infrastructure.RabbitMq;

namespace Test.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // RabbitMQ
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<RabbitMqTopologySetup>();
        services.AddSingleton<RabbitMqPublisher>();

        // PostgreSQL
        services.AddDbContext<MonitorDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("MonitorDb"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(MonitorDbContext).Assembly.FullName)));

        return services;
    }
}
