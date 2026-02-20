using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Test.Contracts.Configuration;
using Test.Contracts.Constants;
using Test.Contracts.Events;
using Test.Contracts.Models;
using Test.Infrastructure.RabbitMq;

// Setup
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
services.AddLogging(b => b.AddConsole());
services.AddSingleton<RabbitMqConnectionFactory>();
services.AddSingleton<RabbitMqPublisher>();

var provider = services.BuildServiceProvider();
var publisher = provider.GetRequiredService<RabbitMqPublisher>();

Console.WriteLine("=== Test Producer ===");
Console.WriteLine("Commands:");
Console.WriteLine("  send [count]  - Send test.created events (default: 1)");
Console.WriteLine("  quit          - Exit");
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
            var count = parts.Length > 1 && int.TryParse(parts[1], out var c) ? c : 1;
            for (int i = 0; i < count; i++)
            {
                var testId = $"test-{Guid.NewGuid().ToString()[..8]}";
                var testEvent = new TestEvent
                {
                    TestId = testId,
                    Status = TestStatus.Created,
                    Timestamp = DateTime.UtcNow
                };

                publisher.PublishEvent(testEvent, RabbitMqConstants.TestCreated);
                Console.WriteLine($"  Sent test.created for {testId}");
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
