import { Activity, CheckCircle2, XCircle, Ban, Clock, AlertTriangle } from 'lucide-react'
import { cn } from '../../lib/utils'
import type { ProcessMetricsInfo } from '../../types'

interface MetricsBarProps {
  metrics: ProcessMetricsInfo | null
}

export function MetricsBar({ metrics }: MetricsBarProps) {
  if (!metrics) return null

  const items = [
    { label: 'Total', value: metrics.totalExecuted, icon: Activity, color: 'text-zinc-300' },
    { label: 'Active', value: metrics.inProgress, icon: Clock, color: 'text-cyan-400' },
    { label: 'Finished', value: metrics.finished, icon: CheckCircle2, color: 'text-emerald-400' },
    { label: 'Failed', value: metrics.failed, icon: XCircle, color: 'text-red-400' },
    { label: 'Cancelled', value: metrics.cancelled, icon: Ban, color: 'text-amber-400' },
    { label: 'Error Rate', value: `${(metrics.errorRate * 100).toFixed(1)}%`, icon: AlertTriangle, color: metrics.errorRate > 0.1 ? 'text-red-400' : 'text-zinc-400' },
  ]

  return (
    <div className="flex items-center gap-6 px-4 py-2.5 bg-zinc-900/60 border border-zinc-800/50 rounded-lg backdrop-blur-sm animate-fade-in">
      {items.map((item) => (
        <div key={item.label} className="flex items-center gap-2">
          <item.icon size={15} className={cn('opacity-70', item.color)} />
          <span className="text-xs text-zinc-500 uppercase tracking-wider">{item.label}</span>
          <span className={cn('text-sm font-semibold font-[family-name:var(--font-mono)]', item.color)}>
            {item.value}
          </span>
        </div>
      ))}
    </div>
  )
}
