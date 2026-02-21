import { useState } from 'react'
import { useQueue } from '../../hooks/useQueue'
import { cn } from '../../lib/utils'
import { getStageByName } from '../../lib/constants'
import { Database, AlertTriangle, RefreshCw, Settings, ChevronDown, ChevronRight } from 'lucide-react'
import type { QueueStatusInfo } from '../../types'

interface QueueCategory {
  key: string
  label: string
  icon: React.ElementType
  color: string
  queues: QueueStatusInfo[]
}

function categorizeQueues(
  stages: QueueStatusInfo[],
  systemQueues: QueueStatusInfo[]
): QueueCategory[] {
  const dlqQueues = systemQueues.filter(q => q.queueName.endsWith('.dlq'))
  const retryQueues = systemQueues.filter(q => q.queueName.endsWith('.retry'))
  const otherQueues = systemQueues.filter(
    q => !q.queueName.endsWith('.dlq') && !q.queueName.endsWith('.retry')
  )

  return [
    { key: 'stages', label: 'Stage Queues', icon: Database, color: 'text-cyan-400', queues: stages },
    { key: 'dlq', label: 'DLQ Queues', icon: AlertTriangle, color: 'text-red-400', queues: dlqQueues },
    { key: 'retry', label: 'Retry Queues', icon: RefreshCw, color: 'text-amber-400', queues: retryQueues },
    { key: 'system', label: 'System Queues', icon: Settings, color: 'text-zinc-400', queues: otherQueues },
  ]
}

function QueueCard({ queue }: { queue: QueueStatusInfo }) {
  const stage = queue.stageName ? getStageByName(queue.stageName) : undefined
  const isRunning = queue.state?.toLowerCase() === 'running'
  const hasMessages = queue.messageCount > 0
  const hasConsumers = queue.consumerCount > 0

  return (
    <div className="bg-zinc-800/50 border border-zinc-700/50 rounded-lg p-3 hover:border-zinc-600/60 transition-colors">
      {/* Queue name + state */}
      <div className="flex items-center gap-2 mb-2">
        <span
          className={cn(
            'h-2 w-2 rounded-full shrink-0',
            isRunning ? 'bg-emerald-400' : 'bg-red-400'
          )}
        />
        <span
          className="text-xs font-mono text-zinc-300 truncate"
          title={queue.queueName}
        >
          {stage?.displayName ?? queue.queueName}
        </span>
      </div>

      {/* Badges row */}
      <div className="flex items-center gap-2 flex-wrap">
        {/* Message count */}
        <span
          className={cn(
            'inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-semibold font-mono',
            hasMessages
              ? 'bg-amber-500/15 text-amber-400 border border-amber-500/20'
              : 'bg-zinc-700/40 text-zinc-500 border border-zinc-700/30'
          )}
        >
          {queue.messageCount} msg
        </span>

        {/* Consumer count */}
        <span
          className={cn(
            'inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-semibold font-mono',
            hasConsumers
              ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/20'
              : 'bg-zinc-700/40 text-zinc-500 border border-zinc-700/30'
          )}
        >
          {queue.consumerCount} cons
        </span>
      </div>

      {/* Rates (if non-zero) */}
      {(queue.publishRate > 0 || queue.deliverRate > 0) && (
        <div className="mt-2 flex items-center gap-3 text-[10px] text-zinc-500 font-mono">
          {queue.publishRate > 0 && (
            <span>pub: {queue.publishRate.toFixed(1)}/s</span>
          )}
          {queue.deliverRate > 0 && (
            <span>del: {queue.deliverRate.toFixed(1)}/s</span>
          )}
        </div>
      )}
    </div>
  )
}

function CategorySection({ category }: { category: QueueCategory }) {
  const [collapsed, setCollapsed] = useState(false)
  const Icon = category.icon
  const CollapseIcon = collapsed ? ChevronRight : ChevronDown

  if (category.queues.length === 0) return null

  return (
    <div>
      {/* Header */}
      <button
        onClick={() => setCollapsed(prev => !prev)}
        className="flex items-center gap-2 w-full text-left mb-2 group cursor-pointer"
      >
        <CollapseIcon size={14} className="text-zinc-500 group-hover:text-zinc-300 transition-colors" />
        <Icon size={14} className={cn('opacity-80', category.color)} />
        <span className="text-xs font-semibold uppercase tracking-wider text-zinc-400 group-hover:text-zinc-300 transition-colors">
          {category.label}
        </span>
        <span className="inline-flex items-center justify-center h-4 min-w-[16px] px-1 rounded-full bg-zinc-700/60 text-[10px] font-mono font-semibold text-zinc-400">
          {category.queues.length}
        </span>
      </button>

      {/* Grid */}
      {!collapsed && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-2">
          {category.queues.map(queue => (
            <QueueCard key={queue.queueName} queue={queue} />
          ))}
        </div>
      )}
    </div>
  )
}

export function QueueOverview() {
  const { pipelineStatus } = useQueue()

  if (!pipelineStatus) return null

  const categories = categorizeQueues(
    pipelineStatus.stages,
    pipelineStatus.systemQueues
  )

  const visibleCategories = categories.filter(c => c.queues.length > 0)

  if (visibleCategories.length === 0) return null

  return (
    <div className="bg-zinc-900/60 border border-zinc-800/50 rounded-lg p-4 backdrop-blur-sm animate-fade-in">
      {/* Section title */}
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold text-zinc-300 uppercase tracking-wider">
          Queue Overview
        </h2>
        <div className="flex items-center gap-3 text-[10px] font-mono text-zinc-500">
          <span>{pipelineStatus.totalMessages} total msg</span>
          <span>{pipelineStatus.totalConsumers} consumers</span>
        </div>
      </div>

      {/* Categories */}
      <div className="space-y-4">
        {visibleCategories.map(category => (
          <CategorySection key={category.key} category={category} />
        ))}
      </div>
    </div>
  )
}
