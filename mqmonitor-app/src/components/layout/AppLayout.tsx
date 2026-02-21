import { type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { Radio, Wifi, WifiOff } from 'lucide-react'
import { useSignalR } from '../../hooks/useSignalR'
import { CreateProcessDropdown } from '../process/CreateProcessDropdown'
import { cn } from '../../lib/utils'

interface AppLayoutProps {
  children: ReactNode
}

export function AppLayout({ children }: AppLayoutProps) {
  const { connectionState } = useSignalR()

  return (
    <div className="min-h-screen bg-surface noise-bg">
      {/* Header */}
      <header className="sticky top-0 z-40 border-b border-zinc-800/80 bg-zinc-950/90 backdrop-blur-md">
        <div className="max-w-[1600px] mx-auto px-4 h-14 flex items-center justify-between">
          {/* Logo + Nav */}
          <div className="flex items-center gap-6">
            <Link to="/" className="flex items-center gap-2.5 group">
              <div className="w-8 h-8 rounded-lg bg-cyan-500/10 border border-cyan-500/20 flex items-center justify-center group-hover:bg-cyan-500/20 transition-colors">
                <Radio size={16} className="text-cyan-400" />
              </div>
              <span className="text-base font-semibold font-[family-name:var(--font-display)] text-zinc-100 tracking-tight">
                MQ Monitor
              </span>
            </Link>
          </div>

          {/* Actions */}
          <div className="flex items-center gap-3">
            {/* Connection indicator */}
            <div className={cn(
              'flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium',
              connectionState === 'connected'
                ? 'text-emerald-400 bg-emerald-400/10'
                : connectionState === 'reconnecting'
                ? 'text-amber-400 bg-amber-400/10'
                : 'text-red-400 bg-red-400/10'
            )}>
              {connectionState === 'connected' ? (
                <Wifi size={12} />
              ) : (
                <WifiOff size={12} />
              )}
              {connectionState === 'connected' ? 'Live' : connectionState === 'reconnecting' ? 'Reconnecting' : 'Disconnected'}
            </div>

            {/* Create Process Dropdown */}
            <CreateProcessDropdown />
          </div>
        </div>
      </header>

      {/* Main content */}
      <main className="relative z-10 max-w-[1600px] mx-auto px-4 py-4">
        {children}
      </main>
    </div>
  )
}
