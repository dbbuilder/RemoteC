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
  const [isLoading] = useState(false)

  const login = async (username: string, password: string) => {
    // In development mode, any username/password works
    const mockUser: User = {
      id: 'dev-user-001',
      email: `${username}@dev.local`,
      displayName: username,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      lastLoginAt: new Date().toISOString(),
      isActive: true,
      roles: ['Admin'], // Give admin role in dev mode
      permissions: ['view_all', 'manage_all'], // Full permissions in dev
    }

    // Simulate async API call
    await new Promise(resolve => setTimeout(resolve, 500))
    
    setUser(mockUser)
    setIsAuthenticated(true)
    
    // Store in localStorage for persistence
    localStorage.setItem('dev-auth', JSON.stringify({ user: mockUser, token: 'dev-token-123' }))
  }

  const logout = async () => {
    setUser(null)
    setIsAuthenticated(false)
    localStorage.removeItem('dev-auth')
  }

  const getAccessToken = async (): Promise<string> => {
    // Return a mock token for development
    return 'dev-token-123'
  }

  // Check for existing auth on mount
  React.useEffect(() => {
    const storedAuth = localStorage.getItem('dev-auth')
    if (storedAuth) {
      const { user: storedUser } = JSON.parse(storedAuth)
      setUser(storedUser)
      setIsAuthenticated(true)
    }
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