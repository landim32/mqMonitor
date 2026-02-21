import { CheckCircle2, Circle, AlertCircle, Loader2 } from 'lucide-react'
import { getStageByName, STATUS_CONFIG } from '../../lib/constants'
import { formatDate, formatDuration } from '../../lib/utils'
import type { SagaStepInfo } from '../../types'

interface SagaTimelineProps {
  steps: SagaStepInfo[]
}

export function SagaTimeline({ steps }: SagaTimelineProps) {
  if (steps.length === 0) {
    return (
      <div className="text-center text-sm text-zinc-600 py-6">
        No saga steps recorded
      </div>
    )
  }

  const sorted = [...steps].sort((a, b) => a.stepOrder - b.stepOrder)

  return (
    <div className="relative pl-6">
      {/* Vertical line */}
      <div className="absolute left-[11px] top-2 bottom-2 w-px bg-zinc-800" />

      {sorted.map((step, i) => {
        const stage = getStageByName(step.stageName)
        const isCompleted = step.status === 'STAGE_COMPLETED' || step.status === 'FINISHED'
        const isFailed = step.status === 'FAILED'
        const isRunning = step.status === 'STAGE_STARTED' || step.status === 'STARTED'
        const nodeColor = isFailed ? '#f87171' : isCompleted ? '#34d399' : isRunning ? '#06b6d4' : '#71717a'

        return (
          <div
            key={step.stepId}
            className="relative pb-6 last:pb-0 animate-stagger"
            style={{ animationDelay: `${i * 0.08}s` }}
          >
            {/* Node */}
            <div
              className="absolute left-[-15px] w-[22px] h-[22px] rounded-full border-2 flex items-center justify-center bg-zinc-950"
              style={{ borderColor: nodeColor }}
            >
              {isCompleted ? (
                <CheckCircle2 size={12} style={{ color: nodeColor }} />
              ) : isFailed ? (
                <AlertCircle size={12} style={{ color: nodeColor }} />
              ) : isRunning ? (
                <Loader2 size={12} className="animate-spin" style={{ color: nodeColor }} />
              ) : (
                <Circle size={8} style={{ color: nodeColor }} />
              )}
            </div>

            {/* Content */}
            <div className="ml-4">
              <div className="flex items-center gap-2 mb-0.5">
                <span className="text-sm font-semibold text-zinc-200">
                  {stage?.displayName || step.stageName}
                </span>
                {stage && (
                  <span
                    className="w-2 h-2 rounded-full"
                    style={{ backgroundColor: stage.color }}
                  />
                )}
                <span
                  className="text-[10px] font-medium px-1.5 py-0.5 rounded"
                  style={{
                    color: STATUS_CONFIG[step.status]?.color || '#71717a',
                    backgroundColor: STATUS_CONFIG[step.status]?.bgColor || 'rgba(113, 113, 122, 0.15)',
                  }}
                >
                  {STATUS_CONFIG[step.status]?.label || step.status}
                </span>
              </div>

              <div className="flex items-center gap-4 text-xs text-zinc-600">
                {step.worker && (
                  <span>Worker: <span className="text-zinc-400">{step.worker}</span></span>
                )}
                <span>{formatDate(step.startedAt)}</span>
                <span className="font-[family-name:var(--font-mono)]">
                  {formatDuration(step.startedAt, step.completedAt)}
                </span>
              </div>

              {step.errorMessage && (
                <p className="mt-1 text-xs text-red-400/80 bg-red-400/5 px-2 py-1 rounded">
                  {step.errorMessage}
                </p>
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}
