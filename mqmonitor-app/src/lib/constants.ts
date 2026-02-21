export interface StageInfo {
  name: string
  displayName: string
  icon: string
  color: string
}

export const STAGES: StageInfo[] = [
  { name: 'report',       displayName: 'Report',       icon: 'FileText',   color: '#06b6d4' },
  { name: 'account',      displayName: 'Account',      icon: 'Wallet',     color: '#8b5cf6' },
  { name: 'routine',      displayName: 'Routine',      icon: 'RefreshCw',  color: '#f59e0b' },
  { name: 'payment',      displayName: 'Payment',      icon: 'CreditCard', color: '#ec4899' },
  { name: 'notification', displayName: 'Notification', icon: 'Bell',       color: '#f97316' },
  { name: 'audit',        displayName: 'Audit',        icon: 'Shield',     color: '#6366f1' },
]

export const STATUS_CONFIG: Record<string, { label: string; color: string; bgColor: string; pulse?: boolean }> = {
  CREATED: { label: 'Created', color: '#a78bfa', bgColor: 'rgba(167, 139, 250, 0.15)' },
  QUEUED: { label: 'Queued', color: '#fbbf24', bgColor: 'rgba(251, 191, 36, 0.15)' },
  STARTED: { label: 'Started', color: '#22d3ee', bgColor: 'rgba(34, 211, 238, 0.15)', pulse: true },
  STAGE_STARTED: { label: 'Processing', color: '#06b6d4', bgColor: 'rgba(6, 182, 212, 0.15)', pulse: true },
  STAGE_COMPLETED: { label: 'Stage Done', color: '#2dd4bf', bgColor: 'rgba(45, 212, 191, 0.15)' },
  FINISHED: { label: 'Finished', color: '#34d399', bgColor: 'rgba(52, 211, 153, 0.15)' },
  FAILED: { label: 'Failed', color: '#f87171', bgColor: 'rgba(248, 113, 113, 0.15)' },
  CANCELLED: { label: 'Cancelled', color: '#fb923c', bgColor: 'rgba(251, 146, 60, 0.15)' },
  CANCEL_REQUESTED: { label: 'Cancelling', color: '#fdba74', bgColor: 'rgba(253, 186, 116, 0.15)', pulse: true },
  COMPENSATING: { label: 'Compensating', color: '#c084fc', bgColor: 'rgba(192, 132, 252, 0.15)', pulse: true },
  COMPENSATED: { label: 'Compensated', color: '#a855f7', bgColor: 'rgba(168, 85, 247, 0.15)' },
}

export const TERMINAL_STATUSES = ['FINISHED', 'FAILED', 'CANCELLED', 'COMPENSATED']

export function isTerminalStatus(status: string): boolean {
  return TERMINAL_STATUSES.includes(status)
}

export function getStageByName(name: string): StageInfo | undefined {
  return STAGES.find(s => s.name.toLowerCase() === name.toLowerCase())
}
