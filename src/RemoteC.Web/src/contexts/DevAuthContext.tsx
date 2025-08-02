import React, { createContext, useContext, useState } from 'react'
import { User } from '@/types'

interface DevAuthContextType {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
  getAccessToken: () => Promise<string>
}

const DevAuthContext = createContext<DevAuthContextType | undefined>(undefined)

export const useDevAuth = () => {
  const context = useContext(DevAuthContext)
  if (!context) {
    throw new Error('useDevAuth must be used within a DevAuthProvider')
  }
  return context
}

export const DevAuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isLoading, setIsLoading] = useState(true)

  const login = async (username: string, password: string) => {
    try {
      // Call the actual dev-login endpoint
      const response = await fetch(`${import.meta.env.VITE_API_URL || 'http://localhost:7001'}/api/auth/dev-login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ 
          email: username.includes('@') ? username : `${username}@remotec.demo`,
          password: password || 'Admin@123'
        }),
      })

      if (!response.ok) {
        throw new Error('Login failed')
      }

      const data = await response.json()
      
      const user: User = {
        id: data.user.id,
        email: data.user.email,
        displayName: data.user.name,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        lastLoginAt: new Date().toISOString(),
        isActive: true,
        roles: data.user.roles || ['Admin'],
        permissions: ['view_all', 'manage_all'], // Full permissions in dev
      }
      
      setUser(user)
      setIsAuthenticated(true)
      
      // Store in localStorage for persistence
      localStorage.setItem('dev-auth', JSON.stringify({ user, token: data.token }))
    } catch (error) {
      console.error('Login failed:', error)
      throw error
    }
  }

  const logout = async () => {
    setUser(null)
    setIsAuthenticated(false)
    localStorage.removeItem('dev-auth')
  }

  const getAccessToken = async (): Promise<string> => {
    // Get the actual token from localStorage
    const storedAuth = localStorage.getItem('dev-auth')
    if (storedAuth) {
      const { token } = JSON.parse(storedAuth)
      return token || 'dev-token-123'
    }
    return 'dev-token-123'
  }

  // Check for existing auth on mount
  React.useEffect(() => {
    const checkAuth = () => {
      try {
        const storedAuth = localStorage.getItem('dev-auth')
        if (storedAuth) {
          const { user: storedUser, token } = JSON.parse(storedAuth)
          // Verify we have both user and token
          if (storedUser && token) {
            setUser(storedUser)
            setIsAuthenticated(true)
          }
        }
      } catch (error) {
        console.error('Error loading auth state:', error)
        // Clear invalid auth data
        localStorage.removeItem('dev-auth')
      } finally {
        setIsLoading(false)
      }
    }
    
    checkAuth()
  }, [])

  const value: DevAuthContextType = {
    user,
    isAuthenticated,
    isLoading,
    login,
    logout,
    getAccessToken,
  }

  return <DevAuthContext.Provider value={value}>{children}</DevAuthContext.Provider>
}