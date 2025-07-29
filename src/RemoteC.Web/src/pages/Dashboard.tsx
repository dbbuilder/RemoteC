import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useApi } from '@/hooks/useApi'
import { Activity, Monitor, Server, Users } from 'lucide-react'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'

export function Dashboard() {
  const api = useApi()

  // Fetch dashboard statistics
  const { data: stats } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: () => api.get('/api/dashboard/stats'),
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
      title: 'Session Duration',
      value: stats?.avgSessionDuration || '0m',
      description: 'Average session duration today',
      icon: Activity,
      trend: '-2%',
    },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">
          Welcome back! Here's an overview of your remote control system.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {statCards.map((stat) => (
          <Card key={stat.title}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">
                {stat.title}
              </CardTitle>
              <stat.icon className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stat.value}</div>
              <p className="text-xs text-muted-foreground">
                {stat.description}
              </p>
              <div className="mt-2">
                <Badge
                  variant={stat.trend.startsWith('+') ? 'default' : 'secondary'}
                  className="text-xs"
                >
                  {stat.trend} from last week
                </Badge>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
        <Card className="col-span-4">
          <CardHeader>
            <CardTitle>Session Activity</CardTitle>
            <CardDescription>
              Number of remote sessions initiated this week
            </CardDescription>
          </CardHeader>
          <CardContent className="pl-2">
            <ResponsiveContainer width="100%" height={350}>
              <BarChart data={sessionData}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                <XAxis
                  dataKey="name"
                  stroke="#888888"
                  fontSize={12}
                  tickLine={false}
                  axisLine={false}
                />
                <YAxis
                  stroke="#888888"
                  fontSize={12}
                  tickLine={false}
                  axisLine={false}
                  tickFormatter={(value) => `${value}`}
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: 'hsl(var(--background))',
                    border: '1px solid hsl(var(--border))',
                    borderRadius: '6px',
                  }}
                />
                <Bar
                  dataKey="sessions"
                  fill="hsl(var(--primary))"
                  radius={[4, 4, 0, 0]}
                />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="col-span-3">
          <CardHeader>
            <CardTitle>Recent Activity</CardTitle>
            <CardDescription>
              Latest actions performed in the system
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[
                {
                  user: 'John Doe',
                  action: 'Started remote session',
                  device: 'DESKTOP-ABC123',
                  time: '2 minutes ago',
                },
                {
                  user: 'Jane Smith',
                  action: 'Ended remote session',
                  device: 'LAPTOP-XYZ789',
                  time: '5 minutes ago',
                },
                {
                  user: 'Mike Johnson',
                  action: 'Transferred file',
                  device: 'SERVER-001',
                  time: '10 minutes ago',
                },
                {
                  user: 'Sarah Wilson',
                  action: 'Generated session PIN',
                  device: 'WORKSTATION-02',
                  time: '15 minutes ago',
                },
              ].map((activity, index) => (
                <div key={index} className="flex items-center gap-4">
                  <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10">
                    <span className="text-sm font-medium">
                      {activity.user.charAt(0)}
                    </span>
                  </div>
                  <div className="flex-1 space-y-1">
                    <p className="text-sm">
                      <span className="font-medium">{activity.user}</span>{' '}
                      {activity.action}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {activity.device} • {activity.time}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}