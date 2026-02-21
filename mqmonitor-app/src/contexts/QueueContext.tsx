import { createContext, useState, useEffect, useCallback, type ReactNode } from 'react'
import { queueService } from '../services/queueService'
import { useSignalR } from '../hooks/useSignalR'
import type { PipelineStatusInfo } from '../types'

interface QueueContextType {
  pipelineStatus: PipelineStatusInfo | null
  loading: boolean
  loadPipelineStatus: () => Promise<void>
}

const QueueContext = createContext<QueueContextType | undefined>(undefined)

export const QueueProvider = ({ children }: { children: ReactNode }) => {
  const [pipelineStatus, setPipelineStatus] = useState<PipelineStatusInfo | null>(null)
  const [loading, setLoading] = useState(false)
  const { connection } = useSignalR()

  // Listen for SignalR QueueStatsUpdated events
  useEffect(() => {
    if (!connection) return

    const handler = (updated: PipelineStatusInfo) => {
      setPipelineStatus(updated)
    }

    connection.on('QueueStatsUpdated', handler)
    return () => { connection.off('QueueStatsUpdated', handler) }
  }, [connection])

  const loadPipelineStatus = useCallback(async () => {
    try {
      setLoading(true)
      const result = await queueService.getPipelineStatus()
      setPipelineStatus(result)
    } catch (err) {
      console.error('Failed to load pipeline status:', err)
    } finally {
      setLoading(false)
    }
  }, [])

  return (
    <QueueContext.Provider value={{ pipelineStatus, loading, loadPipelineStatus }}>
      {children}
    </QueueContext.Provider>
  )
}

export default QueueContext
