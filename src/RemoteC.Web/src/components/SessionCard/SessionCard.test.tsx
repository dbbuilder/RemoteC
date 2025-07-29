import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { SessionCard } from './SessionCard'
import { Session, SessionStatus } from '@/types'

describe('SessionCard', () => {
  const mockOnConnect = vi.fn()
  const mockOnStop = vi.fn()
  const mockOnGeneratePin = vi.fn()

  const mockActiveSession: Session = {
    id: '123',
    deviceId: 'device1',
    device: {
      id: 'device1',
      name: 'Desktop-01',
      machineId: 'ABC123',
      operatingSystem: 'Windows 11',
      lastSeenAt: new Date().toISOString(),
      isOnline: true,
      ipAddress: '192.168.1.100',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    userId: 'user1',
    user: {
      id: 'user1',
      email: 'john@example.com',
      displayName: 'John Doe',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      lastLoginAt: new Date().toISOString(),
      isActive: true,
      roles: ['Admin'],
      permissions: [],
    },
    startedAt: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
    status: 'Active' as SessionStatus,
  }

  const mockCompletedSession: Session = {
    ...mockActiveSession,
    id: '456',
    status: 'Completed' as SessionStatus,
    endedAt: new Date().toISOString(),
    sessionPin: '123456',
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render session information correctly', () => {
    render(
      <SessionCard
        session={mockActiveSession}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText('Session 123')).toBeInTheDocument()
    expect(screen.getByText('Desktop-01')).toBeInTheDocument()
    expect(screen.getByText('John Doe')).toBeInTheDocument()
    expect(screen.getByText('Windows 11')).toBeInTheDocument()
    expect(screen.getByText('192.168.1.100')).toBeInTheDocument()
  })

  it('should display active status with correct styling', () => {
    render(
      <SessionCard
        session={mockActiveSession}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const statusBadge = screen.getByText('Active')
    expect(statusBadge).toHaveClass('bg-green-100')
  })

  it('should display session PIN when available', () => {
    render(
      <SessionCard
        session={mockCompletedSession}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText('123456')).toBeInTheDocument()
    expect(screen.getByText('Session PIN')).toBeInTheDocument()
  })

  it('should show Connect button for active sessions', () => {
    render(
      <SessionCard
        session={mockActiveSession}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const connectButton = screen.getByText('Connect')
    expect(connectButton).toBeInTheDocument()
    
    fireEvent.click(connectButton)
    expect(mockOnConnect).toHaveBeenCalledTimes(1)
  })

  it('should show Stop Session button for active sessions', () => {
    render(
      <SessionCard
        session={mockActiveSession}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const stopButton = screen.getByText('Stop Session')
    expect(stopButton).toBeInTheDocument()
    
    fireEvent.click(stopButton)
    expect(mockOnStop).toHaveBeenCalledTimes(1)
  })

  it('should show Generate PIN button for active sessions without PIN', () => {
    render(
      <SessionCard
        session={mockActiveSession}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const generatePinButton = screen.getByText('Generate PIN')
    expect(generatePinButton).toBeInTheDocument()
    
    fireEvent.click(generatePinButton)
    expect(mockOnGeneratePin).toHaveBeenCalledTimes(1)
  })

  it('should not show Generate PIN button if session already has PIN', () => {
    const sessionWithPin = {
      ...mockActiveSession,
      sessionPin: '654321',
    }

    render(
      <SessionCard
        session={sessionWithPin}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.queryByText('Generate PIN')).not.toBeInTheDocument()
    expect(screen.getByText('654321')).toBeInTheDocument()
  })

  it('should not show action buttons when showActions is false', () => {
    render(
      <SessionCard
        session={mockActiveSession}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
        showActions={false}
      />
    )

    expect(screen.queryByText('Connect')).not.toBeInTheDocument()
    expect(screen.queryByText('Stop Session')).not.toBeInTheDocument()
    expect(screen.queryByText('Generate PIN')).not.toBeInTheDocument()
  })

  it('should display duration for active sessions', () => {
    render(
      <SessionCard
        session={mockActiveSession}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText(/hour/)).toBeInTheDocument()
  })

  it('should display metadata when available', () => {
    const sessionWithMetadata = {
      ...mockActiveSession,
      metadata: {
        clientVersion: '1.0.0',
        protocol: 'WebRTC',
      },
    }

    render(
      <SessionCard
        session={sessionWithMetadata}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText('Additional Information')).toBeInTheDocument()
    expect(screen.getByText(/clientVersion/)).toBeInTheDocument()
    expect(screen.getByText(/1.0.0/)).toBeInTheDocument()
  })

  it('should handle missing device information gracefully', () => {
    const sessionNoDevice = {
      ...mockActiveSession,
      device: undefined,
    }

    render(
      <SessionCard
        session={sessionNoDevice}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText('Unknown Device')).toBeInTheDocument()
  })

  it('should handle missing user information gracefully', () => {
    const sessionNoUser = {
      ...mockActiveSession,
      user: undefined,
    }

    render(
      <SessionCard
        session={sessionNoUser}
        onConnect={mockOnConnect}
        onStop={mockOnStop}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText('Unknown User')).toBeInTheDocument()
  })
})