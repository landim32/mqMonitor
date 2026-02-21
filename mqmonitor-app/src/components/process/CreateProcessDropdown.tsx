import { useState, useRef, useCallback, useEffect } from 'react'
import {
  ChevronDown, FileText, Wallet, RefreshCw, CreditCard,
  Bell, Shield, Send,
} from 'lucide-react'
import { toast } from 'sonner'
import { useProcess } from '../../hooks/useProcess'
import { STAGES } from '../../lib/constants'
import { cn, randomMessage, randomPriority } from '../../lib/utils'

const ICON_MAP: Record<string, React.ElementType> = {
  FileText, Wallet, RefreshCw, CreditCard, Bell, Shield,
}

export function CreateProcessDropdown() {
  const { createProcess } = useProcess()
  const [open, setOpen] = useState(false)
  const [count, setCount] = useState(1)
  const [loading, setLoading] = useState<string | null>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  // Close on outside click
  useEffect(() => {
    if (!open) return

    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [open])

  // Close on Escape
  useEffect(() => {
    if (!open) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [open])

  const handleToggle = useCallback(() => {
    setOpen(prev => !prev)
  }, [])

  const handleCountChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const val = Math.max(1, Math.min(100, Number(e.target.value) || 1))
    setCount(val)
  }, [])

  const handleSend = useCallback(async (stageName: string, displayName: string) => {
    setLoading(stageName)
    try {
      for (let i = 0; i < count; i++) {
        await createProcess({
          stageName,
          message: randomMessage(),
          priority: randomPriority(),
        })
      }
      toast.success(`${count} process${count > 1 ? 'es' : ''} sent to ${displayName}`)
      setOpen(false)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create process')
    } finally {
      setLoading(null)
    }
  }, [count, createProcess])

  return (
    <div ref={containerRef} className="relative">
      {/* Trigger button */}
      <button
        onClick={handleToggle}
        className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-sm font-medium bg-cyan-600 hover:bg-cyan-500 text-white transition-colors shadow-sm shadow-cyan-600/20"
      >
        Create Process
        <ChevronDown size={14} className={cn('transition-transform', open && 'rotate-180')} />
      </button>

      {/* Dropdown panel */}
      {open && (
        <div className="absolute right-0 top-full mt-2 w-72 rounded-lg border border-zinc-700/50 bg-zinc-900 shadow-lg z-50">
          {/* Header */}
          <div className="flex items-center justify-between px-3 py-2.5 border-b border-zinc-800">
            <span className="text-xs font-medium text-zinc-400 uppercase tracking-wide">
              Send to Queue
            </span>
            <div className="flex items-center gap-1.5">
              <label htmlFor="process-count" className="text-xs text-zinc-500">
                Count
              </label>
              <input
                id="process-count"
                type="number"
                min={1}
                max={100}
                value={count}
                onChange={handleCountChange}
                className="w-14 h-7 rounded-md border border-zinc-700 bg-zinc-800 text-xs text-zinc-200 text-center focus:outline-none focus:border-cyan-500/50"
              />
            </div>
          </div>

          {/* Stage list */}
          <div className="py-1">
            {STAGES.map((stage) => {
              const Icon = ICON_MAP[stage.icon] || FileText
              const isLoading = loading === stage.name

              return (
                <div
                  key={stage.name}
                  className="flex items-center gap-3 px-3 py-2 hover:bg-zinc-800/60 transition-colors"
                >
                  <div
                    className="w-7 h-7 rounded-md flex items-center justify-center flex-shrink-0"
                    style={{ backgroundColor: `${stage.color}15`, color: stage.color }}
                  >
                    <Icon size={14} />
                  </div>
                  <span className="text-sm text-zinc-300 flex-1">{stage.displayName}</span>
                  <button
                    onClick={() => handleSend(stage.name, stage.displayName)}
                    disabled={loading !== null}
                    className={cn(
                      'flex items-center gap-1 px-2 py-1 rounded-md text-xs font-medium transition-colors',
                      'text-cyan-400 hover:bg-cyan-500/10 hover:text-cyan-300',
                      'disabled:opacity-40 disabled:cursor-not-allowed',
                      isLoading && 'text-cyan-300 bg-cyan-500/10'
                    )}
                  >
                    <Send size={11} />
                    {isLoading ? 'Sending...' : 'Send'}
                  </button>
                </div>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
