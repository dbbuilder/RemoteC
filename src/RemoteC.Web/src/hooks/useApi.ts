import { useState, useCallback } from 'react'
import { useMsal } from '@azure/msal-react'
import axios, { AxiosError, AxiosRequestConfig } from 'axios'
import { apiConfig, silentRequest } from '@/config/authConfig'

interface ApiError {
  message: string
  status?: number
  details?: any
}

export const useApi = () => {
  const { instance, accounts } = useMsal()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)

  const getAccessToken = async (): Promise<string> => {
    if (accounts.length === 0) {
      throw new Error('No authenticated user')
    }

    try {
      const response = await instance.acquireTokenSilent({
        ...silentRequest,
        account: accounts[0],
      })
      return response.accessToken
    } catch (error) {
      console.error('Token acquisition failed:', error)
      // Try interactive token acquisition
      const response = await instance.acquireTokenPopup({
        ...silentRequest,
        account: accounts[0],
      })
      return response.accessToken
    }
  }

  const request = useCallback(
    async <T = any>(
      method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH',
      url: string,
      data?: any,
      config?: AxiosRequestConfig
    ): Promise<T> => {
      setLoading(true)
      setError(null)

      try {
        const token = await getAccessToken()
        const response = await axios({
          method,
          url: `${apiConfig.uri}${url}`,
          data,
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
            ...config?.headers,
          },
          ...config,
        })

        return response.data
      } catch (err) {
        const axiosError = err as AxiosError
        const apiError: ApiError = {
          message: axiosError.response?.data?.message || axiosError.message || 'An error occurred',
          status: axiosError.response?.status,
          details: axiosError.response?.data,
        }
        setError(apiError)
        throw apiError
      } finally {
        setLoading(false)
      }
    },
    [instance, accounts]
  )

  const get = useCallback(
    <T = any>(url: string, config?: AxiosRequestConfig) => {
      return request<T>('GET', url, undefined, config)
    },
    [request]
  )

  const post = useCallback(
    <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => {
      return request<T>('POST', url, data, config)
    },
    [request]
  )

  const put = useCallback(
    <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => {
      return request<T>('PUT', url, data, config)
    },
    [request]
  )

  const del = useCallback(
    <T = any>(url: string, config?: AxiosRequestConfig) => {
      return request<T>('DELETE', url, undefined, config)
    },
    [request]
  )

  const patch = useCallback(
    <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => {
      return request<T>('PATCH', url, data, config)
    },
    [request]
  )

  return {
    loading,
    error,
    get,
    post,
    put,
    delete: del,
    patch,
  }
}