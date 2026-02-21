using Microsoft.EntityFrameworkCore;

namespace MqMonitor.Infra.Context;

public class MonitorDbContext : DbContext
{
    public MonitorDbContext(DbContextOptions<MonitorDbContext> options) : base(options)
    {
    }

    public DbSet<ProcessExecution> ProcessExecutions => Set<ProcessExecution>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();
    public DbSet<SagaStep> SagaSteps => Set<SagaStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessExecution>(entity =>
        {
            entity.ToTable("process_executions");
            entity.HasKey(e => e.ProcessId);
            entity.Property(e => e.ProcessId).HasColumnName("process_id").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Worker).HasColumnName("worker").HasMaxLength(100);
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.CurrentStage).HasColumnName("current_stage").HasMaxLength(100);
            entity.Property(e => e.Priority).HasColumnName("priority").HasDefaultValue(0);
            entity.Property(e => e.SagaStatus).HasColumnName("saga_status").HasMaxLength(50);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => e.CurrentStage);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.SagaStatus);
        });

        modelBuilder.Entity<EventLog>(entity =>
        {
            entity.ToTable("event_logs");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasColumnName("event_id").HasMaxLength(100);
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp").IsRequired();
            entity.Property(e => e.ProcessId).HasColumnName("process_id").HasMaxLength(100).IsRequired();

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ProcessId);
        });

        modelBuilder.Entity<SagaStep>(entity =>
        {
            entity.ToTable("saga_steps");
            entity.HasKey(e => e.StepId);
            entity.Property(e => e.StepId).HasColumnName("step_id").HasMaxLength(100);
            entity.Property(e => e.ProcessId).HasColumnName("process_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.StageName).HasColumnName("stage_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Worker).HasColumnName("worker").HasMaxLength(100);
            entity.Property(e => e.StartedAt).HasColumnName("started_at").IsRequired();
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            entity.Property(e => e.StepOrder).HasColumnName("step_order").IsRequired();

            entity.HasIndex(e => e.ProcessId);
            entity.HasIndex(e => new { e.ProcessId, e.StepOrder });
            entity.HasIndex(e => e.StageName);
            entity.HasIndex(e => e.Status);
        });
    }
}
