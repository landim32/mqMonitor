export interface ProcessExecutionInfo {
  processId: string
  status: string
  worker: string | null
  startedAt: string | null
  finishedAt: string | null
  updatedAt: string
  errorMessage: string | null
  message: string | null
  currentStage: string | null
  priority: number
  sagaStatus: string | null
}

export interface CreateProcessRequest {
  stageName: string
  message?: string
  priority: number
}

export interface CreateProcessResponse {
  processId: string
  stageName: string
  priority: number
  status: string
  createdAt: string
}

export interface QueueStatusInfo {
  queueName: string
  stageName: string | null
  displayName: string | null
  messageCount: number
  consumerCount: number
  publishRate: number
  deliverRate: number
  ackRate: number
  state: string
}

export interface PipelineStatusInfo {
  stages: QueueStatusInfo[]
  systemQueues: QueueStatusInfo[]
  totalMessages: number
  totalConsumers: number
}

export interface SagaStepInfo {
  stepId: string
  processId: string
  stageName: string
  status: string
  worker: string | null
  startedAt: string
  completedAt: string | null
  errorMessage: string | null
  stepOrder: number
}

export interface EventLogInfo {
  eventId: string
  processId: string
  type: string
  payload: string
  timestamp: string
}

export interface ProcessMetricsInfo {
  totalExecuted: number
  inProgress: number
  failed: number
  cancelled: number
  finished: number
  averageExecutionTimeMs: number
  errorRate: number
  byStage: Record<string, number>
}

export interface ChangePriorityRequest {
  priority: number
}
