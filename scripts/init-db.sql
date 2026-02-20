-- Initialize test_monitor database schema
-- This script is executed automatically by EF Core migrations,
-- but can be run manually if needed.

CREATE TABLE IF NOT EXISTS test_executions (
    test_id VARCHAR(100) PRIMARY KEY,
    status VARCHAR(50) NOT NULL,
    worker VARCHAR(100),
    started_at TIMESTAMP WITH TIME ZONE,
    finished_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
    error_message VARCHAR(2000)
);

CREATE INDEX IF NOT EXISTS ix_test_executions_status ON test_executions (status);
CREATE INDEX IF NOT EXISTS ix_test_executions_updated_at ON test_executions (updated_at);

CREATE TABLE IF NOT EXISTS event_logs (
    event_id VARCHAR(100) PRIMARY KEY,
    type VARCHAR(50) NOT NULL,
    payload JSONB NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_event_logs_type ON event_logs (type);
CREATE INDEX IF NOT EXISTS ix_event_logs_timestamp ON event_logs (timestamp);
