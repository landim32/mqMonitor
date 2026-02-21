import { useState, useEffect, useCallback } from 'react'
import { useParams } from 'react-router-dom'
import { ProcessDetailView } from '../components/process/ProcessDetailView'
import { LoadingSpinner } from '../components/ui/LoadingSpinner'
import { useProcess } from '../hooks/useProcess'
import { useSignalR } from '../hooks/useSignalR'
import { processService } from '../services/processService'
import type { ProcessExecutionInfo, SagaStepInfo, EventLogInfo } from '../types'

export function ProcessPage() {
  const { id } = useParams<{ id: string }>()
  const { connection } = useSignalR()
  const { selectedProcess, selectProcess } = useProcess()
  const [process, setProcess] = useState<ProcessExecutionInfo | null>(null)
  const [sagaSteps, setSagaSteps] = useState<SagaStepInfo[]>([])
  const [events, setEvents] = useState<EventLogInfo[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadData = useCallback(async () => {
    if (!id) return
    try {
      setLoading(true)
      const [proc, steps, evts] = await Promise.all([
        processService.getById(id),
        processService.getSagaSteps(id),
        processService.getEvents(id),
      ])
      setProcess(proc)
      selectProcess(proc)
      setSagaSteps(steps)
      setEvents(evts)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load process')
    } finally {
      setLoading(false)
    }
  }, [id, selectProcess])

  // Initial load
  useEffect(() => {
    loadData()
  }, [loadData])

  // Subscribe to process-specific SignalR group
  useEffect(() => {
    if (!connection || !id) return

    connection.invoke('SubscribeToProcess', id).catch(console.error)

    // Listen for updates to refresh saga steps and events
    const handler = (updated: ProcessExecutionInfo) => {
      if (updated.processId === id) {
        setProcess(prev => prev ? { ...prev, ...updated } : updated)
        // Refresh saga steps and events on update
        processService.getSagaSteps(id).then(setSagaSteps).catch(console.error)
        processService.getEvents(id).then(setEvents).catch(console.error)
      }
    }

    connection.on('ProcessUpdated', handler)

    return () => {
      connection.off('ProcessUpdated', handler)
      connection.invoke('UnsubscribeFromProcess', id).catch(console.error)
    }
  }, [connection, id])

  // Also use real-time selectedProcess from context
  const displayProcess = selectedProcess && process && selectedProcess.processId === process.processId
    ? { ...process, ...selectedProcess }
    : process

  if (loading) {
    return (
      <div className="flex items-center justify-center py-32">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  if (error || !displayProcess) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-zinc-500">
        <p className="text-lg font-medium">Process not found</p>
        <p className="text-sm mt-1">{error || `No process with ID "${id}"`}</p>
      </div>
    )
  }

  return (
    <ProcessDetailView
      process={displayProcess}
      sagaSteps={sagaSteps}
      events={events}
    />
  )
}
