import { Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'sonner'
import { DevAuthProvider, useDevAuth } from '@/contexts/DevAuthContext'
import { DevSignalRProvider } from '@/contexts/DevSignalRContext'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { DevLayout } from '@/components/DevLayout'
import { DevLoginPage } from '@/pages/DevLoginPage'
import { Dashboard } from '@/pages/Dashboard'
import { SessionsPage } from '@/pages/SessionsPage'
import { SessionDetails } from '@/pages/SessionDetails'
import { DevicesPage } from '@/pages/DevicesPage'
import { UsersPage } from '@/pages/UsersPage'
import { SettingsPage } from '@/pages/SettingsPage'
import { AuditLogsPage } from '@/pages/AuditLogsPage'
import { NotFoundPage } from '@/pages/NotFoundPage'

function DevAppRoutes() {
  const { isAuthenticated, isLoading } = useDevAuth()

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return <DevLoginPage />
  }

  return (
    <DevSignalRProvider>
      <DevLayout>
        <Routes>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/sessions" element={<SessionsPage />} />
          <Route path="/sessions/:id" element={<SessionDetails />} />
          <Route path="/devices" element={<DevicesPage />} />
          <Route path="/users" element={<UsersPage />} />
          <Route path="/audit" element={<AuditLogsPage />} />
          <Route path="/settings" element={<SettingsPage />} />
          <Route path="/404" element={<NotFoundPage />} />
          <Route path="*" element={<Navigate to="/404" replace />} />
        </Routes>
      </DevLayout>
    </DevSignalRProvider>
  )
}

function DevApp() {
  return (
    <ThemeProvider defaultTheme="dark" storageKey="remotec-theme">
      <DevAuthProvider>
        <DevAppRoutes />
        <Toaster position="top-right" />
      </DevAuthProvider>
    </ThemeProvider>
  )
}

export default DevApp