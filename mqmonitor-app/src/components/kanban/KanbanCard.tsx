import { useState } from 'react'
import { X } from 'lucide-react'
import { toast } from 'sonner'
import { StatusBadge } from '../ui/StatusBadge'
import { PriorityControl } from '../ui/PriorityControl'
import { ConfirmModal } from '../ui/ConfirmModal'
import { useProcess } from '../../hooks/useProcess'
import { STATUS_CONFIG, isTerminalStatus } from '../../lib/constants'
import { cn } from '../../lib/utils'
import type { ProcessExecutionInfo } from '../../types'

interface KanbanCardProps {
  process: ProcessExecutionInfo
  onClick: () => void
  staggerIndex: number
}

export function KanbanCard({ process, onClick, staggerIndex }: KanbanCardProps) {
  const { cancelProcess, updatePriority } = useProcess()
  const [confirmKill, setConfirmKill] = useState(false)
  const [killing, setKilling] = useState(false)

  const statusConfig = STATUS_CONFIG[process.status]
  const borderColor = statusConfig?.color || '#71717a'
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
        className={cn(
          'rounded-lg border border-zinc-800/50 bg-zinc-800/30 p-2.5 cursor-pointer',
          'hover:bg-zinc-800/50 hover:border-zinc-700/50 hover:-translate-y-0.5 hover:shadow-lg hover:shadow-black/20',
          'transition-all animate-stagger group'
        )}
        style={{
          borderLeftWidth: '3px',
          borderLeftColor: borderColor,
          animationDelay: `${staggerIndex * 0.03}s`,
        }}
      >
        {/* Header: ID + Kill */}
        <div className="flex items-center justify-between mb-1.5">
          <span className="text-xs font-[family-name:var(--font-mono)] text-cyan-400/70 truncate">
            {process.processId}
          </span>
          {!isTerminal && (
            <button
              onClick={(e) => { e.stopPropagation(); setConfirmKill(true) }}
              className="p-0.5 rounded opacity-0 group-hover:opacity-100 transition-opacity text-zinc-600 hover:text-red-400"
            >
              <X size={12} />
            </button>
          )}
        </div>

        {/* Status + Priority */}
        <div className="flex items-center justify-between">
          <StatusBadge status={process.status} />
          <div onClick={e => e.stopPropagation()}>
            <PriorityControl
              priority={process.priority}
              onIncrease={() => handlePriorityChange(1)}
              onDecrease={() => handlePriorityChange(-1)}
              disabled={isTerminal}
              compact
            />
          </div>
        </div>

        {/* Message */}
        {process.message && (
          <p className="mt-1.5 text-[11px] text-zinc-600 truncate">
            {process.message}
          </p>
        )}
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
    </>
  )
}
