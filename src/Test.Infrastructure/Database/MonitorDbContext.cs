using Microsoft.EntityFrameworkCore;
using Test.Contracts.Models;

namespace Test.Infrastructure.Database;

public class MonitorDbContext : DbContext
{
    public MonitorDbContext(DbContextOptions<MonitorDbContext> options) : base(options)
    {
    }

    public DbSet<TestExecution> TestExecutions => Set<TestExecution>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestExecution>(entity =>
        {
            entity.ToTable("test_executions");
            entity.HasKey(e => e.TestId);
            entity.Property(e => e.TestId).HasColumnName("test_id").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Worker).HasColumnName("worker").HasMaxLength(100);
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UpdatedAt);
        });

        modelBuilder.Entity<EventLog>(entity =>
        {
            entity.ToTable("event_logs");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasColumnName("event_id").HasMaxLength(100);
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp").IsRequired();

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
