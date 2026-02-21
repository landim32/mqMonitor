namespace MqMonitor.Infra.Configuration;

public static class RabbitMqConstants
{
    // Exchanges
    public const string EventsExchange = "processes.events";
    public const string CommandsExchange = "processes.commands";
    public const string DeadLetterExchange = "processes.dlx";

    // Routing Keys
    public const string ProcessCreated = "process.created";
    public const string ProcessStarted = "process.started";
    public const string ProcessFinished = "process.finished";
    public const string ProcessFailed = "process.failed";
    public const string ProcessCancelled = "process.cancelled";
    public const string ProcessQueued = "process.queued";
    public const string ProcessStageStarted = "process.stage.started";
    public const string ProcessStageCompleted = "process.stage.completed";
    public const string ProcessCompensating = "process.compensating";
    public const string ProcessCompensated = "process.compensated";
    public const string CancelProcess = "cancel.process";
    public const string ChangePriority = "change.priority";

    // Queues
    public const string WorkerQueue = "processes.worker";
    public const string MonitorQueue = "processes.monitor";
    public const string CancelQueue = "processes.cancel";

    // DLQ
    public const string WorkerDlq = "processes.worker.dlq";
    public const string MonitorDlq = "processes.monitor.dlq";

    // Compensation
    public const string CompensationQueue = "processes.compensation";

    // Retry
    public const string RetryQueue = "processes.retry";
    public const int RetryDelayMs = 5000;
    public const int MaxRetryCount = 3;
}
