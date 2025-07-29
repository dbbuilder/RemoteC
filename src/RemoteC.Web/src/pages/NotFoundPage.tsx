import { Button } from '@/components/ui/button'
import { useNavigate } from 'react-router-dom'

export function NotFoundPage() {
  const navigate = useNavigate()

  return (
    <div className="flex h-full flex-col items-center justify-center">
      <h1 className="text-9xl font-bold text-muted-foreground">404</h1>
      <h2 className="mt-4 text-2xl font-semibold">Page not found</h2>
      <p className="mt-2 text-muted-foreground">
        The page you're looking for doesn't exist.
      </p>
      <Button className="mt-6" onClick={() => navigate('/dashboard')}>
        Go to Dashboard
      </Button>
    </div>
  )
}