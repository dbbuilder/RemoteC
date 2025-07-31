import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Progress } from '@/components/ui/progress'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import {
  Fullscreen,
  Minimize2,
  RefreshCw,
  Camera,
  Keyboard,
  MousePointer,
  Volume2,
  VolumeX,
  Settings,
  Loader2,
} from 'lucide-react'

interface SessionControlProps {
  sessionId: string
  onFullscreen?: () => void
  isFullscreen?: boolean
}

type StreamQuality = 'auto' | 'high' | 'medium' | 'low'

export function SessionControl({
  sessionId,
  onFullscreen,
  isFullscreen = false,
}: SessionControlProps) {
  const { sendMessage, on, off } = useSignalR()
  const [controlMode, setControlMode] = useState<'view' | 'control'>('view')
  const [isConnecting, setIsConnecting] = useState(true)
  const [connectionError, setConnectionError] = useState<string | null>(null)
  const [audioEnabled, setAudioEnabled] = useState(true)
  const [quality, setQuality] = useState<StreamQuality>('auto')

  useEffect(() => {
    const handleConnectionStatus = (status: string) => {
      if (status === 'connected') {
        setIsConnecting(false)
        setConnectionError(null)
      } else if (status === 'error') {
        setIsConnecting(false)
        setConnectionError('Failed to connect to remote session')
      }
    }

    on('SessionConnectionStatus', handleConnectionStatus)

    // Join session
    sendMessage('JoinSession', sessionId).catch((error) => {
      setIsConnecting(false)
      setConnectionError(error.message)
    })

    return () => {
      off('SessionConnectionStatus', handleConnectionStatus)
      sendMessage('LeaveSession', sessionId).catch(console.error)
    }
  }, [sessionId, sendMessage, on, off])

  const handleControlModeChange = (value: string) => {
    if (value === 'view' || value === 'control') {
      setControlMode(value)
      sendMessage('SetControlMode', sessionId, value)
    }
  }

  const handleRefresh = () => {
    sendMessage('RefreshScreen', sessionId)
  }

  const handleScreenshot = () => {
    sendMessage('TakeScreenshot', sessionId)
  }

  const handleAudioToggle = () => {
    const newAudioState = !audioEnabled
    setAudioEnabled(newAudioState)
    sendMessage('SetAudioEnabled', sessionId, newAudioState)
  }

  const handleQualityChange = (newQuality: StreamQuality) => {
    setQuality(newQuality)
    sendMessage('SetStreamQuality', sessionId, newQuality)
  }

  if (isConnecting) {
    return (
      <Card>
        <CardContent className="flex items-center gap-4 p-4">
          <Loader2 className="h-4 w-4 animate-spin" />
          <div className="flex-1">
            <p className="text-sm">Connecting to remote session...</p>
            <Progress className="mt-2" role="progressbar" />
          </div>
        </CardContent>
      </Card>
    )
  }

  if (connectionError) {
    return (
      <Alert variant="destructive">
        <AlertDescription>{connectionError}</AlertDescription>
      </Alert>
    )
  }

  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between gap-4">
          {/* Control Mode Toggle */}
          <div className="flex items-center gap-4">
            <ToggleGroup
              type="single"
              value={controlMode}
              onValueChange={handleControlModeChange}
            >
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <ToggleGroupItem value="view" aria-label="View Only">
                      <MousePointer className="h-4 w-4" />
                    </ToggleGroupItem>
                  </TooltipTrigger>
                  <TooltipContent>View Only</TooltipContent>
                </Tooltip>
              </TooltipProvider>

              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <ToggleGroupItem value="control" aria-label="Full Control">
                      <Keyboard className="h-4 w-4" />
                    </ToggleGroupItem>
                  </TooltipTrigger>
                  <TooltipContent>Full Control</TooltipContent>
                </Tooltip>
              </TooltipProvider>
            </ToggleGroup>

            <span className="text-sm text-muted-foreground">
              {controlMode === 'view' ? 'View Only' : 'Full Control'}
            </span>
          </div>

          {/* Action Buttons */}
          <div className="flex items-center gap-2">
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    size="icon"
                    variant="ghost"
                    onClick={handleRefresh}
                    aria-label="Refresh Screen"
                  >
                    <RefreshCw className="h-4 w-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Refresh Screen</TooltipContent>
              </Tooltip>
            </TooltipProvider>

            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    size="icon"
                    variant="ghost"
                    onClick={handleScreenshot}
                    aria-label="Take Screenshot"
                  >
                    <Camera className="h-4 w-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Take Screenshot</TooltipContent>
              </Tooltip>
            </TooltipProvider>

            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    size="icon"
                    variant="ghost"
                    onClick={handleAudioToggle}
                    aria-label={audioEnabled ? 'Mute Audio' : 'Enable Audio'}
                  >
                    {audioEnabled ? (
                      <Volume2 className="h-4 w-4" />
                    ) : (
                      <VolumeX className="h-4 w-4" />
                    )}
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  {audioEnabled ? 'Mute Audio' : 'Enable Audio'}
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  size="icon"
                  variant="ghost"
                  aria-label="Quality Settings"
                >
                  <Settings className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => handleQualityChange('auto')}>
                  Auto
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleQualityChange('high')}>
                  High
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleQualityChange('medium')}>
                  Medium
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleQualityChange('low')}>
                  Low
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            {onFullscreen && (
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      size="icon"
                      variant="ghost"
                      onClick={onFullscreen}
                      aria-label={isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'}
                    >
                      {isFullscreen ? (
                        <Minimize2 className="h-4 w-4" />
                      ) : (
                        <Fullscreen className="h-4 w-4" />
                      )}
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>
                    {isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'}
                  </TooltipContent>
                </Tooltip>
              </TooltipProvider>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}