import React, { createContext, useContext, useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'

interface SimpleUser {
  id: string
  email: string
  displayName: string
  roles: string[]
}

interface SimpleAuthContextType {
  user: SimpleUser | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => void
}

const SimpleAuthContext = createContext<SimpleAuthContextType | undefined>(undefined)

// Hardcoded users for simple auth
const SIMPLE_USERS = [
  { 
    username: 'admin', 
    password: 'admin123', 
    user: {
      id: 'simple-admin',
      email: 'admin@remotec.local',
      displayName: 'Administrator',
      roles: ['Admin', 'Operator', 'Viewer']
    }
  },
  { 
    username: 'operator', 
    password: 'operator123', 
    user: {
      id: 'simple-operator',
      email: 'operator@remotec.local',
      displayName: 'Operator User',
      roles: ['Operator', 'Viewer']
    }
  },
  { 
    username: 'viewer', 
    password: 'viewer123', 
    user: {
      id: 'simple-viewer',
      email: 'viewer@remotec.local',
      displayName: 'Viewer User',
      roles: ['Viewer']
    }
  }
]

export function SimpleAuthProvider({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate()
  const [user, setUser] = useState<SimpleUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  // Check for existing auth on mount
  useEffect(() => {
    const checkAuth = () => {
      try {
        const stored = localStorage.getItem('remotec-simple-auth')
        if (stored) {
          const authData = JSON.parse(stored)
          if (new Date(authData.expiresAt) > new Date()) {
            setUser(authData.user)
            // Set auth header for API calls
            if (axios.defaults) {
              axios.defaults.headers.common['Authorization'] = `Bearer ${authData.token}`
            }
          } else {
            localStorage.removeItem('remotec-simple-auth')
          }
        }
      } catch (e) {
        console.error('Failed to parse stored auth', e)
        localStorage.removeItem('remotec-simple-auth')
      } finally {
        setIsLoading(false)
      }
    }
    
    checkAuth()
  }, [])

  const login = async (username: string, password: string) => {
    const validUser = SIMPLE_USERS.find(
      u => u.username === username && u.password === password
    )
    
    if (!validUser) {
      throw new Error('Invalid username or password')
    }
    
    // Create auth data
    const authData = {
      user: validUser.user,
      token: btoa(`${username}:${Date.now()}`), // Simple token
      expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString() // 24 hours
    }
    
    // Store auth
    localStorage.setItem('remotec-simple-auth', JSON.stringify(authData))
    
    // Set auth header
    if (axios.defaults) {
      axios.defaults.headers.common['Authorization'] = `Bearer ${authData.token}`
    }
    
    setUser(validUser.user)
    navigate('/dashboard')
  }

  const logout = () => {
    localStorage.removeItem('remotec-simple-auth')
    if (axios.defaults) {
      delete axios.defaults.headers.common['Authorization']
    }
    setUser(null)
    navigate('/login')
  }

  return (
    <SimpleAuthContext.Provider 
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout
      }}
    >
      {children}
    </SimpleAuthContext.Provider>
  )
}

export function useSimpleAuth() {
  const context = useContext(SimpleAuthContext)
  if (context === undefined) {
    throw new Error('useSimpleAuth must be used within a SimpleAuthProvider')
  }
  return context
}