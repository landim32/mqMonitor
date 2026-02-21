import type { PipelineStatusInfo } from '../types'

const API_BASE = '/api/queues'

class QueueService {
  private async handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || `Request failed with status ${response.status}`)
    }
    return response.json()
  }

  async getPipelineStatus(): Promise<PipelineStatusInfo> {
    const response = await fetch(`${API_BASE}/pipeline`)
    return this.handleResponse<PipelineStatusInfo>(response)
  }
}

export const queueService = new QueueService()
export default QueueService
