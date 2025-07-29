import { useRef, useEffect, useState, useCallback } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { Card } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Loader2 } from 'lucide-react'

interface RemoteScreenProps {
  sessionId: string
  width?: number
  height?: number
  onMouseMove?: (x: number, y: number) => void
  onMouseClick?: (x: number, y: number, button: number) => void
  onKeyPress?: (key: string, code: string) => void
  controlEnabled?: boolean
}

export function RemoteScreen({
  sessionId,
  width = 1920,
  height = 1080,
  onMouseMove,
  onMouseClick,
  onKeyPress,
  controlEnabled = false,
}: RemoteScreenProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const { on, off, sendMessage } = useSignalR()
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [fps, setFps] = useState(0)
  const frameCountRef = useRef(0)
  const lastFpsUpdateRef = useRef(Date.now())

  // Handle screen updates
  useEffect(() => {
    const handleScreenUpdate = (frameData: string) => {
      if (!canvasRef.current) return

      const ctx = canvasRef.current.getContext('2d')
      if (!ctx) return

      const img = new Image()
      img.onload = () => {
        ctx.drawImage(img, 0, 0, width, height)
        setIsLoading(false)

        // Update FPS counter
        frameCountRef.current++
        const now = Date.now()
        const elapsed = now - lastFpsUpdateRef.current
        if (elapsed >= 1000) {
          setFps(Math.round((frameCountRef.current * 1000) / elapsed))
          frameCountRef.current = 0
          lastFpsUpdateRef.current = now
        }
      }
      img.onerror = () => {
        setError('Failed to load screen data')
      }
      img.src = `data:image/jpeg;base64,${frameData}`
    }

    on('ScreenUpdate', handleScreenUpdate)
    return () => off('ScreenUpdate', handleScreenUpdate)
  }, [on, off, width, height])

  // Handle mouse events
  const handleMouseMove = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      if (!controlEnabled || !canvasRef.current) return

      const rect = canvasRef.current.getBoundingClientRect()
      const x = ((e.clientX - rect.left) / rect.width) * width
      const y = ((e.clientY - rect.top) / rect.height) * height

      sendMessage('MouseMove', sessionId, x, y)
      onMouseMove?.(x, y)
    },
    [controlEnabled, sessionId, width, height, sendMessage, onMouseMove]
  )

  const handleMouseClick = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      if (!controlEnabled || !canvasRef.current) return

      const rect = canvasRef.current.getBoundingClientRect()
      const x = ((e.clientX - rect.left) / rect.width) * width
      const y = ((e.clientY - rect.top) / rect.height) * height

      sendMessage('MouseClick', sessionId, x, y, e.button)
      onMouseClick?.(x, y, e.button)
    },
    [controlEnabled, sessionId, width, height, sendMessage, onMouseClick]
  )

  // Handle keyboard events
  useEffect(() => {
    if (!controlEnabled) return

    const handleKeyDown = (e: KeyboardEvent) => {
      e.preventDefault()
      sendMessage('KeyDown', sessionId, e.key, e.code)
      onKeyPress?.(e.key, e.code)
    }

    const handleKeyUp = (e: KeyboardEvent) => {
      e.preventDefault()
      sendMessage('KeyUp', sessionId, e.key, e.code)
    }

    window.addEventListener('keydown', handleKeyDown)
    window.addEventListener('keyup', handleKeyUp)

    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      window.removeEventListener('keyup', handleKeyUp)
    }
  }, [controlEnabled, sessionId, sendMessage, onKeyPress])

  // Prevent context menu on right click
  const handleContextMenu = useCallback(
    (e: React.MouseEvent) => {
      if (controlEnabled) {
        e.preventDefault()
      }
    },
    [controlEnabled]
  )

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertDescription>{error}</AlertDescription>
      </Alert>
    )
  }

  return (
    <Card
      className={`relative overflow-hidden bg-black ${
        controlEnabled ? 'cursor-crosshair' : 'cursor-default'
      }`}
      data-testid="remote-screen-container"
    >
      <canvas
        ref={canvasRef}
        width={width}
        height={height}
        className="w-full h-auto block"
        onMouseMove={handleMouseMove}
        onClick={handleMouseClick}
        onContextMenu={handleContextMenu}
        role="img"
        aria-label="Remote screen view"
      />

      {isLoading && (
        <div
          className="absolute inset-0 flex items-center justify-center bg-black/70"
          role="status"
        >
          <Loader2
            className="h-8 w-8 animate-spin text-primary"
            data-testid="loading-spinner"
          />
        </div>
      )}

      {/* FPS Counter */}
      <div className="absolute top-4 right-4 bg-black/60 text-white px-2 py-1 rounded text-sm">
        {fps} FPS
      </div>
    </Card>
  )
}