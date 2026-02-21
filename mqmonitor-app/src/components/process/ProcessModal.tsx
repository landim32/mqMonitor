import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { ExternalLink, X } from 'lucide-react'
import { toast } from 'sonner'
import {
  Modal, ModalContent, ModalHeader, ModalTitle,
  ModalDescription, ModalClose,
} from '../ui/Modal'
import { StatusBadge } from '../ui/StatusBadge'
import { PriorityControl } from '../ui/PriorityControl'
import { ConfirmModal } from '../ui/ConfirmModal'
import { useProcess } from '../../hooks/useProcess'
import { useSignalR } from '../../hooks/useSignalR'
import { getStageByName, isTerminalStatus } from '../../lib/constants'
import { formatDate, cn } from '../../lib/utils'
import type { ProcessExecutionInfo } from '../../types'

interface ProcessModalProps {
  process: ProcessExecutionInfo | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ProcessModal({ process, open, onOpenChange }: ProcessModalProps) {
  const { cancelProcess, updatePriority, selectedProcess } = useProcess()
  const { connection } = useSignalR()
  const [confirmKill, setConfirmKill] = useState(false)
  const [killing, setKilling] = useState(false)

  // The displayed process: prefer selectedProcess (real-time updated) over the original prop
  const displayProcess = selectedProcess && process && selectedProcess.processId === process.processId
    ? selectedProcess
    : process

  // Subscribe/unsubscribe to process-specific SignalR group
  useEffect(() => {
    if (!connection || !process || !open) return

    connection.invoke('SubscribeToProcess', process.processId).catch(console.error)

    return () => {
      connection.invoke('UnsubscribeFromProcess', process.processId).catch(console.error)
    }
  }, [connection, process?.processId, open])

  if (!displayProcess) return null

  const stage = displayProcess.currentStage ? getStageByName(displayProcess.currentStage) : null
  const isTerminal = isTerminalStatus(displayProcess.status)

  const handleKill = async () => {
    setKilling(true)
    try {
      await cancelProcess(displayProcess.processId)
      toast.success(`Cancel command sent for ${displayProcess.processId}`)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to cancel process')
    } finally {
      setKilling(false)
    }
  }

  const handlePriorityChange = async (delta: number) => {
    const newPriority = Math.max(0, Math.min(10, displayProcess.priority + delta))
    try {
      await updatePriority(displayProcess.processId, newPriority)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update priority')
    }
  }

  return (
    <>
      <Modal open={open} onOpenChange={onOpenChange}>
        <ModalContent className="max-w-md">
          <ModalHeader>
            <div className="flex items-center justify-between">
              <ModalTitle className="font-[family-name:var(--font-mono)] text-base">
                {displayProcess.processId}
              </ModalTitle>
              <ModalClose asChild>
                <button className="p-1 rounded-md text-zinc-500 hover:text-zinc-300 hover:bg-zinc-800 transition-colors">
                  <X size={16} />
                </button>
              </ModalClose>
            </div>
            <ModalDescription>Process details — updated in real-time</ModalDescription>
          </ModalHeader>

          <div className="space-y-4">
            {/* Status + Priority row */}
            <div className="flex items-center justify-between">
              <StatusBadge status={displayProcess.status} />
              <PriorityControl
                priority={displayProcess.priority}
                onIncrease={() => handlePriorityChange(1)}
                onDecrease={() => handlePriorityChange(-1)}
                disabled={isTerminal}
              />
            </div>

            {/* Info grid */}
            <div className="grid grid-cols-2 gap-3">
              <InfoItem label="Stage" value={stage?.displayName || displayProcess.currentStage || '—'} />
              <InfoItem label="Worker" value={displayProcess.worker || '—'} />
              <InfoItem label="Started" value={formatDate(displayProcess.startedAt)} />
              <InfoItem label="Finished" value={formatDate(displayProcess.finishedAt)} />
              <InfoItem label="Updated" value={formatDate(displayProcess.updatedAt)} />
              <InfoItem label="Saga" value={displayProcess.sagaStatus || '—'} />
            </div>

            {/* Message */}
            {displayProcess.message && (
              <div>
                <span className="text-[10px] text-zinc-600 uppercase tracking-wider">Message</span>
                <p className="text-sm text-zinc-300 mt-0.5">{displayProcess.message}</p>
              </div>
            )}

            {/* Error */}
            {displayProcess.errorMessage && (
              <div className="p-2.5 rounded-lg bg-red-500/10 border border-red-500/20">
                <span className="text-[10px] text-red-400 uppercase tracking-wider">Error</span>
                <p className="text-sm text-red-300 mt-0.5">{displayProcess.errorMessage}</p>
              </div>
            )}

            {/* Actions */}
            <div className="flex items-center justify-between pt-2 border-t border-zinc-800/50">
              <Link
                to={`/processes/${displayProcess.processId}`}
                onClick={() => onOpenChange(false)}
                className="flex items-center gap-1.5 text-sm text-cyan-400 hover:text-cyan-300 transition-colors"
              >
                <ExternalLink size={14} />
                View Full Details
              </Link>
              {!isTerminal && (
                <button
                  onClick={() => setConfirmKill(true)}
                  className="px-3 py-1.5 rounded-lg text-sm font-medium text-red-400 hover:bg-red-400/10 transition-colors"
                >
                  Cancel Process
                </button>
              )}
            </div>
          </div>
        </ModalContent>
      </Modal>

      <ConfirmModal
        open={confirmKill}
        onOpenChange={setConfirmKill}
        onConfirm={handleKill}
        title="Cancel Process"
        description={`Are you sure you want to cancel process ${displayProcess.processId}?`}
        confirmLabel="Cancel Process"
        variant="danger"
        loading={killing}
      />
    </>
  )
}

function InfoItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-[10px] text-zinc-600 uppercase tracking-wider">{label}</span>
      <p className={cn(
        'text-sm mt-0.5',
        value === '—' ? 'text-zinc-700' : 'text-zinc-300'
      )}>
        {value}
      </p>
    </div>
  )
}
