import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { useUnifiedApi } from '@/hooks/useUnifiedApi'
import { User, PagedResult } from '@/types'
import { MoreHorizontal, UserPlus, Shield, UserCheck, UserX, Search, Mail, Calendar, Users } from 'lucide-react'
import { formatDistanceToNow, format } from 'date-fns'

export function UsersPage() {
  const api = useUnifiedApi()
  const [searchTerm, setSearchTerm] = useState('')

  // Fetch users with auto-refresh every 10 seconds
  const { data: usersData, isLoading, refetch } = useQuery<PagedResult<User>>({
    queryKey: ['users'],
    queryFn: () => api.get('/api/users?pageSize=100'),
    refetchInterval: 10000, // Refresh every 10 seconds
    refetchIntervalInBackground: true,
  })

  // Fetch user statistics with auto-refresh
  const { data: stats } = useQuery({
    queryKey: ['user-stats'],
    queryFn: () => api.get('/api/users/stats'),
    refetchInterval: 10000,
    refetchIntervalInBackground: true,
  })

  const users = usersData?.items || []
  const filteredUsers = users.filter(user => {
    if (!searchTerm) return true
    const searchLower = searchTerm.toLowerCase()
    return (
      user.displayName.toLowerCase().includes(searchLower) ||
      user.email.toLowerCase().includes(searchLower) ||
      user.id.toLowerCase().includes(searchLower) ||
      user.roles.some(role => role.toLowerCase().includes(searchLower))
    )
  })

  const handleUserAction = async (userId: string, action: 'activate' | 'deactivate' | 'resetPassword') => {
    try {
      await api.post(`/api/users/${userId}/${action}`)
      refetch()
    } catch (error) {
      console.error(`Failed to ${action} user:`, error)
    }
  }

  const getRoleBadgeVariant = (role: string) => {
    switch (role.toLowerCase()) {
      case 'admin': return 'destructive'
      case 'operator': return 'default'
      case 'viewer': return 'secondary'
      default: return 'outline'
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Users</h1>
          <p className="text-muted-foreground">
            Manage user accounts, roles, and permissions
          </p>
        </div>
        <Button>
          <UserPlus className="h-4 w-4 mr-2" />
          Add User
        </Button>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Users</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.totalUsers || users.length}</div>
            <p className="text-xs text-muted-foreground">Registered accounts</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Users</CardTitle>
            <UserCheck className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {stats?.activeUsers || users.filter(u => u.isActive).length}
            </div>
            <p className="text-xs text-muted-foreground">Can access system</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Administrators</CardTitle>
            <Shield className="h-4 w-4 text-purple-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {stats?.adminCount || users.filter(u => u.roles.includes('Admin')).length}
            </div>
            <p className="text-xs text-muted-foreground">Full system access</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Today</CardTitle>
            <Calendar className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.activeToday || 0}</div>
            <p className="text-xs text-muted-foreground">Logged in today</p>
          </CardContent>
        </Card>
      </div>

      {/* Users Table */}
      <Card>
        <CardHeader>
          <div className="flex justify-between items-center">
            <div>
              <CardTitle>All Users</CardTitle>
              <CardDescription>Manage user accounts and access levels</CardDescription>
            </div>
            <div className="relative">
              <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search users..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-8 w-64"
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8 text-muted-foreground">
              Loading users...
            </div>
          ) : filteredUsers.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No users found
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Roles</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Last Login</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredUsers.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                          <span className="text-sm font-medium">
                            {user.displayName.charAt(0).toUpperCase()}
                          </span>
                        </div>
                        <div>
                          <div className="font-medium">{user.displayName}</div>
                          <div className="text-xs text-muted-foreground">ID: {user.id.substring(0, 8)}...</div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Mail className="h-3 w-3 text-muted-foreground" />
                        {user.email}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1 flex-wrap">
                        {user.roles.map((role) => (
                          <Badge key={role} variant={getRoleBadgeVariant(role)}>
                            {role}
                          </Badge>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={user.isActive ? 'default' : 'secondary'}>
                        {user.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {user.lastLoginAt ? (
                        <div>
                          <div className="text-sm">
                            {format(new Date(user.lastLoginAt), 'MMM d, HH:mm')}
                          </div>
                          <div className="text-xs text-muted-foreground">
                            {formatDistanceToNow(new Date(user.lastLoginAt), { addSuffix: true })}
                          </div>
                        </div>
                      ) : (
                        <span className="text-muted-foreground">Never</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="text-sm">
                        {format(new Date(user.createdAt), 'MMM d, yyyy')}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" className="h-8 w-8 p-0">
                            <span className="sr-only">Open menu</span>
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuLabel>Actions</DropdownMenuLabel>
                          <DropdownMenuItem>Edit User</DropdownMenuItem>
                          <DropdownMenuItem>Change Roles</DropdownMenuItem>
                          <DropdownMenuSeparator />
                          <DropdownMenuItem onClick={() => handleUserAction(user.id, 'resetPassword')}>
                            Reset Password
                          </DropdownMenuItem>
                          {user.isActive ? (
                            <DropdownMenuItem 
                              onClick={() => handleUserAction(user.id, 'deactivate')}
                              className="text-destructive"
                            >
                              Deactivate User
                            </DropdownMenuItem>
                          ) : (
                            <DropdownMenuItem onClick={() => handleUserAction(user.id, 'activate')}>
                              Activate User
                            </DropdownMenuItem>
                          )}
                        </DropdownMenuContent>
                      </DropdownMenu>
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