import { useState, useMemo } from 'react'
import { ArrowUpDown, Filter } from 'lucide-react'
import { ProcessRow } from './ProcessRow'
import { useProcess } from '../../hooks/useProcess'
import { STAGES, STATUS_CONFIG } from '../../lib/constants'
import { cn } from '../../lib/utils'
import type { ProcessExecutionInfo } from '../../types'

type SortField = 'processId' | 'status' | 'currentStage' | 'priority' | 'updatedAt'
type SortDir = 'asc' | 'desc'

interface ProcessTableProps {
  onSelectProcess: (process: ProcessExecutionInfo) => void
}

export function ProcessTable({ onSelectProcess }: ProcessTableProps) {
  const { processes } = useProcess()
  const [sortField, setSortField] = useState<SortField>('updatedAt')
  const [sortDir, setSortDir] = useState<SortDir>('desc')
  const [stageFilter, setStageFilter] = useState<string>('')
  const [statusFilter, setStatusFilter] = useState<string>('')

  const filtered = useMemo(() => {
    let result = [...processes]
    if (stageFilter) result = result.filter(p => p.currentStage === stageFilter)
    if (statusFilter) result = result.filter(p => p.status === statusFilter)
    return result
  }, [processes, stageFilter, statusFilter])

  const sorted = useMemo(() => {
    return [...filtered].sort((a, b) => {
      const dir = sortDir === 'asc' ? 1 : -1
      switch (sortField) {
        case 'processId': return dir * a.processId.localeCompare(b.processId)
        case 'status': return dir * a.status.localeCompare(b.status)
        case 'currentStage': return dir * (a.currentStage || '').localeCompare(b.currentStage || '')
        case 'priority': return dir * (a.priority - b.priority)
        case 'updatedAt': return dir * (new Date(a.updatedAt).getTime() - new Date(b.updatedAt).getTime())
        default: return 0
      }
    })
  }, [filtered, sortField, sortDir])

  const toggleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDir('desc')
    }
  }

  const columns: { field: SortField; label: string; width: string }[] = [
    { field: 'processId', label: 'Process ID', width: 'w-[160px]' },
    { field: 'status', label: 'Status', width: 'w-[130px]' },
    { field: 'currentStage', label: 'Stage', width: 'w-[120px]' },
    { field: 'priority', label: 'Priority', width: 'w-[100px]' },
  ]

  return (
    <div className="animate-fade-in">
      {/* Filters */}
      <div className="flex items-center gap-3 mb-3">
        <Filter size={14} className="text-zinc-500" />
        <select
          value={stageFilter}
          onChange={e => setStageFilter(e.target.value)}
          className="bg-zinc-800/60 border border-zinc-700/50 rounded-md px-2.5 py-1.5 text-xs text-zinc-300 focus:outline-none focus:border-cyan-500/50"
        >
          <option value="">All Stages</option>
          {STAGES.map(s => (
            <option key={s.name} value={s.name}>{s.displayName}</option>
          ))}
        </select>
        <select
          value={statusFilter}
          onChange={e => setStatusFilter(e.target.value)}
          className="bg-zinc-800/60 border border-zinc-700/50 rounded-md px-2.5 py-1.5 text-xs text-zinc-300 focus:outline-none focus:border-cyan-500/50"
        >
          <option value="">All Statuses</option>
          {Object.entries(STATUS_CONFIG).map(([key, val]) => (
            <option key={key} value={key}>{val.label}</option>
          ))}
        </select>
        <span className="text-xs text-zinc-500 ml-auto">
          {sorted.length} process{sorted.length !== 1 ? 'es' : ''}
        </span>
      </div>

      {/* Table */}
      <div className="border border-zinc-800/50 rounded-lg overflow-hidden bg-zinc-900/40">
        {/* Header */}
        <div className="flex items-center px-3 py-2 bg-zinc-800/40 border-b border-zinc-800/50 text-xs font-medium text-zinc-500 uppercase tracking-wider">
          {columns.map(col => (
            <button
              key={col.field}
              onClick={() => toggleSort(col.field)}
              className={cn(
                'flex items-center gap-1 hover:text-zinc-300 transition-colors',
                col.width,
                sortField === col.field && 'text-cyan-400'
              )}
            >
              {col.label}
              <ArrowUpDown size={11} className="opacity-50" />
            </button>
          ))}
          <span className="flex-1">Message</span>
          <span className="w-[160px]">Updated</span>
          <span className="w-[80px] text-right">Actions</span>
        </div>

        {/* Rows */}
        <div className="divide-y divide-zinc-800/30">
          {sorted.length === 0 ? (
            <div className="px-3 py-12 text-center text-sm text-zinc-600">
              No processes found
            </div>
          ) : (
            sorted.map((process, i) => (
              <ProcessRow
                key={process.processId}
                process={process}
                onClick={() => onSelectProcess(process)}
                style={{ animationDelay: `${i * 0.02}s` }}
              />
            ))
          )}
        </div>
      </div>
    </div>
  )
}
