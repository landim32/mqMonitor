import { useState, type CSSProperties } from 'react'
import { X } from 'lucide-react'
import { toast } from 'sonner'
import { StatusBadge } from '../ui/StatusBadge'
import { PriorityControl } from '../ui/PriorityControl'
import { ConfirmModal } from '../ui/ConfirmModal'
import { useProcess } from '../../hooks/useProcess'
import { getStageByName, isTerminalStatus } from '../../lib/constants'
import { formatDate, cn } from '../../lib/utils'
import type { ProcessExecutionInfo } from '../../types'

interface ProcessRowProps {
  process: ProcessExecutionInfo
  onClick: () => void
  style?: CSSProperties
}

export function ProcessRow({ process, onClick, style }: ProcessRowProps) {
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
    <>
      <div
        onClick={onClick}
        style={style}
        className="flex items-center px-3 py-2.5 hover:bg-zinc-800/40 cursor-pointer transition-colors animate-stagger group"
      >
        {/* Process ID */}
        <span className="w-[160px] text-sm font-[family-name:var(--font-mono)] text-cyan-400/80 truncate">
          {process.processId}
        </span>

        {/* Status */}
        <span className="w-[130px]">
          <StatusBadge status={process.status} />
        </span>

        {/* Stage */}
        <span className="w-[120px] text-sm text-zinc-400">
          {stage ? (
            <span className="flex items-center gap-1.5">
              <span
                className="w-2 h-2 rounded-full"
                style={{ backgroundColor: stage.color }}
              />
              {stage.displayName}
            </span>
          ) : (
            <span className="text-zinc-600">—</span>
          )}
        </span>

        {/* Priority */}
        <span className="w-[100px]" onClick={e => e.stopPropagation()}>
          <PriorityControl
            priority={process.priority}
            onIncrease={() => handlePriorityChange(1)}
            onDecrease={() => handlePriorityChange(-1)}
            disabled={isTerminal}
            compact
          />
        </span>

        {/* Message */}
        <span className="flex-1 text-sm text-zinc-500 truncate pr-3">
          {process.message || '—'}
        </span>

        {/* Updated */}
        <span className="w-[160px] text-xs text-zinc-600 font-[family-name:var(--font-mono)]">
          {formatDate(process.updatedAt)}
        </span>

        {/* Actions */}
        <span className="w-[80px] flex items-center justify-end">
          {!isTerminal && (
            <button
              onClick={(e) => { e.stopPropagation(); setConfirmKill(true) }}
              className={cn(
                'p-1.5 rounded-md opacity-0 group-hover:opacity-100 transition-all',
                'text-zinc-500 hover:text-red-400 hover:bg-red-400/10'
              )}
              title="Cancel process"
            >
              <X size={14} />
            </button>
          )}
        </span>
      </div>

      <ConfirmModal
        open={confirmKill}
        onOpenChange={setConfirmKill}
        onConfirm={handleKill}
        title="Cancel Process"
        description={`Are you sure you want to cancel process ${process.processId}? This action cannot be undone.`}
        confirmLabel="Cancel Process"
        variant="danger"
        loading={killing}
      />
    </>
  )
}
