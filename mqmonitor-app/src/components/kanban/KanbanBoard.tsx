import { useMemo } from 'react'
import { KanbanColumn } from './KanbanColumn'
import { useProcess } from '../../hooks/useProcess'
import { useQueue } from '../../hooks/useQueue'
import { STAGES } from '../../lib/constants'
import type { ProcessExecutionInfo } from '../../types'

interface KanbanBoardProps {
  onSelectProcess: (process: ProcessExecutionInfo) => void
}

export function KanbanBoard({ onSelectProcess }: KanbanBoardProps) {
  const { processes } = useProcess()
  const { pipelineStatus } = useQueue()

  // Group processes by stage + terminal columns
  const columns = useMemo(() => {
    const stageColumns = STAGES.map(stage => {
      const stageProcesses = processes.filter(
        p => p.currentStage?.toLowerCase() === stage.name.toLowerCase() &&
             !['FINISHED', 'FAILED', 'CANCELLED', 'COMPENSATED'].includes(p.status)
      )
      const queueInfo = pipelineStatus?.stages.find(
        q => q.stageName?.toLowerCase() === stage.name.toLowerCase()
      )
      return {
        id: stage.name,
        title: stage.displayName,
        color: stage.color,
        processes: stageProcesses,
        messageCount: queueInfo?.messageCount ?? 0,
        consumerCount: queueInfo?.consumerCount ?? 0,
      }
    })

    const completedProcesses = processes.filter(p => p.status === 'FINISHED')
    const failedProcesses = processes.filter(
      p => ['FAILED', 'CANCELLED', 'COMPENSATED'].includes(p.status)
    )

    return [
      ...stageColumns,
      {
        id: 'completed',
        title: 'Completed',
        color: '#34d399',
        processes: completedProcesses,
        messageCount: 0,
        consumerCount: 0,
      },
      {
        id: 'failed',
        title: 'Failed / Cancelled',
        color: '#f87171',
        processes: failedProcesses,
        messageCount: 0,
        consumerCount: 0,
      },
    ]
  }, [processes, pipelineStatus])

  return (
    <div className="flex gap-3 overflow-x-auto pb-4 animate-fade-in" style={{ minHeight: 'calc(100vh - 220px)' }}>
      {columns.map((col) => (
        <KanbanColumn
          key={col.id}
          title={col.title}
          color={col.color}
          processes={col.processes}
          messageCount={col.messageCount}
          consumerCount={col.consumerCount}
          onSelectProcess={onSelectProcess}
        />
      ))}
    </div>
  )
}
