import { useState, useEffect } from 'react'
import { ProcessTable } from '../components/table/ProcessTable'
import { KanbanBoard } from '../components/kanban/KanbanBoard'
import { ProcessModal } from '../components/process/ProcessModal'
import { QueueOverview } from '../components/queue/QueueOverview'
import { MetricsBar } from '../components/ui/MetricsBar'
import { ViewToggle } from '../components/layout/ViewToggle'
import { LoadingSpinner } from '../components/ui/LoadingSpinner'
import { useProcess } from '../hooks/useProcess'
import { useQueue } from '../hooks/useQueue'
import type { ProcessExecutionInfo } from '../types'

type ViewMode = 'table' | 'kanban'

export function DashboardPage() {
  const { processes, metrics, loading, loadProcesses, loadMetrics, selectProcess, selectedProcess } = useProcess()
  const { loadPipelineStatus } = useQueue()
  const [viewMode, setViewMode] = useState<ViewMode>('table')
  const [modalOpen, setModalOpen] = useState(false)

  // Initial load
  useEffect(() => {
    loadProcesses()
    loadMetrics()
    loadPipelineStatus()
  }, [loadProcesses, loadMetrics, loadPipelineStatus])

  // Refresh metrics periodically
  useEffect(() => {
    const interval = setInterval(() => {
      loadMetrics()
    }, 10000)
    return () => clearInterval(interval)
  }, [loadMetrics])

  const handleSelectProcess = (process: ProcessExecutionInfo) => {
    selectProcess(process)
    setModalOpen(true)
  }

  const handleModalClose = (open: boolean) => {
    setModalOpen(open)
    if (!open) selectProcess(null)
  }

  if (loading && processes.length === 0) {
    return (
      <div className="flex items-center justify-center py-32">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Metrics + View Toggle */}
      <div className="flex items-center justify-between gap-4">
        <MetricsBar metrics={metrics} />
        <ViewToggle mode={viewMode} onChange={setViewMode} />
      </div>

      {/* Views */}
      {viewMode === 'table' ? (
        <ProcessTable onSelectProcess={handleSelectProcess} />
      ) : (
        <KanbanBoard onSelectProcess={handleSelectProcess} />
      )}

      {/* Queue Overview */}
      <QueueOverview />

      {/* Process Modal */}
      <ProcessModal
        process={selectedProcess}
        open={modalOpen}
        onOpenChange={handleModalClose}
      />
    </div>
  )
}
