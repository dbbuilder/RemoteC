// API Configuration
export const apiConfig = {
  baseUrl: process.env.REACT_APP_API_URL || 'https://localhost:7001',
  timeout: 30000,
  retries: 3,
  retryDelay: 1000,
};

// SignalR Configuration
export const signalRConfig = {
  hubUrl: `${apiConfig.baseUrl}/hubs/session`,
  automaticReconnect: true,
  reconnectDelays: [0, 2000, 10000, 30000],
  serverTimeoutInMilliseconds: 30000,
  keepAliveIntervalInMilliseconds: 15000,
};

// Application Configuration
export const appConfig = {
  name: 'RemoteC',
  version: process.env.REACT_APP_VERSION || '1.0.0',
  environment: process.env.NODE_ENV || 'development',
  debug: process.env.NODE_ENV === 'development',
  features: {
    fileTransfer: true,
    commandExecution: true,
    sessionRecording: true,
    audioStreaming: true,
    multiMonitor: true,
  },
};

// Performance Configuration
export const performanceConfig = {
  pagination: {
    defaultPageSize: 25,
    pageSizeOptions: [10, 25, 50, 100],
  },
  polling: {
    sessionStatus: 5000, // 5 seconds
    deviceStatus: 30000, // 30 seconds
    metrics: 10000, // 10 seconds
  },
  cache: {
    defaultTtl: 300000, // 5 minutes
    sessionTtl: 60000, // 1 minute
    deviceTtl: 600000, // 10 minutes
  },
};

// UI Configuration
export const uiConfig = {
  drawer: {
    width: 280,
    collapsedWidth: 64,
  },
  header: {
    height: 64,
  },
  notification: {
    autoHideDuration: 6000,
    maxSnack: 3,
  },
  session: {
    maxQuality: 100,
    minQuality: 10,
    defaultQuality: 75,
    frameRateOptions: [15, 30, 60],
    compressionOptions: ['Low', 'Medium', 'High', 'Ultra'],
  },
};

// Validation Configuration
export const validationConfig = {
  session: {
    nameMinLength: 1,
    nameMaxLength: 100,
  },
  pin: {
    length: 6,
    expirationMinutes: 10,
  },
  file: {
    maxSizeBytes: 1024 * 1024 * 1024, // 1GB
    allowedExtensions: ['.txt', '.doc', '.docx', '.pdf', '.jpg', '.png', '.zip'],
  },
  command: {
    maxLength: 1000,
    historyLimit: 100,
  },
};

// Theme Configuration
export const themeConfig = {
  modes: ['light', 'dark', 'auto'],
  defaultMode: 'light',
  primaryColors: [
    '#1976d2', // Blue
    '#388e3c', // Green
    '#f57c00', // Orange
    '#7b1fa2', // Purple
    '#c2185b', // Pink
    '#00796b', // Teal
  ],
};

// Export all configurations
export default {
  api: apiConfig,
  signalR: signalRConfig,
  app: appConfig,
  performance: performanceConfig,
  ui: uiConfig,
  validation: validationConfig,
  theme: themeConfig,
};