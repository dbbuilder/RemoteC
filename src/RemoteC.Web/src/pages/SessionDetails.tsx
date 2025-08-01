import { useState, useEffect, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useApi } from '@/hooks/useApi'
import { useSignalR } from '@/contexts/SignalRContext'
import { toast } from 'sonner'
import { RemoteScreen } from '@/components/RemoteScreen/RemoteScreen'
import { SessionControl } from '@/components/SessionControl/SessionControl'
import { FileTransfer } from '@/components/FileTransfer/FileTransfer'
import { ClipboardSync } from '@/components/ClipboardSync/ClipboardSync'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  ArrowLeft,
  Monitor,
  Clipboard,
  FileText,
  Video,
  Activity,
  Wifi,
  WifiOff,
  Clock,
  User,
} from 'lucide-react'

interface SessionDetailsData {
  id: string
  hostName: string
  userName: string
  status: 'active' | 'disconnected' | 'paused'
  startedAt: string
  duration: number
  monitors: Array<{
    id: string
    name: string
    isPrimary: boolean
    width: number
    height: number
  }>
  metrics: {
    latency: number
    bandwidth: number
    frameRate: number
    quality: string
  }
}

export function SessionDetails() {
  const { id: sessionId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { get } = useApi()
  const { isConnected } = useSignalR()
  const [session, setSession] = useState<SessionDetailsData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const [selectedMonitor, setSelectedMonitor] = useState<string>('')
  const [controlEnabled, setControlEnabled] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  // Fetch session details
  useEffect(() => {
    const fetchSession = async () => {
      if (!sessionId) return
      
      try {
        setIsLoading(true)
        const response = await get(`/api/sessions/${sessionId}`)
        setSession(response.data)
        if (response.data.monitors?.length > 0) {
          const primary = response.data.monitors.find((m: any) => m.isPrimary)
          setSelectedMonitor(primary?.id || response.data.monitors[0].id)
        }
      } catch (err) {
        console.error('Failed to fetch session:', err)
        setError('Failed to load session details')
        toast.error('Failed to load session details')
      } finally {
        setIsLoading(false)
      }
    }

    fetchSession()
  }, [sessionId, get])

  // Handle fullscreen
  const handleFullscreen = () => {
    if (!containerRef.current) return

    if (!isFullscreen) {
      containerRef.current.requestFullscreen()
      setIsFullscreen(true)
    } else {
      document.exitFullscreen()
      setIsFullscreen(false)
    }
  }

  // Handle monitor selection
  const handleMonitorChange = (monitorId: string) => {
    setSelectedMonitor(monitorId)
    toast.success('Switched to monitor: ' + session?.monitors.find(m => m.id === monitorId)?.name)
  }

  if (isLoading) {
    return (
      <div className="container mx-auto py-6 space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10" />
          <Skeleton className="h-10 w-64" />
        </div>
        <div className="grid gap-6 md:grid-cols-3">
          <div className="md:col-span-2">
            <Skeleton className="h-[600px] w-full" />
          </div>
          <div>
            <Skeleton className="h-[600px] w-full" />
          </div>
        </div>
      </div>
    )
  }

  if (error || !session) {
    return (
      <div className="container mx-auto py-6">
        <Alert variant="destructive">
          <AlertDescription>{error || 'Session not found'}</AlertDescription>
        </Alert>
        <Button onClick={() => navigate('/sessions')} className="mt-4">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to Sessions
        </Button>
      </div>
    )
  }

  return (
    <div className="container mx-auto py-6 space-y-6" ref={containerRef}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            onClick={() => navigate('/sessions')}
            className="p-2"
          >
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold">{session.hostName}</h1>
            <div className="flex items-center gap-4 text-sm text-muted-foreground">
              <span className="flex items-center gap-1">
                <User className="h-4 w-4" />
                {session.userName}
              </span>
              <span className="flex items-center gap-1">
                <Clock className="h-4 w-4" />
                Started {new Date(session.startedAt).toLocaleTimeString()}
              </span>
              <Badge variant={session.status === 'active' ? 'default' : 'secondary'}>
                {session.status}
              </Badge>
            </div>
          </div>
        </div>

        {/* Connection Status */}
        <div className="flex items-center gap-2">
          {isConnected ? (
            <Badge variant="outline" className="gap-1">
              <Wifi className="h-3 w-3" />
              Connected
            </Badge>
          ) : (
            <Badge variant="destructive" className="gap-1">
              <WifiOff className="h-3 w-3" />
              Disconnected
            </Badge>
          )}
        </div>
      </div>

      {/* Main Content */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Remote Screen */}
        <div className="lg:col-span-2 space-y-4">
          {/* Monitor Selection */}
          {session.monitors.length > 1 && (
            <Card>
              <CardContent className="p-4">
                <div className="flex items-center gap-4">
                  <Monitor className="h-5 w-5 text-muted-foreground" />
                  <Select value={selectedMonitor} onValueChange={handleMonitorChange}>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select monitor" />
                    </SelectTrigger>
                    <SelectContent>
                      {session.monitors.map((monitor) => (
                        <SelectItem key={monitor.id} value={monitor.id}>
                          {monitor.name} ({monitor.width}x{monitor.height})
                          {monitor.isPrimary && ' - Primary'}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Session Control */}
          <SessionControl
            sessionId={sessionId!}
            onFullscreen={handleFullscreen}
            isFullscreen={isFullscreen}
          />

          {/* Remote Screen */}
          <RemoteScreen
            sessionId={sessionId!}
            width={session.monitors.find(m => m.id === selectedMonitor)?.width || 1920}
            height={session.monitors.find(m => m.id === selectedMonitor)?.height || 1080}
            controlEnabled={controlEnabled}
          />
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          {/* Session Metrics */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-5 w-5" />
                Performance Metrics
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Latency</span>
                <span className="text-sm font-medium">{session.metrics.latency}ms</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Bandwidth</span>
                <span className="text-sm font-medium">
                  {(session.metrics.bandwidth / 1024 / 1024).toFixed(2)} Mbps
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Frame Rate</span>
                <span className="text-sm font-medium">{session.metrics.frameRate} FPS</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Quality</span>
                <Badge variant="outline">{session.metrics.quality}</Badge>
              </div>
            </CardContent>
          </Card>

          {/* Additional Features */}
          <Tabs defaultValue="clipboard" className="w-full">
            <TabsList className="grid grid-cols-3 w-full">
              <TabsTrigger value="clipboard">
                <Clipboard className="h-4 w-4" />
              </TabsTrigger>
              <TabsTrigger value="files">
                <FileText className="h-4 w-4" />
              </TabsTrigger>
              <TabsTrigger value="recording">
                <Video className="h-4 w-4" />
              </TabsTrigger>
            </TabsList>
            <TabsContent value="clipboard">
              <ClipboardSync sessionId={sessionId!} />
            </TabsContent>
            <TabsContent value="files">
              <FileTransfer sessionId={sessionId!} />
            </TabsContent>
            <TabsContent value="recording">
              <Card>
                <CardHeader>
                  <CardTitle>Session Recording</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-muted-foreground mb-4">
                    Record this session for later playback
                  </p>
                  <Button className="w-full" variant="destructive">
                    Start Recording
                  </Button>
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </div>
  )
}