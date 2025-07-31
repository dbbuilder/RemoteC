// Application configuration
export const config = {
  isDevelopment: import.meta.env.DEV,
  isProduction: import.meta.env.PROD,
  apiUrl: import.meta.env.VITE_API_URL || 'http://localhost:17001',
  environment: import.meta.env.MODE,
  
  // Feature flags
  features: {
    useDevAuth: import.meta.env.DEV, // Use development authentication in dev mode
    azureAdAuth: import.meta.env.PROD, // Use Azure AD in production
  }
}