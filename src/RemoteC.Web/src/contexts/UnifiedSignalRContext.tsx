import React, { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { config } from '@/config/config'

interface SignalRContextType {
  connection: HubConnection | null
  isConnected: boolean
  sendMessage: (method: string, ...args: any[]) => Promise<void>
  on: (method: string, callback: (...args: any[]) => void) => void
  off: (method: string, callback: (...args: any[]) => void) => void
  onHostHealthUpdate: (callback: (hostId: string, health: any) => void) => void
}

const SignalRContext = createContext<SignalRContextType>({
  connection: null,
  isConnected: false,
  sendMessage: async () => {},
  on: () => {},
  off: () => {},
  onHostHealthUpdate: () => {},
})

export const useUnifiedSignalR = () => useContext(SignalRContext)

interface SignalRProviderProps {
  children: React.ReactNode
}

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:17001'
const useSimpleAuth = import.meta.env.VITE_USE_SIMPLE_AUTH === 'true'
const isDevelopment = import.meta.env.DEV

export const UnifiedSignalRProvider: React.FC<SignalRProviderProps> = ({ children }) => {
  const [connection, setConnection] = useState<HubConnection | null>(null)
  const [isConnected, setIsConnected] = useState(false)
  const healthCallbacksRef = useRef<Set<(hostId: string, health: any) => void>>(new Set())

  useEffect(() => {
    const initializeConnection = async () => {
      try {
        let accessToken = ''
        
        // Get token based on auth mode
        if (useSimpleAuth) {
          // Get token from simple auth storage
          const stored = localStorage.getItem('remotec-simple-auth')
          if (stored) {
            const authData = JSON.parse(stored)
            accessToken = authData.token
          }
        } else if (config.features.useDevAuth) {
          // Dev mode - use a simple token
          accessToken = 'dev-token'
        } else {
          // Would need MSAL auth here for production
          console.warn('SignalR: No auth mode configured')
          return
        }

        // Build connection to host hub for health updates
        const newConnection = new HubConnectionBuilder()
          .withUrl(`${API_BASE_URL}/hubs/host`, {  // Connect to host hub, not session hub
            accessTokenFactory: () => accessToken,
          })
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build()

        // Set up event handlers
        newConnection.onreconnecting(() => {
          console.log('SignalR reconnecting...')
          setIsConnected(false)
        })

        newConnection.onreconnected(() => {
          console.log('SignalR reconnected')
          setIsConnected(true)
        })

        newConnection.onclose(() => {
          console.log('SignalR connection closed')
          setIsConnected(false)
        })

        // Set up host health update handler
        newConnection.on('HostHealthUpdate', (hostId: string, health: any) => {
          console.log('Host health update received:', { hostId, health })
          // Notify all registered callbacks
          healthCallbacksRef.current.forEach(callback => {
            try {
              callback(hostId, health)
            } catch (error) {
              console.error('Error in health update callback:', error)
            }
          })
        })

        // Start connection
        await newConnection.start()
        console.log('SignalR connected to host hub')
        setConnection(newConnection)
        setIsConnected(true)
      } catch (error) {
        console.error('SignalR connection failed:', error)
        // In development, log more details
        if (isDevelopment) {
          console.log('SignalR connection details:', {
            url: `${API_BASE_URL}/hubs/host`,
            authMode: useSimpleAuth ? 'simple' : config.features.useDevAuth ? 'dev' : 'msal'
          })
        }
      }
    }

    initializeConnection()

    return () => {
      if (connection) {
        connection.stop().catch(err => console.error('Error stopping SignalR connection:', err))
      }
    }
  }, []) // Only run once on mount

  const sendMessage = useCallback(async (method: string, ...args: any[]) => {
    if (connection && isConnected) {
      try {
        await connection.invoke(method, ...args)
      } catch (error) {
        console.error(`Error sending SignalR message ${method}:`, error)
      }
    } else {
      console.warn('Cannot send message: SignalR not connected')
    }
  }, [connection, isConnected])

  const on = useCallback((method: string, callback: (...args: any[]) => void) => {
    if (connection) {
      connection.on(method, callback)
    }
  }, [connection])

  const off = useCallback((method: string, callback: (...args: any[]) => void) => {
    if (connection) {
      connection.off(method, callback)
    }
  }, [connection])

  const onHostHealthUpdate = useCallback((callback: (hostId: string, health: any) => void) => {
    healthCallbacksRef.current.add(callback)
    
    // Return cleanup function to remove callback
    return () => {
      healthCallbacksRef.current.delete(callback)
    }
  }, [])

  const value = {
    connection,
    isConnected,
    sendMessage,
    on,
    off,
    onHostHealthUpdate,
  }

  return (
    <SignalRContext.Provider value={value}>
      {children}
    </SignalRContext.Provider>
  )
}