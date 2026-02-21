import { createContext, useState, useEffect, useRef, type ReactNode } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

type ConnectionState = 'connecting' | 'connected' | 'disconnected' | 'reconnecting'

interface SignalRContextType {
  connection: HubConnection | null
  connectionState: ConnectionState
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined)

export const SignalRProvider = ({ children }: { children: ReactNode }) => {
  const [connectionState, setConnectionState] = useState<ConnectionState>('connecting')
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/monitor')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build()

    connectionRef.current = conn

    conn.onreconnecting(() => setConnectionState('reconnecting'))
    conn.onreconnected(async () => {
      setConnectionState('connected')
      try { await conn.invoke('SubscribeToAll') } catch (e) { console.error('Failed to resubscribe:', e) }
    })
    conn.onclose(() => setConnectionState('disconnected'))

    const start = async () => {
      try {
        await conn.start()
        setConnectionState('connected')
        await conn.invoke('SubscribeToAll')
      } catch (err) {
        console.error('SignalR connection failed:', err)
        setConnectionState('disconnected')
      }
    }

    start()

    return () => {
      conn.stop()
    }
  }, [])

  return (
    <SignalRContext.Provider value={{ connection: connectionRef.current, connectionState }}>
      {children}
    </SignalRContext.Provider>
  )
}

export default SignalRContext
