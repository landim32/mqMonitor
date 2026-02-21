using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.RabbitMq;
using MqMonitor.Infra.Services;

// Setup
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
services.Configure<PipelineSettings>(configuration.GetSection(PipelineSettings.SectionName));
services.AddLogging(b => b.AddConsole());
services.AddSingleton<RabbitMqConnectionFactory>();
services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
services.AddSingleton<IProcessCreationService, ProcessCreationService>();

var provider = services.BuildServiceProvider();
var creationService = provider.GetRequiredService<IProcessCreationService>();
var pipelineSettings = provider.GetRequiredService<IOptions<PipelineSettings>>().Value;

Console.WriteLine("=== Process Producer (Pipeline) ===");
Console.WriteLine("Commands:");
Console.WriteLine("  send <stage> [count] [priority] - Send processes to a pipeline stage");
Console.WriteLine("  stages                          - List available stages");
Console.WriteLine("  quit                            - Exit");
Console.WriteLine();
Console.WriteLine("Available stages: " + string.Join(", ", creationService.GetAvailableStages()));
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(input))
        continue;

    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var command = parts[0].ToLower();

    switch (command)
    {
        case "send":
        {
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: send <stage> [count] [priority]");
                Console.WriteLine("  Example: send relatorio 3 5");
                break;
            }

            var stageName = parts[1].ToLower();
            var count = parts.Length > 2 && int.TryParse(parts[2], out var c) ? c : 1;
            var priority = parts.Length > 3 && int.TryParse(parts[3], out var p) ? p : 0;

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var result = creationService.CreateProcess(new CreateProcessRequest
                    {
                        StageName = stageName,
                        Priority = priority
                    });

                    Console.WriteLine($"  Sent process {result.ProcessId} -> stage '{result.StageName}' (priority: {result.Priority})");
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
            break;
        }

        case "stages":
            Console.WriteLine("Available stages:");
            foreach (var s in pipelineSettings.Stages)
            {
                Console.WriteLine($"  {s.Name} ({s.DisplayName}) -> queue: {s.QueueName}, routing: {s.RoutingKey}");
            }
            break;

        case "quit":
        case "exit":
            Console.WriteLine("Bye!");
            return;

        default:
            Console.WriteLine($"Unknown command: {command}");
            break;
    }
}
