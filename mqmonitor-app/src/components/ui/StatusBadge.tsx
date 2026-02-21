import { cn } from '../../lib/utils'
import { STATUS_CONFIG } from '../../lib/constants'

interface StatusBadgeProps {
  status: string
  className?: string
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const config = STATUS_CONFIG[status] || { label: status, color: '#71717a', bgColor: 'rgba(113, 113, 122, 0.15)' }

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium font-[family-name:var(--font-mono)] tracking-tight',
        config.pulse && 'animate-pulse-cyan',
        className
      )}
      style={{ color: config.color, backgroundColor: config.bgColor }}
    >
      {config.pulse && (
        <span
          className="w-1.5 h-1.5 rounded-full"
          style={{ backgroundColor: config.color }}
        />
      )}
      {config.label}
    </span>
  )
}
