import { createContext, useState, useCallback, useEffect, type ReactNode } from 'react'
import { processService } from '../services/processService'
import { useSignalR } from '../hooks/useSignalR'
import type { ProcessExecutionInfo, CreateProcessRequest, CreateProcessResponse, ProcessMetricsInfo } from '../types'

interface ProcessContextType {
  processes: ProcessExecutionInfo[]
  selectedProcess: ProcessExecutionInfo | null
  metrics: ProcessMetricsInfo | null
  loading: boolean
  error: string | null
  loadProcesses: (stage?: string, status?: string) => Promise<void>
  createProcess: (request: CreateProcessRequest) => Promise<CreateProcessResponse>
  cancelProcess: (processId: string) => Promise<void>
  updatePriority: (processId: string, priority: number) => Promise<void>
  selectProcess: (process: ProcessExecutionInfo | null) => void
  loadMetrics: () => Promise<void>
  clearError: () => void
}

const ProcessContext = createContext<ProcessContextType | undefined>(undefined)

export const ProcessProvider = ({ children }: { children: ReactNode }) => {
  const [processes, setProcesses] = useState<ProcessExecutionInfo[]>([])
  const [selectedProcess, setSelectedProcess] = useState<ProcessExecutionInfo | null>(null)
  const [metrics, setMetrics] = useState<ProcessMetricsInfo | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const { connection } = useSignalR()

  // Listen for SignalR ProcessUpdated events
  useEffect(() => {
    if (!connection) return

    const handler = (updated: ProcessExecutionInfo) => {
      setProcesses(prev => {
        const idx = prev.findIndex(p => p.processId === updated.processId)
        if (idx >= 0) {
          const copy = [...prev]
          copy[idx] = { ...copy[idx], ...updated }
          return copy
        }
        return [updated, ...prev]
      })
      // Also update selected process if it matches
      setSelectedProcess(prev =>
        prev && prev.processId === updated.processId ? { ...prev, ...updated } : prev
      )
    }

    connection.on('ProcessUpdated', handler)
    return () => { connection.off('ProcessUpdated', handler) }
  }, [connection])

  const loadProcesses = useCallback(async (stage?: string, status?: string) => {
    try {
      setLoading(true)
      setError(null)
      const result = await processService.list(stage, status)
      setProcesses(result)
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Failed to load processes'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }, [])

  const createProcess = useCallback(async (request: CreateProcessRequest): Promise<CreateProcessResponse> => {
    setError(null)
    const result = await processService.create(request)
    return result
  }, [])

  const cancelProcess = useCallback(async (processId: string) => {
    setError(null)
    await processService.cancel(processId)
  }, [])

  const updatePriority = useCallback(async (processId: string, priority: number) => {
    setError(null)
    await processService.updatePriority(processId, priority)
    // Optimistic update
    setProcesses(prev => prev.map(p => p.processId === processId ? { ...p, priority } : p))
    setSelectedProcess(prev => prev && prev.processId === processId ? { ...prev, priority } : prev)
  }, [])

  const selectProcess = useCallback((process: ProcessExecutionInfo | null) => {
    setSelectedProcess(process)
  }, [])

  const loadMetrics = useCallback(async () => {
    try {
      const result = await processService.getMetrics()
      setMetrics(result)
    } catch (err) {
      console.error('Failed to load metrics:', err)
    }
  }, [])

  const clearError = useCallback(() => { setError(null) }, [])

  const value: ProcessContextType = {
    processes, selectedProcess, metrics, loading, error,
    loadProcesses, createProcess, cancelProcess, updatePriority,
    selectProcess, loadMetrics, clearError,
  }

  return <ProcessContext.Provider value={value}>{children}</ProcessContext.Provider>
}

export default ProcessContext
