import { useContext } from 'react'
import QueueContext from '../contexts/QueueContext'

export const useQueue = () => {
  const context = useContext(QueueContext)
  if (!context) throw new Error('useQueue must be used within a QueueProvider')
  return context
}

export default useQueue
