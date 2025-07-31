export interface User {
  id: string
  email: string
  displayName: string
  createdAt: string
  updatedAt: string
  lastLoginAt: string
  isActive: boolean
  roles: string[]
  permissions: string[]
}

export interface Device {
  id: string
  name: string
  machineId: string
  operatingSystem: string
  lastSeenAt: string
  isOnline: boolean
  ipAddress?: string
  macAddress?: string
  userId?: string
  user?: User
  createdAt: string
  updatedAt: string
  health?: HostHealth
}

export interface HostHealth {
  cpuUsage: number
  memoryUsage: number
  diskUsage: number
  temperature?: number
  lastReported: string
}

export interface Session {
  id: string
  deviceId: string
  device?: Device
  userId: string
  user?: User
  startedAt: string
  endedAt?: string
  status: SessionStatus
  sessionPin?: string
  metadata?: Record<string, any>
}

export type SessionStatus = 'Pending' | 'Active' | 'Completed' | 'Failed'

export interface SessionPin {
  id: string
  sessionId: string
  pin: string
  createdAt: string
  expiresAt: string
  isUsed: boolean
}

export interface FileTransfer {
  id: string
  sessionId: string
  fileName: string
  fileSize: number
  direction: 'Upload' | 'Download'
  status: FileTransferStatus
  progress: number
  startedAt: string
  completedAt?: string
  error?: string
}

export type FileTransferStatus = 'Pending' | 'InProgress' | 'Completed' | 'Failed' | 'Cancelled'

export interface AuditLog {
  id: string
  userId: string
  user?: User
  action: string
  entityType: string
  entityId: string
  oldValues?: string
  newValues?: string
  ipAddress?: string
  userAgent?: string
  timestamp: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}