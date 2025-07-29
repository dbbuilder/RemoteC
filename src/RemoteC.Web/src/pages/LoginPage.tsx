import { useState } from 'react'
import { useMsal } from '@azure/msal-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { loginRequest } from '@/config/authConfig'
import { Monitor, Shield, Zap } from 'lucide-react'

export function LoginPage() {
  const { instance } = useMsal()
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleLogin = async () => {
    try {
      setIsLoading(true)
      setError(null)
      await instance.loginPopup(loginRequest)
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

        <Card>
          <CardHeader>
            <CardTitle className="text-2xl">Welcome back</CardTitle>
            <CardDescription>
              Sign in with your corporate account to access RemoteC
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
            
            <Button
              className="w-full"
              size="lg"
              onClick={handleLogin}
              disabled={isLoading}
            >
              {isLoading ? (
                <div className="flex items-center gap-2">
                  <div className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                  Signing in...
                </div>
              ) : (
                'Sign in with Azure AD'
              )}
            </Button>
          </CardContent>
          <CardFooter>
            <p className="text-center text-sm text-muted-foreground">
              By signing in, you agree to our Terms of Service and Privacy Policy
            </p>
          </CardFooter>
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
              Azure AD integration with RBAC
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