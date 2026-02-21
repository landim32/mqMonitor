import { LayoutList, Columns3 } from 'lucide-react'
import { cn } from '../../lib/utils'

type ViewMode = 'table' | 'kanban'

interface ViewToggleProps {
  mode: ViewMode
  onChange: (mode: ViewMode) => void
}

export function ViewToggle({ mode, onChange }: ViewToggleProps) {
  return (
    <div className="flex items-center bg-zinc-800/60 border border-zinc-700/50 rounded-lg p-0.5">
      <button
        onClick={() => onChange('table')}
        className={cn(
          'flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium transition-all',
          mode === 'table'
            ? 'bg-zinc-700 text-cyan-400 shadow-sm'
            : 'text-zinc-500 hover:text-zinc-300'
        )}
      >
        <LayoutList size={14} />
        Task Manager
      </button>
      <button
        onClick={() => onChange('kanban')}
        className={cn(
          'flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium transition-all',
          mode === 'kanban'
            ? 'bg-zinc-700 text-cyan-400 shadow-sm'
            : 'text-zinc-500 hover:text-zinc-300'
        )}
      >
        <Columns3 size={14} />
        Kanban
      </button>
    </div>
  )
}
