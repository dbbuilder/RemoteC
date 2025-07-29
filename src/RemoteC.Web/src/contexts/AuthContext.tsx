import React, { createContext, useContext, useState, useEffect } from 'react'
import { useMsal, useIsAuthenticated } from '@azure/msal-react'
import { AccountInfo, InteractionStatus } from '@azure/msal-browser'
import { loginRequest, silentRequest } from '@/config/authConfig'
import { User } from '@/types'

interface AuthContextType {
  user: User | null
  account: AccountInfo | null
  isAuthenticated: boolean
  isLoading: boolean
  login: () => Promise<void>
  logout: () => Promise<void>
  getAccessToken: () => Promise<string>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { instance, accounts, inProgress } = useMsal()
  const isAuthenticated = useIsAuthenticated()
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const account = accounts[0] || null

  useEffect(() => {
    if (isAuthenticated && account) {
      // Create user object from account info
      const userData: User = {
        id: account.localAccountId || account.homeAccountId,
        email: account.username,
        displayName: account.name || account.username,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        lastLoginAt: new Date().toISOString(),
        isActive: true,
        roles: [],
        permissions: [],
      }
      setUser(userData)
    } else {
      setUser(null)
    }
    
    if (inProgress === InteractionStatus.None) {
      setIsLoading(false)
    }
  }, [isAuthenticated, account, inProgress])

  const login = async () => {
    try {
      await instance.loginPopup(loginRequest)
    } catch (error) {
      console.error('Login failed:', error)
      throw error
    }
  }

  const logout = async () => {
    try {
      await instance.logoutPopup()
    } catch (error) {
      console.error('Logout failed:', error)
      throw error
    }
  }

  const getAccessToken = async (): Promise<string> => {
    if (!account) {
      throw new Error('No account found')
    }

    try {
      // Try to acquire token silently first
      const response = await instance.acquireTokenSilent({
        ...silentRequest,
        account,
      })
      return response.accessToken
    } catch (error) {
      // If silent acquisition fails, fall back to interactive
      console.log('Silent token acquisition failed, attempting interactive')
      const response = await instance.acquireTokenPopup({
        ...loginRequest,
        account,
      })
      return response.accessToken
    }
  }

  const value: AuthContextType = {
    user,
    account,
    isAuthenticated,
    isLoading,
    login,
    logout,
    getAccessToken,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}