import { useEffect, useState } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { useMsal } from '@azure/msal-react'
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { Toaster } from 'sonner'
import { AuthProvider } from '@/contexts/AuthContext'
import { SignalRProvider } from '@/contexts/SignalRContext'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { Layout } from '@/components/Layout'
import { LoginPage } from '@/pages/LoginPage'
import { Dashboard } from '@/pages/Dashboard'
import { SessionsPage } from '@/pages/SessionsPage'
import { SessionDetails } from '@/pages/SessionDetails'
import { DevicesPage } from '@/pages/DevicesPage'
import { UsersPage } from '@/pages/UsersPage'
import { SettingsPage } from '@/pages/SettingsPage'
import { AuditLogsPage } from '@/pages/AuditLogsPage'
import { NotFoundPage } from '@/pages/NotFoundPage'

function App() {
  const { instance } = useMsal()
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const initializeAuth = async () => {
      try {
        await instance.initialize()
        
        // Handle redirect promise
        const response = await instance.handleRedirectPromise()
        if (response) {
          console.log('Authentication successful:', response)
        }
      } catch (error) {
        console.error('Authentication initialization failed:', error)
      } finally {
        setIsLoading(false)
      }
    }

    initializeAuth()
  }, [instance])

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    )
  }

  return (
    <ThemeProvider defaultTheme="dark" storageKey="remotec-theme">
      <AuthProvider>
        <AuthenticatedTemplate>
          <SignalRProvider>
            <Layout>
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
            </Layout>
          </SignalRProvider>
        </AuthenticatedTemplate>

        <UnauthenticatedTemplate>
          <LoginPage />
        </UnauthenticatedTemplate>

        <Toaster position="top-right" />
      </AuthProvider>
    </ThemeProvider>
  )
}

export default App