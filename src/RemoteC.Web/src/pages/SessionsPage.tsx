import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useUnifiedApi } from '@/hooks/useUnifiedApi'
import { Session, PagedResult, SessionStatus } from '@/types'
import { Play, Pause, StopCircle, Search, Download, Upload, Clock, Monitor } from 'lucide-react'
import { formatDistanceToNow, format } from 'date-fns'
import { useNavigate } from 'react-router-dom'

export function SessionsPage() {
  const api = useUnifiedApi()
  const navigate = useNavigate()
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<SessionStatus | 'All'>('All')

  // Fetch sessions with auto-refresh every 3 seconds for active sessions
  const { data: sessionsData, isLoading: isLoadingSessions, error, refetch } = useQuery<PagedResult<Session>>({
    queryKey: ['sessions', statusFilter],
    queryFn: async () => {
      try {
        const params = new URLSearchParams({ pageSize: '100' })
        if (statusFilter !== 'All') {
          params.append('status', statusFilter)
        }
        return await api.get(`/api/sessions?${params}`)
      } catch (error) {
        console.error('Failed to fetch sessions:', error)
        // Return mock data for demo purposes when API is not available (dev mode only)
        const isDev = import.meta.env.DEV
        if (isDev && error instanceof Error && error.message.includes('Network Error')) {
          console.log('Using mock data for sessions (development mode)')
          const { mockSessions } = await import('@/mocks/mockApi')
          return mockSessions
        }
        throw error
      }
    },
    refetchInterval: statusFilter === 'Active' ? 3000 : 10000, // Faster refresh for active sessions
    refetchIntervalInBackground: true,
  })

  // Fetch session statistics with auto-refresh
  const { data: stats } = useQuery({
    queryKey: ['session-stats'],
    queryFn: async () => {
      try {
        return await api.get('/api/sessions/stats')
      } catch (error) {
        console.error('Failed to fetch session stats:', error)
        // Return mock stats in dev mode
        const isDev = import.meta.env.DEV
        if (isDev && error instanceof Error && error.message.includes('Network Error')) {
          return {
            activeSessions: 3,
            todaysSessions: 15,
            avgDuration: '45m',
            dataTransferred: '256 MB'
          }
        }
        throw error
      }
    },
    refetchInterval: 5000,
    refetchIntervalInBackground: true,
  })

  const sessions = sessionsData?.items || []
  const filteredSessions = sessions.filter(session => {
    if (!searchTerm) return true
    const searchLower = searchTerm.toLowerCase()
    return (
      session.device?.name.toLowerCase().includes(searchLower) ||
      session.user?.displayName.toLowerCase().includes(searchLower) ||
      session.sessionPin?.toLowerCase().includes(searchLower) ||
      session.id.toLowerCase().includes(searchLower)
    )
  })

  const getStatusBadgeVariant = (status: SessionStatus) => {
    switch (status) {
      case 'Active': return 'default'
      case 'Pending': return 'secondary'
      case 'Completed': return 'outline'
      case 'Failed': return 'destructive'
      default: return 'secondary'
    }
  }

  const getSessionDuration = (session: Session) => {
    if (!session.startedAt) return 'N/A'
    const start = new Date(session.startedAt)
    const end = session.endedAt ? new Date(session.endedAt) : new Date()
    const durationMs = end.getTime() - start.getTime()
    const minutes = Math.floor(durationMs / 60000)
    const hours = Math.floor(minutes / 60)
    const remainingMinutes = minutes % 60
    
    if (hours > 0) {
      return `${hours}h ${remainingMinutes}m`
    }
    return `${minutes}m`
  }

  const handleSessionAction = async (sessionId: string, action: 'pause' | 'resume' | 'stop') => {
    try {
      await api.post(`/api/sessions/${sessionId}/${action}`)
      refetch()
    } catch (error) {
      console.error(`Failed to ${action} session:`, error)
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Sessions</h1>
        <p className="text-muted-foreground">
          Monitor and manage active remote control sessions
        </p>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Sessions</CardTitle>
            <Monitor className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.activeSessions || 0}</div>
            <p className="text-xs text-muted-foreground">Currently running</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Today's Sessions</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.todaysSessions || 0}</div>
            <p className="text-xs text-muted-foreground">Started today</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Avg Duration</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.avgDuration || '0m'}</div>
            <p className="text-xs text-muted-foreground">Per session today</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Data Transferred</CardTitle>
            <div className="flex gap-1">
              <Upload className="h-3 w-3 text-blue-500" />
              <Download className="h-3 w-3 text-green-500" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.dataTransferred || '0 MB'}</div>
            <p className="text-xs text-muted-foreground">Total today</p>
          </CardContent>
        </Card>
      </div>

      {/* Sessions Table */}
      <Card>
        <CardHeader>
          <div className="flex justify-between items-center">
            <div>
              <CardTitle>Remote Sessions</CardTitle>
              <CardDescription>View and manage all remote control sessions</CardDescription>
            </div>
            <div className="flex gap-2">
              <div className="relative">
                <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search sessions..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-8 w-64"
                />
              </div>
              <Button onClick={() => refetch()} variant="outline" size="sm">
                Refresh
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Tabs value={statusFilter} onValueChange={(value) => setStatusFilter(value as SessionStatus | 'All')}>
            <TabsList>
              <TabsTrigger value="All">All Sessions</TabsTrigger>
              <TabsTrigger value="Active">Active</TabsTrigger>
              <TabsTrigger value="Pending">Pending</TabsTrigger>
              <TabsTrigger value="Completed">Completed</TabsTrigger>
              <TabsTrigger value="Failed">Failed</TabsTrigger>
            </TabsList>
            <TabsContent value={statusFilter} className="mt-4">
              {isLoadingSessions ? (
                <div className="text-center py-8 text-muted-foreground">
                  Loading sessions...
                </div>
              ) : error && !(error instanceof Error && error.message.includes('Network Error')) ? (
                <div className="text-center py-8 text-destructive">
                  Error loading sessions: {error instanceof Error ? error.message : 'Unknown error'}
                </div>
              ) : filteredSessions.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">
                  No sessions found
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Session ID</TableHead>
                      <TableHead>Device</TableHead>
                      <TableHead>User</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Started</TableHead>
                      <TableHead>Duration</TableHead>
                      <TableHead>PIN</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {filteredSessions.map((session) => (
                      <TableRow key={session.id}>
                        <TableCell className="font-mono text-xs">
                          {session.id.substring(0, 8)}...
                        </TableCell>
                        <TableCell>
                          <div>
                            <div className="font-medium">{session.device?.name || 'Unknown'}</div>
                            <div className="text-xs text-muted-foreground">
                              {session.device?.operatingSystem || 'N/A'}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div>
                            <div className="font-medium">{session.user?.displayName || 'Unknown'}</div>
                            <div className="text-xs text-muted-foreground">
                              {session.user?.email || 'N/A'}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant={getStatusBadgeVariant(session.status)}>
                            {session.status}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <div className="text-sm">
                            {session.startedAt 
                              ? format(new Date(session.startedAt), 'MMM d, HH:mm')
                              : 'N/A'}
                          </div>
                          {session.startedAt && (
                            <div className="text-xs text-muted-foreground">
                              {formatDistanceToNow(new Date(session.startedAt), { addSuffix: true })}
                            </div>
                          )}
                        </TableCell>
                        <TableCell>{getSessionDuration(session)}</TableCell>
                        <TableCell>
                          {session.sessionPin && (
                            <code className="px-2 py-1 bg-muted rounded text-xs">
                              {session.sessionPin}
                            </code>
                          )}
                        </TableCell>
                        <TableCell>
                          <div className="flex gap-1">
                            {session.status === 'Active' && (
                              <>
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => navigate(`/sessions/${session.id}`)}
                                >
                                  View
                                </Button>
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => handleSessionAction(session.id, 'pause')}
                                >
                                  <Pause className="h-4 w-4" />
                                </Button>
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => handleSessionAction(session.id, 'stop')}
                                >
                                  <StopCircle className="h-4 w-4" />
                                </Button>
                              </>
                            )}
                            {session.status === 'Pending' && (
                              <Button
                                size="sm"
                                variant="ghost"
                                onClick={() => handleSessionAction(session.id, 'resume')}
                              >
                                <Play className="h-4 w-4" />
                              </Button>
                            )}
                            {(session.status === 'Completed' || session.status === 'Failed') && (
                              <Button
                                size="sm"
                                variant="ghost"
                                onClick={() => navigate(`/sessions/${session.id}`)}
                              >
                                Details
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>
    </div>
  )
}