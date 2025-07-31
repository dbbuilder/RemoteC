import { config } from '@/config/config'
import { useApi } from './useApi'
import { useDevApi } from './useDevApi'

// Unified API hook that uses the appropriate implementation based on environment
export function useUnifiedApi() {
  if (config.features.useDevAuth) {
    // eslint-disable-next-line react-hooks/rules-of-hooks
    return useDevApi()
  }
  
  // eslint-disable-next-line react-hooks/rules-of-hooks
  return useApi()
}

// Export as default for easier migration
export default useUnifiedApi