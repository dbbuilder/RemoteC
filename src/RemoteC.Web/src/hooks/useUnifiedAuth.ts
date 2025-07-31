import { config } from '@/config/config'
import { useAuth } from '@/contexts/AuthContext'
import { useDevAuth } from '@/contexts/DevAuthContext'

// Unified auth hook that uses the appropriate auth context based on environment
export function useUnifiedAuth() {
  if (config.features.useDevAuth) {
    // eslint-disable-next-line react-hooks/rules-of-hooks
    return useDevAuth()
  }
  
  // In production, useAuth will still work but we need to handle the case
  // where it's not available (when using DevApp)
  try {
    // eslint-disable-next-line react-hooks/rules-of-hooks
    return useAuth()
  } catch {
    // If useAuth fails, we're in DevApp, so return a mock
    return {
      user: null,
      account: null,
      isAuthenticated: false,
      isLoading: false,
      login: async () => { throw new Error('Auth not available') },
      logout: async () => { throw new Error('Auth not available') },
      getAccessToken: async () => { throw new Error('Auth not available') },
    }
  }
}