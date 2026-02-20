namespace Test.Contracts.Constants;

public static class RabbitMqConstants
{
    // Exchanges
    public const string EventsExchange = "tests.events";
    public const string CommandsExchange = "tests.commands";
    public const string DeadLetterExchange = "tests.dlx";

    // Routing Keys
    public const string TestCreated = "test.created";
    public const string TestStarted = "test.started";
    public const string TestFinished = "test.finished";
    public const string TestFailed = "test.failed";
    public const string TestCancelled = "test.cancelled";
    public const string CancelTest = "cancel.test";

    // Queues
    public const string WorkerQueue = "tests.worker";
    public const string MonitorQueue = "tests.monitor";
    public const string CancelQueue = "tests.cancel";

    // DLQ
    public const string WorkerDlq = "tests.worker.dlq";
    public const string MonitorDlq = "tests.monitor.dlq";

    // Retry
    public const string RetryQueue = "tests.retry";
    public const int RetryDelayMs = 5000;
    public const int MaxRetryCount = 3;
}
