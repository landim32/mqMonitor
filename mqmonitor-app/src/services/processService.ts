import type {
  ProcessExecutionInfo, CreateProcessRequest, CreateProcessResponse,
  SagaStepInfo, EventLogInfo, ProcessMetricsInfo,
} from '../types'

const API_BASE = '/api/processes'

class ProcessService {
  private async handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || `Request failed with status ${response.status}`)
    }
    return response.json()
  }

  async list(stage?: string, status?: string): Promise<ProcessExecutionInfo[]> {
    const params = new URLSearchParams()
    if (stage) params.set('stage', stage)
    if (status) params.set('status', status)
    const query = params.toString()
    const response = await fetch(`${API_BASE}${query ? `?${query}` : ''}`)
    return this.handleResponse<ProcessExecutionInfo[]>(response)
  }

  async getById(processId: string): Promise<ProcessExecutionInfo> {
    const response = await fetch(`${API_BASE}/${processId}`)
    return this.handleResponse<ProcessExecutionInfo>(response)
  }

  async create(request: CreateProcessRequest): Promise<CreateProcessResponse> {
    const response = await fetch(API_BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    })
    return this.handleResponse<CreateProcessResponse>(response)
  }

  async getEvents(processId: string): Promise<EventLogInfo[]> {
    const response = await fetch(`${API_BASE}/${processId}/events`)
    return this.handleResponse<EventLogInfo[]>(response)
  }

  async getSagaSteps(processId: string): Promise<SagaStepInfo[]> {
    const response = await fetch(`${API_BASE}/${processId}/saga`)
    return this.handleResponse<SagaStepInfo[]>(response)
  }

  async updatePriority(processId: string, priority: number): Promise<void> {
    const response = await fetch(`${API_BASE}/${processId}/priority`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ priority }),
    })
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to update priority')
    }
  }

  async cancel(processId: string): Promise<void> {
    const response = await fetch(`${API_BASE}/${processId}/cancel`, {
      method: 'POST',
    })
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to cancel process')
    }
  }

  async getMetrics(): Promise<ProcessMetricsInfo> {
    const response = await fetch(`${API_BASE}/metrics`)
    return this.handleResponse<ProcessMetricsInfo>(response)
  }
}

export const processService = new ProcessService()
export default ProcessService
