import { useState } from 'react'
import { ChevronRight, ChevronDown } from 'lucide-react'
import { formatDate } from '../../lib/utils'
import type { EventLogInfo } from '../../types'

interface EventHistoryProps {
  events: EventLogInfo[]
}

export function EventHistory({ events }: EventHistoryProps) {
  if (events.length === 0) {
    return (
      <div className="text-center text-sm text-zinc-600 py-6">
        No events recorded
      </div>
    )
  }

  const sorted = [...events].sort(
    (a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
  )

  return (
    <div className="border border-zinc-800/50 rounded-lg overflow-hidden">
      {/* Header */}
      <div className="flex items-center px-3 py-2 bg-zinc-800/40 border-b border-zinc-800/50 text-xs font-medium text-zinc-500 uppercase tracking-wider">
        <span className="w-8" />
        <span className="w-[160px]">Timestamp</span>
        <span className="w-[200px]">Type</span>
        <span className="flex-1">Event ID</span>
      </div>

      {/* Rows */}
      <div className="divide-y divide-zinc-800/30">
        {sorted.map((event, i) => (
          <EventRow key={event.eventId} event={event} index={i} />
        ))}
      </div>
    </div>
  )
}

function EventRow({ event, index }: { event: EventLogInfo; index: number }) {
  const [expanded, setExpanded] = useState(false)

  let parsedPayload: string | null = null
  try {
    const obj = JSON.parse(event.payload)
    parsedPayload = JSON.stringify(obj, null, 2)
  } catch {
    parsedPayload = event.payload
  }

  return (
    <div
      className="animate-stagger"
      style={{ animationDelay: `${index * 0.02}s` }}
    >
      <div
        onClick={() => setExpanded(!expanded)}
        className="flex items-center px-3 py-2 hover:bg-zinc-800/30 cursor-pointer transition-colors"
      >
        <span className="w-8 text-zinc-600">
          {expanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
        </span>
        <span className="w-[160px] text-xs text-zinc-500 font-[family-name:var(--font-mono)]">
          {formatDate(event.timestamp)}
        </span>
        <span className="w-[200px] text-xs text-cyan-400/70 font-[family-name:var(--font-mono)]">
          {event.type}
        </span>
        <span className="flex-1 text-xs text-zinc-600 font-[family-name:var(--font-mono)] truncate">
          {event.eventId}
        </span>
      </div>

      {expanded && parsedPayload && (
        <div className="px-3 pb-3 pl-11">
          <pre className="text-[11px] text-zinc-500 font-[family-name:var(--font-mono)] bg-zinc-900/60 border border-zinc-800/40 rounded-lg p-3 overflow-x-auto max-h-[300px] overflow-y-auto">
            {parsedPayload}
          </pre>
        </div>
      )}
    </div>
  )
}
