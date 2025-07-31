import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { Button } from '@/components/ui/button'
import { useUnifiedApi } from '@/hooks/useUnifiedApi'
import { Device, PagedResult } from '@/types'
import { Monitor, Cpu, Thermometer, Activity, RefreshCw } from 'lucide-react'
import { formatDistanceToNow } from 'date-fns'
import { useSignalR } from '@/contexts/UnifiedSignalRContext'

export function DevicesPage() {
  const api = useUnifiedApi()
  const { connection } = useSignalR()
  const [devices, setDevices] = useState<Device[]>([])

  // Fetch devices with auto-refresh every 5 seconds
  const { data, isLoading, refetch, isFetching } = useQuery<PagedResult<Device>>({
    queryKey: ['devices'],
    queryFn: () => api.get('/api/devices?pageSize=100'),
    refetchInterval: 5000, // Refresh every 5 seconds
    refetchIntervalInBackground: true, // Continue refreshing when tab is not active
  })

  // Listen for real-time health updates
  useEffect(() => {
    if (!connection) return

    const handleHealthUpdate = (hostId: string, health: any) => {
      setDevices(prev => prev.map(device => 
        device.id === hostId 
          ? { ...device, health: { ...health, lastReported: new Date().toISOString() } }
          : device
      ))
    }

    connection.on('HostHealthUpdate', handleHealthUpdate)

    return () => {
      connection.off('HostHealthUpdate', handleHealthUpdate)
    }
  }, [connection])

  // Update devices when data changes
  useEffect(() => {
    if (data?.items) {
      setDevices(data.items)
    }
  }, [data])

  const getHealthBadgeVariant = (value: number, type: 'cpu' | 'memory' | 'disk') => {
    if (type === 'cpu' || type === 'memory') {
      if (value < 50) return 'default'
      if (value < 80) return 'secondary'
      return 'destructive'
    } else { // disk
      if (value < 70) return 'default'
      if (value < 90) return 'secondary'
      return 'destructive'
    }
  }

  const formatHealthValue = (value: number | undefined) => {
    if (value === undefined || value < 0) return 'N/A'
    return `${Math.round(value)}%`
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Devices</h1>
          <p className="text-muted-foreground">
            Monitor and manage connected hosts and their health status
          </p>
        </div>
        <div className="flex items-center gap-2">
          {isFetching && (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <RefreshCw className="h-3 w-3 animate-spin" />
              Updating...
            </div>
          )}
          <Button onClick={() => refetch()} size="sm" variant="outline">
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh Now
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Devices</CardTitle>
            <Monitor className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{devices.length}</div>
            <p className="text-xs text-muted-foreground">Registered in system</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Online</CardTitle>
            <Activity className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {devices.filter(d => d.isOnline).length}
            </div>
            <p className="text-xs text-muted-foreground">Currently connected</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Avg CPU Usage</CardTitle>
            <Cpu className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {devices.length > 0
                ? formatHealthValue(
                    devices.reduce((sum, d) => sum + (d.health?.cpuUsage || 0), 0) / devices.length
                  )
                : 'N/A'}
            </div>
            <p className="text-xs text-muted-foreground">Across all devices</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Warnings</CardTitle>
            <Thermometer className="h-4 w-4 text-orange-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {devices.filter(d => 
                (d.health?.cpuUsage || 0) > 80 ||
                (d.health?.memoryUsage || 0) > 80 ||
                (d.health?.diskUsage || 0) > 90
              ).length}
            </div>
            <p className="text-xs text-muted-foreground">High resource usage</p>
          </CardContent>
        </Card>
      </div>

      {/* Devices Table */}
      <Card>
        <CardHeader>
          <CardTitle>All Devices</CardTitle>
          <CardDescription>
            Real-time monitoring of connected hosts and their resource usage
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8 text-muted-foreground">
              Loading devices...
            </div>
          ) : devices.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No devices registered yet
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Device Name</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Operating System</TableHead>
                  <TableHead>CPU</TableHead>
                  <TableHead>Memory</TableHead>
                  <TableHead>Disk</TableHead>
                  <TableHead>Last Seen</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {devices.map((device) => (
                  <TableRow key={device.id}>
                    <TableCell className="font-medium">
                      <div>
                        <div className="font-medium">{device.name}</div>
                        <div className="text-xs text-muted-foreground">{device.id}</div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={device.isOnline ? 'default' : 'secondary'}>
                        {device.isOnline ? 'Online' : 'Offline'}
                      </Badge>
                    </TableCell>
                    <TableCell>{device.operatingSystem}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Progress 
                          value={device.health?.cpuUsage || 0} 
                          className="w-16 h-2"
                        />
                        <Badge 
                          variant={getHealthBadgeVariant(device.health?.cpuUsage || 0, 'cpu')}
                          className="text-xs"
                        >
                          {formatHealthValue(device.health?.cpuUsage)}
                        </Badge>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Progress 
                          value={Math.max(0, device.health?.memoryUsage || 0)} 
                          className="w-16 h-2"
                        />
                        <Badge 
                          variant={getHealthBadgeVariant(device.health?.memoryUsage || 0, 'memory')}
                          className="text-xs"
                        >
                          {formatHealthValue(device.health?.memoryUsage)}
                        </Badge>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Progress 
                          value={device.health?.diskUsage || 0} 
                          className="w-16 h-2"
                        />
                        <Badge 
                          variant={getHealthBadgeVariant(device.health?.diskUsage || 0, 'disk')}
                          className="text-xs"
                        >
                          {formatHealthValue(device.health?.diskUsage)}
                        </Badge>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="text-sm">
                        {formatDistanceToNow(new Date(device.lastSeenAt), { addSuffix: true })}
                      </div>
                      {device.health?.lastReported && (
                        <div className="text-xs text-muted-foreground">
                          Health: {formatDistanceToNow(new Date(device.health.lastReported), { addSuffix: true })}
                        </div>
                      )}
                    </TableCell>
                    <TableCell>
                      <Button size="sm" variant="ghost">
                        Connect
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}