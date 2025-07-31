import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useUnifiedApi } from '@/hooks/useUnifiedApi'
import { Activity, Monitor, Server, Users, AlertCircle } from 'lucide-react'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'

export function Dashboard() {
  const api = useUnifiedApi()

  // Fetch dashboard statistics with auto-refresh every 5 seconds
  const { data: stats, error, isLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: async () => {
      try {
        console.log('Fetching dashboard stats...')
        const result = await api.get('/api/dashboard/stats')
        console.log('Dashboard stats received:', result)
        return result
      } catch (error) {
        console.error('Dashboard fetch error:', error)
        // Return mock data for demo purposes when API is not available (dev mode only)
        const isDev = import.meta.env.DEV
        if (isDev && error instanceof Error && error.message.includes('Network Error')) {
          console.log('Using mock data for dashboard (development mode)')
          const { mockDashboardStats } = await import('@/mocks/mockApi')
          return mockDashboardStats
        }
        throw error
      }
    },
    refetchInterval: 5000, // Refresh every 5 seconds
    refetchIntervalInBackground: true,
    retry: 1,
  })

  // Mock data for the chart
  const sessionData = [
    { name: 'Mon', sessions: 65 },
    { name: 'Tue', sessions: 59 },
    { name: 'Wed', sessions: 80 },
    { name: 'Thu', sessions: 81 },
    { name: 'Fri', sessions: 56 },
    { name: 'Sat', sessions: 40 },
    { name: 'Sun', sessions: 30 },
  ]

  const statCards = [
    {
      title: 'Active Sessions',
      value: stats?.activeSessions || 0,
      description: 'Currently active remote sessions',
      icon: Monitor,
      trend: '+12%',
    },
    {
      title: 'Online Devices',
      value: stats?.onlineDevices || 0,
      description: 'Devices currently online',
      icon: Server,
      trend: '+5%',
    },
    {
      title: 'Total Users',
      value: stats?.totalUsers || 0,
      description: 'Registered users in the system',
      icon: Users,
      trend: '+3%',
    },
    {
      title: 'System Health',
      value: stats?.systemHealth || 'Good',
      description: 'Overall system status',
      icon: Activity,
      trend: 'Stable',
    },
  ]

  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">
            System overview and statistics
          </p>
        </div>
        
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error loading dashboard data</AlertTitle>
          <AlertDescription>
            {error instanceof Error ? error.message : 'Failed to load dashboard statistics. Please check if the backend API is running on port 17001.'}
          </AlertDescription>
        </Alert>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">
          System overview and statistics
        </p>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {statCards.map((stat) => {
          const Icon = stat.icon
          return (
            <Card key={stat.title}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  {stat.title}
                </CardTitle>
                <Icon className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {isLoading ? '...' : stat.value}
                </div>
                <p className="text-xs text-muted-foreground">
                  {stat.description}
                </p>
                <Badge variant="secondary" className="mt-2">
                  {stat.trend}
                </Badge>
              </CardContent>
            </Card>
          )
        })}
      </div>

      {/* Session Activity Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Weekly Session Activity</CardTitle>
          <CardDescription>
            Number of remote sessions per day
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={sessionData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="sessions" fill="hsl(var(--primary))" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </CardContent>
      </Card>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
          <CardDescription>
            Latest system events and user actions
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[
              { user: 'John Doe', action: 'Started remote session', device: 'DESKTOP-ABC123', time: '2 minutes ago' },
              { user: 'Jane Smith', action: 'Ended remote session', device: 'LAPTOP-XYZ789', time: '5 minutes ago' },
              { user: 'Admin', action: 'Updated system settings', device: 'SERVER-001', time: '10 minutes ago' },
              { user: 'Mike Johnson', action: 'Connected to device', device: 'WORKSTATION-456', time: '15 minutes ago' },
            ].map((activity, index) => (
              <div key={index} className="flex items-center justify-between border-b pb-3 last:border-0">
                <div>
                  <p className="font-medium">{activity.user}</p>
                  <p className="text-sm text-muted-foreground">{activity.action} on {activity.device}</p>
                </div>
                <p className="text-sm text-muted-foreground">{activity.time}</p>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}