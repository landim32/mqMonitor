import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { SignalRProvider } from './contexts/SignalRContext'
import { QueueProvider } from './contexts/QueueContext'
import { ProcessProvider } from './contexts/ProcessContext'
import './index.css'
import App from './App'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <SignalRProvider>
        <QueueProvider>
          <ProcessProvider>
            <App />
          </ProcessProvider>
        </QueueProvider>
      </SignalRProvider>
    </BrowserRouter>
  </StrictMode>,
)
