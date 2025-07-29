import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { SessionList } from './SessionList'
import { Session, SessionStatus } from '@/types'

describe('SessionList', () => {
  const mockOnView = vi.fn()
  const mockOnStop = vi.fn()
  const mockOnDelete = vi.fn()
  const mockOnGeneratePin = vi.fn()

  const mockSessions: Session[] = [
    {
      id: '1',
      deviceId: 'device1',
      device: { 
        id: 'device1', 
        name: 'Desktop-01',
        machineId: 'ABC123',
        operatingSystem: 'Windows 11',
        lastSeenAt: new Date().toISOString(),
        isOnline: true,
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
        roles: [],
        permissions: [],
      },
      startedAt: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
      status: 'Active' as SessionStatus,
    },
    {
      id: '2',
      deviceId: 'device2',
      device: { 
        id: 'device2', 
        name: 'Laptop-02',
        machineId: 'XYZ789',
        operatingSystem: 'Windows 10',
        lastSeenAt: new Date().toISOString(),
        isOnline: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      userId: 'user2',
      user: { 
        id: 'user2', 
        email: 'jane@example.com',
        displayName: 'Jane Smith',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        lastLoginAt: new Date().toISOString(),
        isActive: true,
        roles: [],
        permissions: [],
      },
      startedAt: new Date(Date.now() - 7200000).toISOString(), // 2 hours ago
      endedAt: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
      status: 'Completed' as SessionStatus,
      sessionPin: '123456',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render empty state when no sessions', () => {
    render(
      <SessionList
        sessions={[]}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText('No active sessions')).toBeInTheDocument()
    expect(screen.getByText('Start a new remote control session to see it here.')).toBeInTheDocument()
  })

  it('should render sessions list', () => {
    render(
      <SessionList
        sessions={mockSessions}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText('Desktop-01')).toBeInTheDocument()
    expect(screen.getByText('Laptop-02')).toBeInTheDocument()
    expect(screen.getByText('John Doe')).toBeInTheDocument()
    expect(screen.getByText('Jane Smith')).toBeInTheDocument()
  })

  it('should display session status badges with correct colors', () => {
    render(
      <SessionList
        sessions={mockSessions}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const activeBadge = screen.getByText('Active')
    const completedBadge = screen.getByText('Completed')

    expect(activeBadge).toHaveClass('bg-green-100')
    expect(completedBadge).toHaveClass('bg-gray-100')
  })

  it('should display session PIN when available', () => {
    render(
      <SessionList
        sessions={mockSessions}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    expect(screen.getByText('PIN: 123456')).toBeInTheDocument()
  })

  it('should call onView when view button is clicked', () => {
    render(
      <SessionList
        sessions={mockSessions}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const viewButtons = screen.getAllByLabelText('View session')
    fireEvent.click(viewButtons[0])

    expect(mockOnView).toHaveBeenCalledWith(mockSessions[0])
  })

  it('should call onStop when stop button is clicked for active session', () => {
    render(
      <SessionList
        sessions={mockSessions}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const stopButton = screen.getByLabelText('Stop session')
    fireEvent.click(stopButton)

    expect(mockOnStop).toHaveBeenCalledWith(mockSessions[0])
  })

  it('should call onDelete when delete button is clicked for completed session', () => {
    render(
      <SessionList
        sessions={mockSessions}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const deleteButton = screen.getByLabelText('Delete session')
    fireEvent.click(deleteButton)

    expect(mockOnDelete).toHaveBeenCalledWith(mockSessions[1])
  })

  it('should call onGeneratePin when generate PIN button is clicked', () => {
    render(
      <SessionList
        sessions={mockSessions}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    const generatePinButton = screen.getByLabelText('Generate PIN')
    fireEvent.click(generatePinButton)

    expect(mockOnGeneratePin).toHaveBeenCalledWith(mockSessions[0])
  })

  it('should format duration correctly', () => {
    render(
      <SessionList
        sessions={mockSessions}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
      />
    )

    // Active session should show time since start
    expect(screen.getByText(/60 min/)).toBeInTheDocument()
    
    // Completed session should show duration between start and end
    expect(screen.getByText(/60 min/)).toBeInTheDocument()
  })

  it('should show loading state when isLoading is true', () => {
    render(
      <SessionList
        sessions={[]}
        onView={mockOnView}
        onStop={mockOnStop}
        onDelete={mockOnDelete}
        onGeneratePin={mockOnGeneratePin}
        isLoading={true}
      />
    )

    expect(screen.getByRole('status')).toBeInTheDocument()
  })
})