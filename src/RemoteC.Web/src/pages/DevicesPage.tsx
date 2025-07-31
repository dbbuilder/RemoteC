import { useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useUnifiedSignalR } from '@/contexts/UnifiedSignalRContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useUnifiedApi } from '@/hooks/useUnifiedApi'
import { 
  Monitor, 
  Server, 
  Activity, 
  Cpu, 
  MemoryStick,
  Search,
  RefreshCw,
  AlertCircle
} from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'

interface Device {
  id: string
  name: string
  status: 'Online' | 'Offline' | 'Unknown'
  ipAddress: string
  lastSeen: string
  osVersion: string
  cpuUsage?: number
  memoryUsage?: number
  diskUsage?: number
  networkLatencyMs?: number
  activeSessions?: number
  isHealthy?: boolean
}

export function DevicesPage() {
  const api = useUnifiedApi()
  const { onHostHealthUpdate } = useUnifiedSignalR()
  const [searchTerm, setSearchTerm] = useState('')
  const [deviceHealth, setDeviceHealth] = useState<Record<string, any>>({})

  // Fetch devices with auto-refresh
  const { data: devicesData, isLoading, error, refetch } = useQuery({
    queryKey: ['devices', searchTerm],
    queryFn: async () => {
      try {
        const params = new URLSearchParams()
        if (searchTerm) params.append('search', searchTerm)
        const result = await api.get(`/api/devices?${params}`)
        return result
      } catch (error) {
        console.error('Failed to fetch devices:', error)
        // Return mock data for demo purposes when API is not available (dev mode only)
        const isDev = import.meta.env.DEV
        if (isDev && error instanceof Error && error.message.includes('Network Error')) {
          console.log('Using mock data for devices (development mode)')
          const { mockDevices } = await import('@/mocks/mockApi')
          return mockDevices
        }
        throw error
      }
    },
    refetchInterval: 10000, // Refresh every 10 seconds
  })

  // Subscribe to SignalR health updates
  useEffect(() => {
    const cleanup = onHostHealthUpdate((hostId, health) => {
      console.log('Received health update for host:', hostId, health)
      setDeviceHealth(prev => ({
        ...prev,
        [hostId]: health
      }))
    })
    
    return cleanup
  }, [onHostHealthUpdate])

  const devices: Device[] = devicesData?.items || []

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Online':
        return <Badge variant="success">Online</Badge>
      case 'Offline':
        return <Badge variant="secondary">Offline</Badge>
      default:
        return <Badge variant="outline">Unknown</Badge>
    }
  }

  const getHealthIcon = (device: Device) => {
    const health = deviceHealth[device.id] || device
    if (!health.isHealthy) {
      return <AlertCircle className="h-4 w-4 text-destructive" />
    }
    return <Activity className="h-4 w-4 text-green-500" />
  }

  if (error && !(error instanceof Error && error.message.includes('Network Error'))) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Devices</h1>
          <p className="text-muted-foreground">
            Manage and monitor connected devices
          </p>
        </div>
        
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error loading devices</AlertTitle>
          <AlertDescription>
            {error instanceof Error ? error.message : 'Failed to load devices. Please try again.'}
          </AlertDescription>
        </Alert>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Devices</h1>
        <p className="text-muted-foreground">
          Manage and monitor connected devices
        </p>
      </div>

      {/* Search and Actions */}
      <div className="flex items-center justify-between">
        <div className="relative w-full max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search devices..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-9"
          />
        </div>
        <Button onClick={() => refetch()} variant="outline" size="icon">
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      {/* Devices Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {isLoading ? (
          <>
            {[1, 2, 3].map((i) => (
              <Card key={i} className="animate-pulse">
                <CardHeader>
                  <div className="h-4 bg-muted rounded w-3/4"></div>
                  <div className="h-3 bg-muted rounded w-1/2 mt-2"></div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    <div className="h-3 bg-muted rounded"></div>
                    <div className="h-3 bg-muted rounded"></div>
                    <div className="h-3 bg-muted rounded"></div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </>
        ) : devices.length === 0 ? (
          <div className="col-span-full text-center py-12">
            <Server className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <p className="text-muted-foreground">No devices found</p>
          </div>
        ) : (
          devices.map((device) => {
            const health = deviceHealth[device.id] || device
            return (
              <Card key={device.id} className="relative">
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div>
                      <CardTitle className="flex items-center gap-2">
                        <Monitor className="h-5 w-5" />
                        {device.name}
                      </CardTitle>
                      <CardDescription>{device.osVersion}</CardDescription>
                    </div>
                    <div className="flex items-center gap-2">
                      {getHealthIcon(device)}
                      {getStatusBadge(device.status)}
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">IP Address:</span>
                      <span className="font-medium">{device.ipAddress}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Last Seen:</span>
                      <span className="font-medium">
                        {new Date(device.lastSeen).toLocaleString()}
                      </span>
                    </div>
                    {device.status === 'Online' && (
                      <>
                        <div className="flex justify-between">
                          <span className="text-muted-foreground">Active Sessions:</span>
                          <span className="font-medium">{health.activeSessions || 0}</span>
                        </div>
                        <div className="pt-2 space-y-2">
                          <div className="flex items-center justify-between">
                            <span className="flex items-center gap-1 text-muted-foreground">
                              <Cpu className="h-3 w-3" /> CPU
                            </span>
                            <span className="font-medium">{health.cpuUsage || 0}%</span>
                          </div>
                          <div className="flex items-center justify-between">
                            <span className="flex items-center gap-1 text-muted-foreground">
                              <MemoryStick className="h-3 w-3" /> Memory
                            </span>
                            <span className="font-medium">{health.memoryUsage || 0}%</span>
                          </div>
                          <div className="flex items-center justify-between">
                            <span className="flex items-center gap-1 text-muted-foreground">
                              <Server className="h-3 w-3" /> Disk
                            </span>
                            <span className="font-medium">{health.diskUsage || 0}%</span>
                          </div>
                        </div>
                      </>
                    )}
                  </div>
                  {device.status === 'Online' && (
                    <div className="mt-4 flex gap-2">
                      <Button size="sm" className="flex-1">
                        Connect
                      </Button>
                      <Button size="sm" variant="outline" className="flex-1">
                        Details
                      </Button>
                    </div>
                  )}
                </CardContent>
              </Card>
            )
          })
        )}
      </div>
    </div>
  )
}