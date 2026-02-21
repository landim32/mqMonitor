import { MessageSquare, Users } from 'lucide-react'
import { KanbanCard } from './KanbanCard'
import type { ProcessExecutionInfo } from '../../types'

interface KanbanColumnProps {
  title: string
  color: string
  processes: ProcessExecutionInfo[]
  messageCount: number
  consumerCount: number
  onSelectProcess: (process: ProcessExecutionInfo) => void
}

export function KanbanColumn({
  title, color, processes, messageCount, consumerCount, onSelectProcess,
}: KanbanColumnProps) {
  return (
    <div className="flex-shrink-0 w-[280px] flex flex-col bg-zinc-900/30 border border-zinc-800/40 rounded-xl overflow-hidden">
      {/* Header */}
      <div
        className="px-3 py-2.5 border-b border-zinc-800/40"
        style={{ background: `linear-gradient(135deg, ${color}08, ${color}03)` }}
      >
        <div className="flex items-center justify-between mb-1">
          <div className="flex items-center gap-2">
            <span
              className="w-2.5 h-2.5 rounded-full"
              style={{ backgroundColor: color }}
            />
            <span className="text-sm font-semibold text-zinc-200">{title}</span>
          </div>
          <span className="text-xs font-[family-name:var(--font-mono)] text-zinc-500 bg-zinc-800/60 px-1.5 py-0.5 rounded">
            {processes.length}
          </span>
        </div>
        {(messageCount > 0 || consumerCount > 0) && (
          <div className="flex items-center gap-3 text-[10px] text-zinc-600">
            <span className="flex items-center gap-1">
              <MessageSquare size={10} /> {messageCount} queued
            </span>
            <span className="flex items-center gap-1">
              <Users size={10} /> {consumerCount} consumers
            </span>
          </div>
        )}
      </div>

      {/* Cards */}
      <div className="flex-1 overflow-y-auto p-2 space-y-2">
        {processes.length === 0 ? (
          <div className="text-center text-xs text-zinc-700 py-8">
            No processes
          </div>
        ) : (
          processes.map((process, i) => (
            <KanbanCard
              key={process.processId}
              process={process}
              onClick={() => onSelectProcess(process)}
              staggerIndex={i}
            />
          ))
        )}
      </div>
    </div>
  )
}
