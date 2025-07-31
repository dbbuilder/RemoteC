import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { AlertCircle, Monitor, Shield, Zap } from 'lucide-react'
import { useDevAuth } from '@/contexts/DevAuthContext'

export function DevLoginPage() {
  const navigate = useNavigate()
  const { login } = useDevAuth()
  const [username, setUsername] = useState('admin')
  const [password, setPassword] = useState('admin')
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      setIsLoading(true)
      setError(null)
      
      // In development mode, any username/password works
      await login(username, password)
      navigate('/dashboard')
    } catch (error: any) {
      console.error('Login failed:', error)
      setError(error.message || 'Login failed. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background">
      <div className="w-full max-w-md space-y-8 px-4">
        <div className="text-center">
          <h1 className="text-4xl font-bold tracking-tight">RemoteC</h1>
          <p className="mt-2 text-lg text-muted-foreground">
            Enterprise Remote Control Solution
          </p>
        </div>

        <Alert className="border-yellow-500 bg-yellow-50 dark:bg-yellow-950">
          <AlertCircle className="h-4 w-4 text-yellow-600" />
          <AlertTitle>Development Mode</AlertTitle>
          <AlertDescription>
            Authentication is simplified. Use any username/password to login.
          </AlertDescription>
        </Alert>

        <Card>
          <form onSubmit={handleLogin}>
            <CardHeader>
              <CardTitle className="text-2xl">Sign in</CardTitle>
              <CardDescription>
                Enter your credentials to access RemoteC
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {error && (
                <Alert variant="destructive">
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}
              
              <div className="space-y-2">
                <Label htmlFor="username">Username</Label>
                <Input
                  id="username"
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  placeholder="Enter any username"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="password">Password</Label>
                <Input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Enter any password"
                  required
                />
              </div>
            </CardContent>
            <CardFooter>
              <Button
                type="submit"
                className="w-full"
                size="lg"
                disabled={isLoading}
              >
                {isLoading ? (
                  <div className="flex items-center gap-2">
                    <div className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                    Signing in...
                  </div>
                ) : (
                  'Sign in'
                )}
              </Button>
            </CardFooter>
          </form>
        </Card>

        <div className="grid grid-cols-3 gap-4 text-center">
          <div className="space-y-2">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
              <Monitor className="h-6 w-6 text-primary" />
            </div>
            <h3 className="text-sm font-medium">Remote Access</h3>
            <p className="text-xs text-muted-foreground">
              Control devices securely from anywhere
            </p>
          </div>
          <div className="space-y-2">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
              <Shield className="h-6 w-6 text-primary" />
            </div>
            <h3 className="text-sm font-medium">Enterprise Security</h3>
            <p className="text-xs text-muted-foreground">
              Simplified auth in dev mode
            </p>
          </div>
          <div className="space-y-2">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
              <Zap className="h-6 w-6 text-primary" />
            </div>
            <h3 className="text-sm font-medium">High Performance</h3>
            <p className="text-xs text-muted-foreground">
              Low latency remote control
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}