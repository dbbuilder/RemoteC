// Application configuration
export const config = {
  isDevelopment: import.meta.env.DEV,
  isProduction: import.meta.env.PROD,
  apiUrl: import.meta.env.VITE_API_URL || 'http://localhost:7001',
  environment: import.meta.env.MODE,
  
  // Feature flags
  features: {
    useDevAuth: true, // Always use dev auth for demo
    azureAdAuth: false, // Disable Azure AD for demo
  }
}