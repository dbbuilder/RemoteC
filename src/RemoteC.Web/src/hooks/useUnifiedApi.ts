import { config } from '@/config/config'
import { useApi } from './useApi'
import { useDevApi } from './useDevApi'
import { useSimpleApi } from './useSimpleApi'
import { AxiosRequestConfig } from 'axios'

// Check if we should use simple auth
const useSimpleAuth = import.meta.env.VITE_USE_SIMPLE_AUTH === 'true'

// Unified API interface
interface UnifiedApi {
  get: <T = any>(url: string, config?: AxiosRequestConfig) => Promise<T>
  post: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => Promise<T>
  put: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => Promise<T>
  delete: <T = any>(url: string, config?: AxiosRequestConfig) => Promise<T>
  patch: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => Promise<T>
}

// Unified API hook that uses the appropriate implementation based on environment
export function useUnifiedApi(): UnifiedApi {
  if (useSimpleAuth) {
    // eslint-disable-next-line react-hooks/rules-of-hooks
    const axiosInstance = useSimpleApi()
    
    // Wrap axios instance to match unified API interface and extract data
    return {
      get: <T = any>(url: string, config?: AxiosRequestConfig) => 
        axiosInstance.get<T>(url, config).then(res => res.data),
      post: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => 
        axiosInstance.post<T>(url, data, config).then(res => res.data),
      put: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => 
        axiosInstance.put<T>(url, data, config).then(res => res.data),
      delete: <T = any>(url: string, config?: AxiosRequestConfig) => 
        axiosInstance.delete<T>(url, config).then(res => res.data),
      patch: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => 
        axiosInstance.patch<T>(url, data, config).then(res => res.data),
    }
  }
  
  if (config.features.useDevAuth) {
    // eslint-disable-next-line react-hooks/rules-of-hooks
    const api = useDevApi()
    return {
      get: api.get,
      post: api.post,
      put: api.put,
      delete: api.delete,
      patch: api.patch,
    }
  }
  
  // eslint-disable-next-line react-hooks/rules-of-hooks
  const api = useApi()
  return {
    get: api.get,
    post: api.post,
    put: api.put,
    delete: api.delete,
    patch: api.patch,
  }
}

// Export as default for easier migration
export default useUnifiedApi