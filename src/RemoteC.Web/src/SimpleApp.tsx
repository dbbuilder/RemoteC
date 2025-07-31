import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Toaster } from 'sonner'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { SimpleAuthProvider, useSimpleAuth } from '@/contexts/SimpleAuthContext'
import { SignalRProvider } from '@/contexts/SignalRContext'
import { Layout } from '@/components/Layout'
import { SimpleLoginPage } from '@/pages/SimpleLoginPage'
import { Dashboard } from '@/pages/Dashboard'
import { DevicesPage } from '@/pages/DevicesPage'
import { SessionsPage } from '@/pages/SessionsPage'
import { SessionDetails } from '@/pages/SessionDetails'
import { UsersPage } from '@/pages/UsersPage'
import { AuditLogsPage } from '@/pages/AuditLogsPage'
import { SettingsPage } from '@/pages/SettingsPage'
import { NotFoundPage } from '@/pages/NotFoundPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 1,
    },
  },
})

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useSimpleAuth()

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

function AppRoutes() {
  const { isAuthenticated } = useSimpleAuth()

  return (
    <Routes>
      <Route path="/login" element={
        isAuthenticated ? <Navigate to="/dashboard" replace /> : <SimpleLoginPage />
      } />
      
      <Route path="/" element={
        <ProtectedRoute>
          <Layout />
        </ProtectedRoute>
      }>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="devices" element={<DevicesPage />} />
        <Route path="sessions" element={<SessionsPage />} />
        <Route path="sessions/:id" element={<SessionDetails />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="audit-logs" element={<AuditLogsPage />} />
        <Route path="settings" element={<SettingsPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}

export function SimpleApp() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ThemeProvider>
          <SimpleAuthProvider>
            <SignalRProvider>
              <AppRoutes />
              <Toaster position="top-right" />
              <ReactQueryDevtools initialIsOpen={false} />
            </SignalRProvider>
          </SimpleAuthProvider>
        </ThemeProvider>
      </BrowserRouter>
    </QueryClientProvider>
  )
}