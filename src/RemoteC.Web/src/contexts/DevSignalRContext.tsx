import React, { createContext, useContext, useEffect, useState, useCallback } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useDevAuth } from './DevAuthContext'

interface SignalRContextType {
  connection: HubConnection | null
  isConnected: boolean
  sendMessage: (method: string, ...args: any[]) => Promise<void>
  on: (method: string, callback: (...args: any[]) => void) => void
  off: (method: string, callback: (...args: any[]) => void) => void
}

const SignalRContext = createContext<SignalRContextType>({
  connection: null,
  isConnected: false,
  sendMessage: async () => {},
  on: () => {},
  off: () => {},
})

export const useSignalR = () => useContext(SignalRContext)

interface SignalRProviderProps {
  children: React.ReactNode
}

export const DevSignalRProvider: React.FC<SignalRProviderProps> = ({ children }) => {
  const { getAccessToken, isAuthenticated } = useDevAuth()
  const [connection, setConnection] = useState<HubConnection | null>(null)
  const [isConnected, setIsConnected] = useState(false)

  useEffect(() => {
    const initializeConnection = async () => {
      if (!isAuthenticated) return

      try {
        // Get access token
        const token = await getAccessToken()

        // Build connection
        const newConnection = new HubConnectionBuilder()
          .withUrl(`${import.meta.env.VITE_API_URL || 'http://localhost:7001'}/hubs/session`, {
            accessTokenFactory: () => token,
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

        // Start connection
        await newConnection.start()
        console.log('SignalR connected')
        setConnection(newConnection)
        setIsConnected(true)
      } catch (error) {
        console.error('SignalR connection failed:', error)
      }
    }

    initializeConnection()

    // Cleanup
    return () => {
      if (connection) {
        connection.stop()
      }
    }
  }, [isAuthenticated])

  const sendMessage = useCallback(
    async (method: string, ...args: any[]) => {
      if (connection && isConnected) {
        try {
          await connection.invoke(method, ...args)
        } catch (error) {
          console.error(`Error sending message ${method}:`, error)
          throw error
        }
      } else {
        throw new Error('SignalR connection is not available')
      }
    },
    [connection, isConnected]
  )

  const on = useCallback(
    (method: string, callback: (...args: any[]) => void) => {
      if (connection) {
        connection.on(method, callback)
      }
    },
    [connection]
  )

  const off = useCallback(
    (method: string, callback: (...args: any[]) => void) => {
      if (connection) {
        connection.off(method, callback)
      }
    },
    [connection]
  )

  const value: SignalRContextType = {
    connection,
    isConnected,
    sendMessage,
    on,
    off,
  }

  return <SignalRContext.Provider value={value}>{children}</SignalRContext.Provider>
}

// Export alias for compatibility
export const SignalRProvider = DevSignalRProvider