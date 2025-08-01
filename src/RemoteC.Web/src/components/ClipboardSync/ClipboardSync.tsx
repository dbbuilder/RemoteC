import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { toast } from 'sonner'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import {
  Clipboard,
  ClipboardCopy,
  ClipboardPaste,
  CheckCircle,
  AlertCircle,
  RefreshCw,
} from 'lucide-react'

interface ClipboardSyncProps {
  sessionId: string
}

export function ClipboardSync({ sessionId }: ClipboardSyncProps) {
  const { on, off, sendMessage } = useSignalR()
  const [autoSync, setAutoSync] = useState(true)
  const [localClipboard, setLocalClipboard] = useState('')
  const [remoteClipboard, setRemoteClipboard] = useState('')
  const [lastSyncTime, setLastSyncTime] = useState<Date | null>(null)
  const [syncStatus, setSyncStatus] = useState<'idle' | 'syncing' | 'success' | 'error'>('idle')

  useEffect(() => {
    // Listen for remote clipboard updates
    const handleRemoteClipboardUpdate = (data: { content: string; timestamp: string }) => {
      setRemoteClipboard(data.content)
      setLastSyncTime(new Date(data.timestamp))
      
      if (autoSync) {
        // Automatically copy to local clipboard
        copyToClipboard(data.content)
      }
      
      toast.info('Remote clipboard updated')
    }

    on('ClipboardUpdate', handleRemoteClipboardUpdate)

    return () => {
      off('ClipboardUpdate', handleRemoteClipboardUpdate)
    }
  }, [on, off, autoSync, sessionId])

  const copyToClipboard = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text)
      toast.success('Copied to clipboard')
    } catch (error) {
      console.error('Failed to copy to clipboard:', error)
      toast.error('Failed to copy to clipboard')
    }
  }

  const pasteFromClipboard = async () => {
    try {
      const text = await navigator.clipboard.readText()
      setLocalClipboard(text)
      
      if (autoSync) {
        await syncToRemote(text)
      }
    } catch (error) {
      console.error('Failed to read clipboard:', error)
      toast.error('Failed to read clipboard. Please check permissions.')
    }
  }

  const syncToRemote = async (content?: string) => {
    const textToSync = content || localClipboard
    if (!textToSync) {
      toast.warning('Nothing to sync')
      return
    }

    try {
      setSyncStatus('syncing')
      await sendMessage('SyncClipboard', sessionId, {
        content: textToSync,
        timestamp: new Date().toISOString(),
      })
      
      setSyncStatus('success')
      setLastSyncTime(new Date())
      toast.success('Clipboard synced to remote')
      
      setTimeout(() => setSyncStatus('idle'), 2000)
    } catch (error) {
      console.error('Failed to sync clipboard:', error)
      setSyncStatus('error')
      toast.error('Failed to sync clipboard')
      
      setTimeout(() => setSyncStatus('idle'), 2000)
    }
  }

  const syncFromRemote = async () => {
    try {
      setSyncStatus('syncing')
      await sendMessage('RequestClipboard', sessionId)
      
      // The response will come through the ClipboardUpdate event
      setSyncStatus('success')
      setTimeout(() => setSyncStatus('idle'), 2000)
    } catch (error) {
      console.error('Failed to request remote clipboard:', error)
      setSyncStatus('error')
      toast.error('Failed to get remote clipboard')
      
      setTimeout(() => setSyncStatus('idle'), 2000)
    }
  }

  const getSyncStatusIcon = () => {
    switch (syncStatus) {
      case 'syncing':
        return <RefreshCw className="h-4 w-4 animate-spin" />
      case 'success':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'error':
        return <AlertCircle className="h-4 w-4 text-red-500" />
      default:
        return <Clipboard className="h-4 w-4" />
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            {getSyncStatusIcon()}
            Clipboard Sync
          </CardTitle>
          <div className="flex items-center space-x-2">
            <Switch
              id="auto-sync"
              checked={autoSync}
              onCheckedChange={setAutoSync}
            />
            <Label htmlFor="auto-sync" className="text-sm">
              Auto-sync
            </Label>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Last Sync Info */}
        {lastSyncTime && (
          <div className="text-xs text-muted-foreground">
            Last synced: {lastSyncTime.toLocaleTimeString()}
          </div>
        )}

        {/* Local Clipboard */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="local-clipboard">Local Clipboard</Label>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="outline"
                onClick={pasteFromClipboard}
              >
                <ClipboardPaste className="h-3 w-3 mr-1" />
                Paste
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={() => syncToRemote()}
                disabled={!localClipboard || syncStatus === 'syncing'}
              >
                Sync to Remote
              </Button>
            </div>
          </div>
          <Textarea
            id="local-clipboard"
            value={localClipboard}
            onChange={(e) => setLocalClipboard(e.target.value)}
            placeholder="Paste or type content here..."
            rows={3}
            className="font-mono text-sm"
          />
        </div>

        {/* Remote Clipboard */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="remote-clipboard">Remote Clipboard</Label>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="outline"
                onClick={() => copyToClipboard(remoteClipboard)}
                disabled={!remoteClipboard}
              >
                <ClipboardCopy className="h-3 w-3 mr-1" />
                Copy
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={syncFromRemote}
                disabled={syncStatus === 'syncing'}
              >
                Sync from Remote
              </Button>
            </div>
          </div>
          <Textarea
            id="remote-clipboard"
            value={remoteClipboard}
            readOnly
            placeholder="Remote clipboard content will appear here..."
            rows={3}
            className="font-mono text-sm bg-muted"
          />
        </div>

        {/* Status Badge */}
        <div className="flex justify-center">
          <Badge variant={autoSync ? 'default' : 'secondary'}>
            {autoSync ? 'Auto-sync enabled' : 'Manual sync mode'}
          </Badge>
        </div>
      </CardContent>
    </Card>
  )
}