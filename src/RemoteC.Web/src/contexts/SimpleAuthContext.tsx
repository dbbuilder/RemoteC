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

// Simple JWT token generator for development
function generateDevToken(user: SimpleUser): string {
  const header = {
    alg: 'HS256',
    typ: 'JWT'
  }
  
  const payload = {
    sub: user.id,
    email: user.email,
    name: user.displayName,
    roles: user.roles,
    exp: Math.floor(Date.now() / 1000) + (60 * 60 * 24), // 24 hours
    iat: Math.floor(Date.now() / 1000),
    iss: 'remotec-dev',
    aud: 'remotec-api'
  }
  
  // Simple base64 encoding (not secure, for development only)
  const base64Header = btoa(JSON.stringify(header))
  const base64Payload = btoa(JSON.stringify(payload))
  const signature = btoa('development-signature') // Fake signature for dev
  
  return `${base64Header}.${base64Payload}.${signature}`
}

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
    // Find user in hardcoded list
    const foundUser = SIMPLE_USERS.find(u => 
      u.username.toLowerCase() === username.toLowerCase() && 
      u.password === password
    )

    if (!foundUser) {
      throw new Error('Invalid username or password')
    }

    // Generate development JWT token
    const token = generateDevToken(foundUser.user)
    
    // Store auth data
    const authData = {
      user: foundUser.user,
      token: token,
      expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString() // 24 hours
    }
    
    localStorage.setItem('remotec-simple-auth', JSON.stringify(authData))
    
    // Set auth header
    if (axios.defaults) {
      axios.defaults.headers.common['Authorization'] = `Bearer ${token}`
    }
    
    setUser(foundUser.user)
    navigate('/dashboard')
  }

  const logout = () => {
    localStorage.removeItem('remotec-simple-auth')
    delete axios.defaults.headers.common['Authorization']
    setUser(null)
    navigate('/login')
  }

  const value = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    logout
  }

  return (
    <SimpleAuthContext.Provider value={value}>
      {children}
    </SimpleAuthContext.Provider>
  )
}

export const useSimpleAuth = () => {
  const context = useContext(SimpleAuthContext)
  if (!context) {
    throw new Error('useSimpleAuth must be used within a SimpleAuthProvider')
  }
  return context
}