import { ChevronUp, ChevronDown } from 'lucide-react'
import { cn } from '../../lib/utils'

interface PriorityControlProps {
  priority: number
  onIncrease: () => void
  onDecrease: () => void
  disabled?: boolean
  compact?: boolean
}

export function PriorityControl({ priority, onIncrease, onDecrease, disabled = false, compact = false }: PriorityControlProps) {
  return (
    <div className={cn('flex items-center gap-1', compact ? 'gap-0.5' : 'gap-1')}>
      <button
        onClick={(e) => { e.stopPropagation(); onIncrease() }}
        disabled={disabled || priority >= 10}
        className={cn(
          'p-0.5 rounded transition-all hover:scale-110 active:scale-95',
          'text-zinc-500 hover:text-cyan-400 hover:bg-cyan-400/10',
          'disabled:opacity-30 disabled:hover:scale-100 disabled:hover:text-zinc-500 disabled:hover:bg-transparent',
        )}
        title="Increase priority"
      >
        <ChevronUp size={compact ? 14 : 16} />
      </button>
      <span className={cn(
        'font-[family-name:var(--font-mono)] font-semibold min-w-[1.5rem] text-center',
        compact ? 'text-xs' : 'text-sm',
        priority >= 7 ? 'text-amber-400' : priority >= 4 ? 'text-cyan-400' : 'text-zinc-400'
      )}>
        {priority}
      </span>
      <button
        onClick={(e) => { e.stopPropagation(); onDecrease() }}
        disabled={disabled || priority <= 0}
        className={cn(
          'p-0.5 rounded transition-all hover:scale-110 active:scale-95',
          'text-zinc-500 hover:text-cyan-400 hover:bg-cyan-400/10',
          'disabled:opacity-30 disabled:hover:scale-100 disabled:hover:text-zinc-500 disabled:hover:bg-transparent',
        )}
        title="Decrease priority"
      >
        <ChevronDown size={compact ? 14 : 16} />
      </button>
    </div>
  )
}
