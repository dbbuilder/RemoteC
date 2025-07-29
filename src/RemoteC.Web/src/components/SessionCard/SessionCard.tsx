import { formatDistance } from 'date-fns'
import { Session } from '@/types'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { 
  Monitor, 
  User, 
  Clock, 
  Key, 
  Wifi,
  HardDrive,
  Activity
} from 'lucide-react'

interface SessionCardProps {
  session: Session
  onConnect?: () => void
  onStop?: () => void
  onGeneratePin?: () => void
  showActions?: boolean
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

export function SessionCard({
  session,
  onConnect,
  onStop,
  onGeneratePin,
  showActions = true,
}: SessionCardProps) {
  const duration = session.endedAt
    ? formatDistance(new Date(session.startedAt), new Date(session.endedAt))
    : formatDistance(new Date(session.startedAt), new Date())

  const isActive = session.status === 'Active'

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="text-xl">Session {session.id}</CardTitle>
          <Badge className={getStatusColor(session.status)} variant="secondary">
            {session.status}
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Device Information */}
        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Monitor className="h-4 w-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">Device</p>
                <p className="text-sm text-muted-foreground">
                  {session.device?.name || 'Unknown Device'}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <HardDrive className="h-4 w-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">Operating System</p>
                <p className="text-sm text-muted-foreground">
                  {session.device?.operatingSystem || 'Unknown OS'}
                </p>
              </div>
            </div>

            {session.device?.ipAddress && (
              <div className="flex items-center gap-2">
                <Wifi className="h-4 w-4 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium">IP Address</p>
                  <p className="text-sm text-muted-foreground">
                    {session.device.ipAddress}
                  </p>
                </div>
              </div>
            )}
          </div>

          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <User className="h-4 w-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">User</p>
                <p className="text-sm text-muted-foreground">
                  {session.user?.displayName || session.user?.email || 'Unknown User'}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Clock className="h-4 w-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">Duration</p>
                <p className="text-sm text-muted-foreground">
                  {duration}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Activity className="h-4 w-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">Started</p>
                <p className="text-sm text-muted-foreground">
                  {formatDistance(new Date(session.startedAt), new Date(), { addSuffix: true })}
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Session PIN */}
        {session.sessionPin && (
          <>
            <Separator />
            <div className="flex items-center justify-center p-4 bg-muted rounded-lg">
              <div className="text-center">
                <div className="flex items-center gap-2 justify-center mb-1">
                  <Key className="h-4 w-4 text-muted-foreground" />
                  <p className="text-sm font-medium">Session PIN</p>
                </div>
                <p className="text-2xl font-mono font-bold">{session.sessionPin}</p>
              </div>
            </div>
          </>
        )}

        {/* Metadata */}
        {session.metadata && (
          <>
            <Separator />
            <div>
              <p className="text-sm font-medium mb-2">Additional Information</p>
              <pre className="text-xs text-muted-foreground bg-muted p-2 rounded overflow-auto">
                {JSON.stringify(session.metadata, null, 2)}
              </pre>
            </div>
          </>
        )}
      </CardContent>

      {showActions && (
        <CardFooter className="flex gap-2">
          {isActive && onConnect && (
            <Button onClick={onConnect}>
              Connect
            </Button>
          )}
          {isActive && onStop && (
            <Button onClick={onStop} variant="destructive">
              Stop Session
            </Button>
          )}
          {isActive && onGeneratePin && !session.sessionPin && (
            <Button onClick={onGeneratePin} variant="secondary">
              Generate PIN
            </Button>
          )}
        </CardFooter>
      )}
    </Card>
  )
}