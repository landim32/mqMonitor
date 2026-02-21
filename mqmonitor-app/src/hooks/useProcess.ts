import { useContext } from 'react'
import ProcessContext from '../contexts/ProcessContext'

export const useProcess = () => {
  const context = useContext(ProcessContext)
  if (!context) throw new Error('useProcess must be used within a ProcessProvider')
  return context
}

export default useProcess
