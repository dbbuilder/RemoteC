import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useUnifiedApi } from '@/hooks/useUnifiedApi'
import { AuditLog, PagedResult } from '@/types'
import { Search, Download, Filter, Calendar, User, Activity, AlertCircle } from 'lucide-react'
import { formatDistanceToNow, format } from 'date-fns'

export function AuditLogsPage() {
  const api = useUnifiedApi()
  const [searchTerm, setSearchTerm] = useState('')
  const [severityFilter] = useState<string>('All')
  const [actionFilter, setActionFilter] = useState<string>('All')
  const [dateRange, setDateRange] = useState<string>('7days')

  // Fetch audit logs with filters
  const { data: logsData, isLoading, refetch, isFetching } = useQuery<PagedResult<AuditLog>>({
    queryKey: ['audit-logs', severityFilter, actionFilter, dateRange, searchTerm],
    queryFn: () => {
      const params = new URLSearchParams({ 
        pageSize: '100',
        orderBy: 'timestamp',
        isDescending: 'true'
      })
      
      if (severityFilter !== 'All') {
        params.append('severity', severityFilter)
      }
      if (actionFilter !== 'All') {
        params.append('action', actionFilter)
      }
      if (searchTerm) {
        params.append('search', searchTerm)
      }
      
      // Add date range filter
      const now = new Date()
      let startDate: Date
      switch(dateRange) {
        case '1hour':
          startDate = new Date(now.getTime() - 60 * 60 * 1000)
          break
        case '24hours':
          startDate = new Date(now.getTime() - 24 * 60 * 60 * 1000)
          break
        case '7days':
          startDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000)
          break
        case '30days':
          startDate = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000)
          break
        default:
          startDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000)
      }
      params.append('startDate', startDate.toISOString())
      
      return api.get(`/api/audit?${params}`)
    },
    refetchInterval: 30000, // Refresh every 30 seconds
  })

  const logs = logsData?.items || []


  const getActionIcon = (action: string) => {
    if (action.includes('Login')) return User
    if (action.includes('Session')) return Activity
    if (action.includes('Error') || action.includes('Failed')) return AlertCircle
    return Activity
  }

  const handleExport = async () => {
    try {
      const response = await api.get('/api/audit/export', { 
        responseType: 'blob',
        params: { format: 'csv', dateRange }
      })
      
      // Create download link
      const url = window.URL.createObjectURL(new Blob([response]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `audit-logs-${format(new Date(), 'yyyy-MM-dd')}.csv`)
      document.body.appendChild(link)
      link.click()
      link.remove()
    } catch (error) {
      console.error('Export failed:', error)
    }
  }

  // Get unique actions for filter
  const uniqueActions = [...new Set(logs.map(log => log.action))].sort()

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Audit Logs</h1>
          <p className="text-muted-foreground">
            System activity monitoring and compliance audit trail
          </p>
        </div>
        <Button onClick={handleExport} variant="outline">
          <Download className="h-4 w-4 mr-2" />
          Export CSV
        </Button>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Events</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{logsData?.totalCount || 0}</div>
            <p className="text-xs text-muted-foreground">In selected period</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Critical Events</CardTitle>
            <AlertCircle className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {logs.filter(l => l.action.includes('Failed') || l.action.includes('Error')).length}
            </div>
            <p className="text-xs text-muted-foreground">Errors and failures</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">User Actions</CardTitle>
            <User className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {logs.filter(l => l.action.includes('User') || l.action.includes('Login')).length}
            </div>
            <p className="text-xs text-muted-foreground">Authentication events</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Recent Activity</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {logs.filter(l => new Date(l.timestamp) > new Date(Date.now() - 60 * 60 * 1000)).length}
            </div>
            <p className="text-xs text-muted-foreground">Last hour</p>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
          <CardDescription>Filter audit logs by various criteria</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4 flex-wrap">
            <div className="flex-1 min-w-[200px]">
              <div className="relative">
                <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search logs..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-8"
                />
              </div>
            </div>
            
            <Select value={dateRange} onValueChange={setDateRange}>
              <SelectTrigger className="w-[150px]">
                <Calendar className="h-4 w-4 mr-2" />
                <SelectValue placeholder="Time range" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="1hour">Last Hour</SelectItem>
                <SelectItem value="24hours">Last 24 Hours</SelectItem>
                <SelectItem value="7days">Last 7 Days</SelectItem>
                <SelectItem value="30days">Last 30 Days</SelectItem>
              </SelectContent>
            </Select>

            <Select value={actionFilter} onValueChange={setActionFilter}>
              <SelectTrigger className="w-[180px]">
                <Filter className="h-4 w-4 mr-2" />
                <SelectValue placeholder="Filter by action" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="All">All Actions</SelectItem>
                {uniqueActions.map(action => (
                  <SelectItem key={action} value={action}>{action}</SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Button onClick={() => refetch()} variant="outline" disabled={isFetching}>
              {isFetching ? 'Refreshing...' : 'Refresh'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Logs Table */}
      <Card>
        <CardHeader>
          <CardTitle>Audit Trail</CardTitle>
          <CardDescription>Detailed system activity log</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8 text-muted-foreground">
              Loading audit logs...
            </div>
          ) : logs.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No audit logs found for the selected filters
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Timestamp</TableHead>
                  <TableHead>User</TableHead>
                  <TableHead>Action</TableHead>
                  <TableHead>Entity</TableHead>
                  <TableHead>IP Address</TableHead>
                  <TableHead>Details</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {logs.map((log) => {
                  const ActionIcon = getActionIcon(log.action)
                  return (
                    <TableRow key={log.id}>
                      <TableCell>
                        <div>
                          <div className="text-sm">
                            {format(new Date(log.timestamp), 'MMM d, HH:mm:ss')}
                          </div>
                          <div className="text-xs text-muted-foreground">
                            {formatDistanceToNow(new Date(log.timestamp), { addSuffix: true })}
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div>
                          <div className="font-medium">{log.user?.displayName || 'System'}</div>
                          <div className="text-xs text-muted-foreground">{log.user?.email || log.userId}</div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <ActionIcon className="h-4 w-4 text-muted-foreground" />
                          <span>{log.action}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="text-sm">
                          <div>{log.entityType}</div>
                          <div className="text-xs text-muted-foreground font-mono">
                            {log.entityId?.substring(0, 8)}...
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <code className="text-xs">{log.ipAddress || 'N/A'}</code>
                      </TableCell>
                      <TableCell>
                        {(log.oldValues || log.newValues) && (
                          <Button size="sm" variant="ghost">
                            View Changes
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}