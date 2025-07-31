import axios from 'axios'
import { useEffect } from 'react'
import { useSimpleAuth } from '@/contexts/SimpleAuthContext'

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:17001'

// Create axios instance for simple auth
const simpleApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Add response interceptor for auth errors
simpleApi.interceptors.response.use(
  (response) => response.data,
  (error) => {
    if (error.response?.status === 401) {
      // Clear auth and redirect to login
      localStorage.removeItem('remotec-simple-auth')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export function useSimpleApi() {
  const { isAuthenticated } = useSimpleAuth()
  
  useEffect(() => {
    // Set auth header from stored auth
    const stored = localStorage.getItem('remotec-simple-auth')
    if (stored && isAuthenticated) {
      try {
        const authData = JSON.parse(stored)
        simpleApi.defaults.headers.common['Authorization'] = `Bearer ${authData.token}`
      } catch (e) {
        console.error('Failed to set auth header', e)
      }
    }
  }, [isAuthenticated])
  
  return simpleApi
}