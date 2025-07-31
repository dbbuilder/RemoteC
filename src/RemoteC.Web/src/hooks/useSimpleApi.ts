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

// Add request interceptor to always include the latest token
simpleApi.interceptors.request.use(
  (config) => {
    const stored = localStorage.getItem('remotec-simple-auth')
    if (stored) {
      try {
        const authData = JSON.parse(stored)
        config.headers['Authorization'] = `Bearer ${authData.token}`
      } catch (e) {
        console.error('Failed to parse auth token', e)
      }
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Add response interceptor for auth errors
simpleApi.interceptors.response.use(
  (response) => response, // Return full response, not just data
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
    // Force re-check of auth header when auth state changes
    if (!isAuthenticated) {
      delete simpleApi.defaults.headers.common['Authorization']
    }
  }, [isAuthenticated])
  
  return simpleApi
}