import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useApi } from '@/hooks/useApi'
import { toast } from 'sonner'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Loader2, Link2 } from 'lucide-react'

interface PinConnectProps {
  trigger?: React.ReactNode
}

export function PinConnect({ trigger }: PinConnectProps) {
  const navigate = useNavigate()
  const { post } = useApi()
  const [open, setOpen] = useState(false)
  const [pin, setPin] = useState('')
  const [isLoading, setIsLoading] = useState(false)

  const handleConnect = async () => {
    if (!pin || pin.length !== 6) {
      toast.error('Please enter a valid 6-digit PIN')
      return
    }

    try {
      setIsLoading(true)
      const response = await post('/api/sessions/join-by-pin', { pin })
      const sessionId = response.data.sessionId
      
      toast.success('Successfully connected to session')
      setOpen(false)
      setPin('')
      navigate(`/sessions/${sessionId}`)
    } catch (error: any) {
      console.error('Failed to connect with PIN:', error)
      toast.error(error.response?.data?.message || 'Invalid PIN or session not found')
    } finally {
      setIsLoading(false)
    }
  }

  const handlePinChange = (value: string) => {
    // Only allow digits and limit to 6 characters
    const cleaned = value.replace(/\D/g, '').slice(0, 6)
    setPin(cleaned)
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && pin.length === 6 && !isLoading) {
      handleConnect()
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        {trigger || (
          <Button variant="outline" size="sm">
            <Link2 className="mr-2 h-4 w-4" />
            Quick Connect
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Quick Connect with PIN</DialogTitle>
          <DialogDescription>
            Enter the 6-digit PIN displayed on the host device to quickly connect to a session.
          </DialogDescription>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="pin">Session PIN</Label>
            <Input
              id="pin"
              type="text"
              inputMode="numeric"
              pattern="[0-9]*"
              placeholder="123456"
              value={pin}
              onChange={(e) => handlePinChange(e.target.value)}
              onKeyPress={handleKeyPress}
              className="text-center text-2xl tracking-widest font-mono"
              autoFocus
              disabled={isLoading}
            />
            <p className="text-sm text-muted-foreground">
              The PIN is valid for 5 minutes after generation
            </p>
          </div>
        </div>
        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => {
              setOpen(false)
              setPin('')
            }}
            disabled={isLoading}
          >
            Cancel
          </Button>
          <Button
            onClick={handleConnect}
            disabled={pin.length !== 6 || isLoading}
          >
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Connecting...
              </>
            ) : (
              'Connect'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}