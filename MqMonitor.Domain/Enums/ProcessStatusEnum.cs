namespace MqMonitor.Domain.Enums;

public enum ProcessStatusEnum
{
    Created,
    Started,
    Finished,
    Failed,
    Cancelled,
    CancelRequested,
    Queued,
    StageStarted,
    StageCompleted,
    Compensating,
    Compensated
}

public static class ProcessStatusExtensions
{
    public static string ToConstant(this ProcessStatusEnum status) => status switch
    {
        ProcessStatusEnum.Created => "CREATED",
        ProcessStatusEnum.Started => "STARTED",
        ProcessStatusEnum.Finished => "FINISHED",
        ProcessStatusEnum.Failed => "FAILED",
        ProcessStatusEnum.Cancelled => "CANCELLED",
        ProcessStatusEnum.CancelRequested => "CANCEL_REQUESTED",
        ProcessStatusEnum.Queued => "QUEUED",
        ProcessStatusEnum.StageStarted => "STAGE_STARTED",
        ProcessStatusEnum.StageCompleted => "STAGE_COMPLETED",
        ProcessStatusEnum.Compensating => "COMPENSATING",
        ProcessStatusEnum.Compensated => "COMPENSATED",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static ProcessStatusEnum FromConstant(string value) => value switch
    {
        "CREATED" => ProcessStatusEnum.Created,
        "STARTED" => ProcessStatusEnum.Started,
        "FINISHED" => ProcessStatusEnum.Finished,
        "FAILED" => ProcessStatusEnum.Failed,
        "CANCELLED" => ProcessStatusEnum.Cancelled,
        "CANCEL_REQUESTED" => ProcessStatusEnum.CancelRequested,
        "QUEUED" => ProcessStatusEnum.Queued,
        "STAGE_STARTED" => ProcessStatusEnum.StageStarted,
        "STAGE_COMPLETED" => ProcessStatusEnum.StageCompleted,
        "COMPENSATING" => ProcessStatusEnum.Compensating,
        "COMPENSATED" => ProcessStatusEnum.Compensated,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown process status: {value}")
    };

    public static bool IsTerminal(this ProcessStatusEnum status) =>
        status is ProcessStatusEnum.Finished or ProcessStatusEnum.Failed or ProcessStatusEnum.Cancelled;
}
