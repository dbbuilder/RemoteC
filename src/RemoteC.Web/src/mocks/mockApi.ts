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
  pageSize: 10,
  totalPages: 1
}

export const mockUsers = {
  items: [
    {
      id: '1',
      email: 'admin@remotec.com',
      displayName: 'System Administrator',
      roles: ['Admin', 'Operator', 'Viewer'],
      isActive: true,
      lastLoginAt: new Date().toISOString(),
      createdAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date().toISOString(),
      permissions: []
    },
    {
      id: '2',
      email: 'john.doe@remotec.com',
      displayName: 'John Doe',
      roles: ['Operator', 'Viewer'],
      isActive: true,
      lastLoginAt: new Date(Date.now() - 3600000).toISOString(),
      createdAt: new Date(Date.now() - 15 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date().toISOString(),
      permissions: []
    },
    {
      id: '3',
      email: 'jane.smith@remotec.com',
      displayName: 'Jane Smith',
      roles: ['Viewer'],
      isActive: false,
      lastLoginAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
      createdAt: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date().toISOString(),
      permissions: []
    }
  ],
  totalCount: 3,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 1
}

export const mockSessions = {
  items: [
    {
      id: '1',
      deviceId: '1',
      device: {
        id: '1',
        name: 'DESKTOP-ABC123',
        machineId: 'ABC123',
        operatingSystem: 'Windows 11',
        lastSeenAt: new Date().toISOString(),
        isOnline: true,
        ipAddress: '192.168.1.100',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      userId: '1',
      user: {
        id: '1',
        email: 'admin@remotec.com',
        displayName: 'System Administrator',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        lastLoginAt: new Date().toISOString(),
        isActive: true,
        roles: ['Admin'],
        permissions: []
      },
      status: 'Active' as const,
      startedAt: new Date(Date.now() - 1800000).toISOString(),
      endedAt: undefined,
      sessionPin: '123456'
    },
    {
      id: '2',
      deviceId: '2',
      device: {
        id: '2',
        name: 'LAPTOP-XYZ789',
        machineId: 'XYZ789',
        operatingSystem: 'Windows 10',
        lastSeenAt: new Date().toISOString(),
        isOnline: true,
        ipAddress: '192.168.1.101',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      userId: '2',
      user: {
        id: '2',
        email: 'john.doe@remotec.com',
        displayName: 'John Doe',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        lastLoginAt: new Date().toISOString(),
        isActive: true,
        roles: ['Operator'],
        permissions: []
      },
      status: 'Completed' as const,
      startedAt: new Date(Date.now() - 7200000).toISOString(),
      endedAt: new Date(Date.now() - 3600000).toISOString(),
      sessionPin: '789012'
    }
  ],
  totalCount: 2,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 1
}

export const mockAuditLogs = {
  items: [
    {
      id: '1',
      timestamp: new Date().toISOString(),
      userId: '1',
      action: 'UserLogin',
      entityType: 'User',
      entityId: '1',
      ipAddress: '192.168.1.200',
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0',
      newValues: JSON.stringify({ login: 'successful' })
    },
    {
      id: '2',
      timestamp: new Date(Date.now() - 600000).toISOString(),
      userId: '2',
      action: 'SessionStart',
      entityType: 'Session',
      entityId: '1',
      ipAddress: '192.168.1.201',
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0',
      newValues: JSON.stringify({ deviceName: 'DESKTOP-ABC123' })
    }
  ],
  totalCount: 2,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 1
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