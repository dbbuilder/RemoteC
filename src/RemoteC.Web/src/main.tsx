import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { PublicClientApplication } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import App from './App'
import DevApp from './DevApp'
import { msalConfig } from './config/authConfig'
import { config } from './config/config'
import './index.css'

// Create MSAL instance
const msalInstance = new PublicClientApplication(msalConfig)

// Create React Query client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 3,
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
    },
  },
})

// Choose the appropriate app based on environment
const AppComponent = config.features.useDevAuth ? DevApp : App

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    {config.features.useDevAuth ? (
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <DevApp />
        </BrowserRouter>
      </QueryClientProvider>
    ) : (
      <MsalProvider instance={msalInstance}>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </QueryClientProvider>
      </MsalProvider>
    )}
  </React.StrictMode>,
)