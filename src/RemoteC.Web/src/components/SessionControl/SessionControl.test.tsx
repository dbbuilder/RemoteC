import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { SessionControl } from './SessionControl'
import { useSignalR } from '@/contexts/SignalRContext'

// Mock the SignalR context
vi.mock('@/contexts/SignalRContext')

describe('SessionControl', () => {
  const mockSendMessage = vi.fn()
  const mockOn = vi.fn()
  const mockOff = vi.fn()
  const mockOnFullscreen = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    
    // Setup default mock implementation
    vi.mocked(useSignalR).mockReturnValue({
      connection: null,
      isConnected: true,
      sendMessage: mockSendMessage,
      on: mockOn,
      off: mockOff,
    })
  })

  it('should show connecting state initially', () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    expect(screen.getByText('Connecting to remote session...')).toBeInTheDocument()
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('should join session on mount', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    await waitFor(() => {
      expect(mockSendMessage).toHaveBeenCalledWith('JoinSession', 'test-session')
    })
  })

  it('should leave session on unmount', () => {
    const { unmount } = render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    unmount()

    expect(mockSendMessage).toHaveBeenCalledWith('LeaveSession', 'test-session')
  })

  it('should show control options when connected', async () => {
    const { rerender } = render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    // Simulate connection success
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    
    connectionHandler?.('connected')

    rerender(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    await waitFor(() => {
      expect(screen.queryByText('Connecting to remote session...')).not.toBeInTheDocument()
      expect(screen.getByLabelText('View Only')).toBeInTheDocument()
      expect(screen.getByLabelText('Full Control')).toBeInTheDocument()
    })
  })

  it('should show error when connection fails', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    // Simulate connection error
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    
    connectionHandler?.('error')

    await waitFor(() => {
      expect(screen.getByText('Failed to connect to remote session')).toBeInTheDocument()
    })
  })

  it('should toggle between view and control modes', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    // Simulate connected state
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    connectionHandler?.('connected')

    await waitFor(() => {
      expect(screen.getByText('View Only')).toBeInTheDocument()
    })

    const controlButton = screen.getByLabelText('Full Control')
    fireEvent.click(controlButton)

    expect(mockSendMessage).toHaveBeenCalledWith('SetControlMode', 'test-session', 'control')

    await waitFor(() => {
      expect(screen.getByText('Full Control')).toBeInTheDocument()
    })
  })

  it('should handle refresh screen action', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    // Simulate connected state
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    connectionHandler?.('connected')

    await waitFor(() => {
      const refreshButton = screen.getByLabelText('Refresh Screen')
      fireEvent.click(refreshButton)
    })

    expect(mockSendMessage).toHaveBeenCalledWith('RefreshScreen', 'test-session')
  })

  it('should handle screenshot action', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    // Simulate connected state
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    connectionHandler?.('connected')

    await waitFor(() => {
      const screenshotButton = screen.getByLabelText('Take Screenshot')
      fireEvent.click(screenshotButton)
    })

    expect(mockSendMessage).toHaveBeenCalledWith('TakeScreenshot', 'test-session')
  })

  it('should toggle audio', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    // Simulate connected state
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    connectionHandler?.('connected')

    await waitFor(() => {
      const audioButton = screen.getByLabelText('Mute Audio')
      fireEvent.click(audioButton)
    })

    expect(mockSendMessage).toHaveBeenCalledWith('SetAudioEnabled', 'test-session', false)

    await waitFor(() => {
      const audioButton = screen.getByLabelText('Enable Audio')
      fireEvent.click(audioButton)
    })

    expect(mockSendMessage).toHaveBeenCalledWith('SetAudioEnabled', 'test-session', true)
  })

  it('should handle fullscreen toggle', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
        isFullscreen={false}
      />
    )

    // Simulate connected state
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    connectionHandler?.('connected')

    await waitFor(() => {
      const fullscreenButton = screen.getByLabelText('Fullscreen')
      fireEvent.click(fullscreenButton)
    })

    expect(mockOnFullscreen).toHaveBeenCalledTimes(1)
  })

  it('should show exit fullscreen when in fullscreen mode', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
        isFullscreen={true}
      />
    )

    // Simulate connected state
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    connectionHandler?.('connected')

    await waitFor(() => {
      expect(screen.getByLabelText('Exit Fullscreen')).toBeInTheDocument()
    })
  })

  it('should show quality selector', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    // Simulate connected state
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    connectionHandler?.('connected')

    await waitFor(() => {
      const qualityButton = screen.getByLabelText('Quality Settings')
      fireEvent.click(qualityButton)
    })

    expect(screen.getByText('Auto')).toBeInTheDocument()
    expect(screen.getByText('High')).toBeInTheDocument()
    expect(screen.getByText('Medium')).toBeInTheDocument()
    expect(screen.getByText('Low')).toBeInTheDocument()
  })

  it('should change stream quality', async () => {
    render(
      <SessionControl
        sessionId="test-session"
        onFullscreen={mockOnFullscreen}
      />
    )

    // Simulate connected state
    const connectionHandler = mockOn.mock.calls.find(
      call => call[0] === 'SessionConnectionStatus'
    )?.[1]
    connectionHandler?.('connected')

    await waitFor(() => {
      const qualityButton = screen.getByLabelText('Quality Settings')
      fireEvent.click(qualityButton)
    })

    const highQuality = screen.getByText('High')
    fireEvent.click(highQuality)

    expect(mockSendMessage).toHaveBeenCalledWith('SetStreamQuality', 'test-session', 'high')
  })
})