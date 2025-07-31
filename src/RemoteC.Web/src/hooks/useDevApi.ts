import { useState, useCallback } from 'react'
import axios, { AxiosError, AxiosRequestConfig } from 'axios'
import { config } from '@/config/config'

interface ApiError {
  message: string
  status?: number
  details?: any
}

export const useDevApi = () => {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)

  const getAccessToken = async (): Promise<string> => {
    // In dev mode, return the stored token or a dummy token
    const storedAuth = localStorage.getItem('dev-auth')
    if (storedAuth) {
      const { token } = JSON.parse(storedAuth)
      return token
    }
    return 'dev-token-123'
  }

  const request = useCallback(
    async <T = any>(
      method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH',
      url: string,
      data?: any,
      requestConfig?: AxiosRequestConfig
    ): Promise<T> => {
      setLoading(true)
      setError(null)

      try {
        const token = await getAccessToken()
        const response = await axios({
          method,
          url: `${config.apiUrl}${url}`,
          data,
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
            ...requestConfig?.headers,
          },
          ...requestConfig,
        })

        return response.data
      } catch (err) {
        const axiosError = err as AxiosError
        const responseData = axiosError.response?.data as { message?: string } | undefined
        const apiError: ApiError = {
          message: responseData?.message || axiosError.message || 'An error occurred',
          status: axiosError.response?.status,
          details: axiosError.response?.data,
        }
        setError(apiError)
        throw apiError
      } finally {
        setLoading(false)
      }
    },
    []
  )

  const get = useCallback(
    <T = any>(url: string, requestConfig?: AxiosRequestConfig) => {
      return request<T>('GET', url, undefined, requestConfig)
    },
    [request]
  )

  const post = useCallback(
    <T = any>(url: string, data?: any, requestConfig?: AxiosRequestConfig) => {
      return request<T>('POST', url, data, requestConfig)
    },
    [request]
  )

  const put = useCallback(
    <T = any>(url: string, data?: any, requestConfig?: AxiosRequestConfig) => {
      return request<T>('PUT', url, data, requestConfig)
    },
    [request]
  )

  const del = useCallback(
    <T = any>(url: string, requestConfig?: AxiosRequestConfig) => {
      return request<T>('DELETE', url, undefined, requestConfig)
    },
    [request]
  )

  const patch = useCallback(
    <T = any>(url: string, data?: any, requestConfig?: AxiosRequestConfig) => {
      return request<T>('PATCH', url, data, requestConfig)
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