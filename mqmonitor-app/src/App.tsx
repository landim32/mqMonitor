import { Routes, Route } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AppLayout } from './components/layout/AppLayout'
import { DashboardPage } from './pages/DashboardPage'
import { ProcessPage } from './pages/ProcessPage'

function App() {
  return (
    <>
      <AppLayout>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/processes/:id" element={<ProcessPage />} />
        </Routes>
      </AppLayout>
      <Toaster
        position="bottom-right"
        richColors
        toastOptions={{
          style: {
            background: '#18181b',
            border: '1px solid #3f3f46',
            color: '#fafafa',
          },
        }}
      />
    </>
  )
}

export default App
