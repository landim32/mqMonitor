import { useState } from 'react'
import { ArrowLeft, X } from 'lucide-react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { StatusBadge } from '../ui/StatusBadge'
import { PriorityControl } from '../ui/PriorityControl'
import { ConfirmModal } from '../ui/ConfirmModal'
import { SagaTimeline } from './SagaTimeline'
import { EventHistory } from './EventHistory'
import { useProcess } from '../../hooks/useProcess'
import { getStageByName, isTerminalStatus } from '../../lib/constants'
import { formatDate, formatDuration, cn } from '../../lib/utils'
import type { ProcessExecutionInfo, SagaStepInfo, EventLogInfo } from '../../types'

interface ProcessDetailViewProps {
  process: ProcessExecutionInfo
  sagaSteps: SagaStepInfo[]
  events: EventLogInfo[]
}

export function ProcessDetailView({ process, sagaSteps, events }: ProcessDetailViewProps) {
  const { cancelProcess, updatePriority } = useProcess()
  const [confirmKill, setConfirmKill] = useState(false)
  const [killing, setKilling] = useState(false)

  const stage = process.currentStage ? getStageByName(process.currentStage) : null
  const isTerminal = isTerminalStatus(process.status)

  const handleKill = async () => {
    setKilling(true)
    try {
      await cancelProcess(process.processId)
      toast.success(`Cancel command sent for ${process.processId}`)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to cancel process')
    } finally {
      setKilling(false)
    }
  }

  const handlePriorityChange = async (delta: number) => {
    const newPriority = Math.max(0, Math.min(10, process.priority + delta))
    try {
      await updatePriority(process.processId, newPriority)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update priority')
    }
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6 animate-fade-in">
      {/* Breadcrumb */}
      <Link
        to="/"
        className="flex items-center gap-1.5 text-sm text-zinc-500 hover:text-cyan-400 transition-colors w-fit"
      >
        <ArrowLeft size={14} />
        Back to Dashboard
      </Link>

      {/* Header Card */}
      <div className="bg-zinc-900/50 border border-zinc-800/50 rounded-xl p-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h1 className="text-xl font-[family-name:var(--font-mono)] font-semibold text-zinc-100">
              {process.processId}
            </h1>
            <p className="text-sm text-zinc-500 mt-0.5">
              Process details — updated in real-time
            </p>
          </div>
          <div className="flex items-center gap-3">
            <PriorityControl
              priority={process.priority}
              onIncrease={() => handlePriorityChange(1)}
              onDecrease={() => handlePriorityChange(-1)}
              disabled={isTerminal}
            />
            {!isTerminal && (
              <button
                onClick={() => setConfirmKill(true)}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium text-red-400 hover:bg-red-400/10 border border-red-400/20 transition-colors"
              >
                <X size={14} />
                Cancel
              </button>
            )}
          </div>
        </div>

        {/* Status + Info grid */}
        <div className="flex items-center gap-4 mb-5">
          <StatusBadge status={process.status} />
          {stage && (
            <span className="flex items-center gap-1.5 text-sm text-zinc-400">
              <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: stage.color }} />
              {stage.displayName}
            </span>
          )}
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <DetailItem label="Worker" value={process.worker || '—'} />
          <DetailItem label="Message" value={process.message || '—'} />
          <DetailItem label="Started" value={formatDate(process.startedAt)} />
          <DetailItem label="Finished" value={formatDate(process.finishedAt)} />
          <DetailItem label="Duration" value={formatDuration(process.startedAt, process.finishedAt)} />
          <DetailItem label="Updated" value={formatDate(process.updatedAt)} />
          <DetailItem label="Saga Status" value={process.sagaStatus || '—'} />
          <DetailItem label="Current Stage" value={process.currentStage || '—'} />
        </div>

        {/* Error */}
        {process.errorMessage && (
          <div className="mt-4 p-3 rounded-lg bg-red-500/10 border border-red-500/20">
            <span className="text-[10px] text-red-400 uppercase tracking-wider font-medium">Error</span>
            <p className="text-sm text-red-300 mt-1">{process.errorMessage}</p>
          </div>
        )}
      </div>

      {/* Saga Timeline */}
      <div className="bg-zinc-900/50 border border-zinc-800/50 rounded-xl p-6">
        <h2 className="text-base font-semibold text-zinc-200 mb-4">Saga Timeline</h2>
        <SagaTimeline steps={sagaSteps} />
      </div>

      {/* Event History */}
      <div className="bg-zinc-900/50 border border-zinc-800/50 rounded-xl p-6">
        <h2 className="text-base font-semibold text-zinc-200 mb-4">Event History</h2>
        <EventHistory events={events} />
      </div>

      <ConfirmModal
        open={confirmKill}
        onOpenChange={setConfirmKill}
        onConfirm={handleKill}
        title="Cancel Process"
        description={`Are you sure you want to cancel process ${process.processId}?`}
        confirmLabel="Cancel Process"
        variant="danger"
        loading={killing}
      />
    </div>
  )
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-[10px] text-zinc-600 uppercase tracking-wider">{label}</span>
      <p className={cn(
        'text-sm mt-0.5 font-[family-name:var(--font-mono)]',
        value === '—' ? 'text-zinc-700' : 'text-zinc-300'
      )}>
        {value}
      </p>
    </div>
  )
}
