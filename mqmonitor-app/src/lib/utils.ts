import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatDate(date: string | Date | null | undefined): string {
  if (!date) return '—'
  const d = new Date(date)
  return d.toLocaleString('en-US', {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false,
  })
}

export function formatDuration(startDate: string | Date | null | undefined, endDate: string | Date | null | undefined): string {
  if (!startDate) return '—'
  const start = new Date(startDate)
  const end = endDate ? new Date(endDate) : new Date()
  const diffMs = end.getTime() - start.getTime()
  if (diffMs < 1000) return `${diffMs}ms`
  if (diffMs < 60000) return `${(diffMs / 1000).toFixed(1)}s`
  return `${(diffMs / 60000).toFixed(1)}m`
}

export function randomMessage(): string {
  const adjectives = ['Urgent', 'Scheduled', 'Automated', 'Manual', 'Priority', 'Batch', 'Recurring', 'Ad-hoc']
  const nouns = ['report generation', 'data sync', 'payment processing', 'account reconciliation', 'audit check', 'routine maintenance', 'document export', 'notification dispatch']
  const adj = adjectives[Math.floor(Math.random() * adjectives.length)]
  const noun = nouns[Math.floor(Math.random() * nouns.length)]
  return `${adj} ${noun}`
}

export function randomPriority(): number {
  return Math.floor(Math.random() * 10) + 1
}
