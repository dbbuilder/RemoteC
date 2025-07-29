import React, { useCallback, useEffect, useState } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { Box, CircularProgress, Alert } from '@mui/material';

import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { AuthProvider } from './contexts/AuthContext';
import { SignalRProvider } from './contexts/SignalRContext';
import { Layout } from './components/Layout/Layout';
import { LoginPage } from './pages/LoginPage';
import { Dashboard } from './pages/Dashboard';
import { SessionsPage } from './pages/SessionsPage';
import { SessionDetails } from './pages/SessionDetails';
import { DevicesPage } from './pages/DevicesPage';
import { UsersPage } from './pages/UsersPage';
import { SettingsPage } from './pages/SettingsPage';
import { AuditLogsPage } from './pages/AuditLogsPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { ErrorBoundary } from './components/ErrorBoundary/ErrorBoundary';

function App() {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const handleLogin = useCallback(async () => {
    try {
      setIsLoading(true);
      await instance.loginRedirect({
        scopes: ['openid', 'profile', 'email'],
      });
    } catch (error) {
      console.error('Login failed:', error);
      setError('Login failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, [instance]);

  const handleLogout = useCallback(async () => {
    try {
      await instance.logoutRedirect();
    } catch (error) {
      console.error('Logout failed:', error);
    }
  }, [instance]);

  useEffect(() => {
    const initializeAuth = async () => {
      try {
        await instance.initialize();
        
        // Handle redirect promise
        const response = await instance.handleRedirectPromise();
        if (response) {
          console.log('Authentication successful:', response);
        }
      } catch (error) {
        console.error('Authentication initialization failed:', error);
        setError('Authentication failed. Please refresh the page.');
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();
  }, [instance]);

  if (isLoading) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
      >
        <CircularProgress size={60} />
      </Box>
    );
  }

  if (error) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
        p={3}
      >
        <Alert severity="error" sx={{ maxWidth: 400 }}>
          {error}
        </Alert>
      </Box>
    );
  }

  return (
    <ErrorBoundary>
      <AuthProvider>
        <AuthenticatedTemplate>
          <SignalRProvider>
            <Layout onLogout={handleLogout}>
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
          <LoginPage onLogin={handleLogin} isLoading={isLoading} />
        </UnauthenticatedTemplate>
      </AuthProvider>
    </ErrorBoundary>
  );
}

export default App;