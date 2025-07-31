// Mock API responses for development when backend is not available

export const mockDashboardStats = {
  activeSessions: 3,
  onlineDevices: 12,
  totalUsers: 25,
  systemHealth: 'Good'
}

export const mockDevices = {
  items: [
    {
      id: '1',
      name: 'DESKTOP-ABC123',
      status: 'Online',
      ipAddress: '192.168.1.100',
      lastSeen: new Date().toISOString(),
      osVersion: 'Windows 11 Pro',
      cpuUsage: 25,
      memoryUsage: 60,
      diskUsage: 45,
      networkLatencyMs: 5,
      activeSessions: 1,
      isHealthy: true
    },
    {
      id: '2',
      name: 'LAPTOP-XYZ789',
      status: 'Online',
      ipAddress: '192.168.1.101',
      lastSeen: new Date().toISOString(),
      osVersion: 'Windows 10 Pro',
      cpuUsage: 45,
      memoryUsage: 80,
      diskUsage: 70,
      networkLatencyMs: 10,
      activeSessions: 0,
      isHealthy: true
    },
    {
      id: '3',
      name: 'SERVER-001',
      status: 'Offline',
      ipAddress: '192.168.1.50',
      lastSeen: new Date(Date.now() - 3600000).toISOString(),
      osVersion: 'Windows Server 2022',
      cpuUsage: 0,
      memoryUsage: 0,
      diskUsage: 0,
      networkLatencyMs: 0,
      activeSessions: 0,
      isHealthy: false
    }
  ],
  totalCount: 3,
  pageNumber: 1,
  pageSize: 10
}

export const mockUsers = {
  items: [
    {
      id: '1',
      email: 'admin@remotec.com',
      displayName: 'System Administrator',
      roles: ['Admin', 'Operator', 'Viewer'],
      isActive: true,
      lastLogin: new Date().toISOString(),
      createdAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString()
    },
    {
      id: '2',
      email: 'john.doe@remotec.com',
      displayName: 'John Doe',
      roles: ['Operator', 'Viewer'],
      isActive: true,
      lastLogin: new Date(Date.now() - 3600000).toISOString(),
      createdAt: new Date(Date.now() - 15 * 24 * 60 * 60 * 1000).toISOString()
    },
    {
      id: '3',
      email: 'jane.smith@remotec.com',
      displayName: 'Jane Smith',
      roles: ['Viewer'],
      isActive: false,
      lastLogin: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
      createdAt: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000).toISOString()
    }
  ],
  totalCount: 3,
  pageNumber: 1,
  pageSize: 10
}

export const mockSessions = {
  items: [
    {
      id: '1',
      hostId: '1',
      hostName: 'DESKTOP-ABC123',
      viewerId: '1',
      viewerName: 'admin@remotec.com',
      status: 'Active',
      startTime: new Date(Date.now() - 1800000).toISOString(),
      endTime: null,
      duration: 1800,
      clientIp: '192.168.1.200'
    },
    {
      id: '2',
      hostId: '2',
      hostName: 'LAPTOP-XYZ789',
      viewerId: '2',
      viewerName: 'john.doe@remotec.com',
      status: 'Ended',
      startTime: new Date(Date.now() - 7200000).toISOString(),
      endTime: new Date(Date.now() - 3600000).toISOString(),
      duration: 3600,
      clientIp: '192.168.1.201'
    }
  ],
  totalCount: 2,
  pageNumber: 1,
  pageSize: 10
}

export const mockAuditLogs = {
  items: [
    {
      id: '1',
      timestamp: new Date().toISOString(),
      userId: '1',
      userName: 'admin@remotec.com',
      action: 'UserLogin',
      actionType: 'Authentication',
      severity: 'Info',
      ipAddress: '192.168.1.200',
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0',
      result: 'Success',
      details: 'User logged in successfully'
    },
    {
      id: '2',
      timestamp: new Date(Date.now() - 600000).toISOString(),
      userId: '2',
      userName: 'john.doe@remotec.com',
      action: 'SessionStart',
      actionType: 'RemoteControl',
      severity: 'Info',
      ipAddress: '192.168.1.201',
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0',
      result: 'Success',
      details: 'Remote session started with DESKTOP-ABC123'
    }
  ],
  totalCount: 2,
  pageNumber: 1,
  pageSize: 10
}

export const mockSettings = {
  general: {
    systemName: 'RemoteC Enterprise',
    adminEmail: 'admin@remotec.com',
    maintenanceMode: false,
    allowPublicRegistration: false
  },
  security: {
    sessionTimeout: 30,
    maxLoginAttempts: 5,
    enforcePasswordPolicy: true,
    minPasswordLength: 8,
    require2FA: false,
    allowedIpRanges: []
  },
  remoteControl: {
    maxConcurrentSessions: 10,
    sessionRecording: true,
    clipboardSharing: true,
    fileTransferEnabled: true,
    maxFileSize: 100,
    compressionLevel: 'medium'
  },
  notifications: {
    emailEnabled: true,
    smtpServer: 'smtp.gmail.com',
    smtpPort: 587,
    smtpUser: 'notifications@remotec.com',
    alertOnFailedLogin: true,
    alertOnNewDevice: true,
    dailyReports: false
  },
  database: {
    connectionString: 'Hidden for security',
    backupSchedule: 'daily',
    retentionDays: 30,
    enableAuditLog: true,
    logLevel: 'info'
  }
}