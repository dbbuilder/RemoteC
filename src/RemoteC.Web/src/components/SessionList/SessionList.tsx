import { formatDistanceToNow, differenceInMinutes } from 'date-fns'
import { Session } from '@/types'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Eye, StopCircle, Trash2, Key, Monitor } from 'lucide-react'

interface SessionListProps {
  sessions: Session[]
  onView: (session: Session) => void
  onStop: (session: Session) => void
  onDelete: (session: Session) => void
  onGeneratePin: (session: Session) => void
  isLoading?: boolean
}

const getStatusColor = (status: Session['status']) => {
  switch (status) {
    case 'Active':
      return 'bg-green-100 text-green-800 hover:bg-green-200'
    case 'Pending':
      return 'bg-yellow-100 text-yellow-800 hover:bg-yellow-200'
    case 'Completed':
      return 'bg-gray-100 text-gray-800 hover:bg-gray-200'
    case 'Failed':
      return 'bg-red-100 text-red-800 hover:bg-red-200'
    default:
      return 'bg-gray-100 text-gray-800 hover:bg-gray-200'
  }
}

const formatDuration = (startedAt: string, endedAt?: string) => {
  const start = new Date(startedAt)
  const end = endedAt ? new Date(endedAt) : new Date()
  const minutes = differenceInMinutes(end, start)
  return `${minutes} min`
}

export function SessionList({
  sessions,
  onView,
  onStop,
  onDelete,
  onGeneratePin,
  isLoading = false,
}: SessionListProps) {
  if (isLoading) {
    return (
      <div className="space-y-4" role="status">
        {[1, 2, 3].map((i) => (
          <Card key={i}>
            <CardContent className="p-6">
              <div className="space-y-3">
                <Skeleton className="h-4 w-[250px]" />
                <Skeleton className="h-4 w-[200px]" />
                <Skeleton className="h-4 w-[150px]" />
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    )
  }

  if (sessions.length === 0) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center justify-center py-12">
          <Monitor className="h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-lg font-semibold">No active sessions</h3>
          <p className="text-sm text-muted-foreground mt-1">
            Start a new remote control session to see it here.
          </p>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Device</TableHead>
            <TableHead>User</TableHead>
            <TableHead>Started</TableHead>
            <TableHead>Duration</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>PIN</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {sessions.map((session) => (
            <TableRow key={session.id}>
              <TableCell className="font-medium">
                {session.device?.name || 'Unknown Device'}
              </TableCell>
              <TableCell>{session.user?.displayName || 'Unknown User'}</TableCell>
              <TableCell>
                {formatDistanceToNow(new Date(session.startedAt), { addSuffix: true })}
              </TableCell>
              <TableCell>{formatDuration(session.startedAt, session.endedAt)}</TableCell>
              <TableCell>
                <Badge className={getStatusColor(session.status)} variant="secondary">
                  {session.status}
                </Badge>
              </TableCell>
              <TableCell>
                {session.sessionPin ? (
                  <span className="font-mono text-sm">PIN: {session.sessionPin}</span>
                ) : (
                  session.status === 'Active' && (
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => onGeneratePin(session)}
                      aria-label="Generate PIN"
                    >
                      <Key className="h-4 w-4" />
                    </Button>
                  )
                )}
              </TableCell>
              <TableCell className="text-right">
                <div className="flex justify-end gap-2">
                  <Button
                    size="icon"
                    variant="ghost"
                    onClick={() => onView(session)}
                    aria-label="View session"
                  >
                    <Eye className="h-4 w-4" />
                  </Button>
                  {session.status === 'Active' ? (
                    <Button
                      size="icon"
                      variant="ghost"
                      onClick={() => onStop(session)}
                      aria-label="Stop session"
                      className="text-destructive hover:text-destructive"
                    >
                      <StopCircle className="h-4 w-4" />
                    </Button>
                  ) : (
                    <Button
                      size="icon"
                      variant="ghost"
                      onClick={() => onDelete(session)}
                      aria-label="Delete session"
                      className="text-destructive hover:text-destructive"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}