import { config } from '@/config/config'
import { useApi } from './useApi'
import { useDevApi } from './useDevApi'
import { useSimpleApi } from './useSimpleApi'

// Check if we should use simple auth
const useSimpleAuth = import.meta.env.VITE_USE_SIMPLE_AUTH === 'true'

// Unified API hook that uses the appropriate implementation based on environment
export function useUnifiedApi() {
  if (useSimpleAuth) {
    // eslint-disable-next-line react-hooks/rules-of-hooks
    return useSimpleApi()
  }
  
  if (config.features.useDevAuth) {
    // eslint-disable-next-line react-hooks/rules-of-hooks
    return useDevApi()
  }
  
  // eslint-disable-next-line react-hooks/rules-of-hooks
  return useApi()
}

// Export as default for easier migration
export default useUnifiedApi