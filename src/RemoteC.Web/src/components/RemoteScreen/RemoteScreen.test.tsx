import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { RemoteScreen } from './RemoteScreen'
import { useSignalR } from '@/contexts/SignalRContext'

// Mock the SignalR context
vi.mock('@/contexts/SignalRContext')

// Mock canvas context
const mockGetContext = vi.fn()
const mockDrawImage = vi.fn()

describe('RemoteScreen', () => {
  const mockSendMessage = vi.fn()
  const mockOn = vi.fn()
  const mockOff = vi.fn()
  const mockOnMouseMove = vi.fn()
  const mockOnMouseClick = vi.fn()
  const mockOnKeyPress = vi.fn()

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

    // Mock canvas
    HTMLCanvasElement.prototype.getContext = mockGetContext
    mockGetContext.mockReturnValue({
      drawImage: mockDrawImage,
    })
  })

  it('should render canvas with correct dimensions', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        width={1920}
        height={1080}
      />
    )

    const canvas = screen.getByRole('img', { hidden: true })
    expect(canvas).toHaveAttribute('width', '1920')
    expect(canvas).toHaveAttribute('height', '1080')
  })

  it('should show loading state initially', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
      />
    )

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument()
  })

  it('should subscribe to screen updates on mount', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
      />
    )

    expect(mockOn).toHaveBeenCalledWith('ScreenUpdate', expect.any(Function))
  })

  it('should unsubscribe from screen updates on unmount', () => {
    const { unmount } = render(
      <RemoteScreen
        sessionId="test-session"
      />
    )

    unmount()

    expect(mockOff).toHaveBeenCalledWith('ScreenUpdate', expect.any(Function))
  })

  it('should handle screen update data', async () => {
    render(
      <RemoteScreen
        sessionId="test-session"
      />
    )

    // Get the screen update handler
    const screenUpdateHandler = mockOn.mock.calls.find(
      call => call[0] === 'ScreenUpdate'
    )?.[1]

    // Create a mock image
    const mockImage = new Image()
    vi.spyOn(window, 'Image').mockImplementation(() => mockImage)

    // Simulate screen update
    screenUpdateHandler?.('base64ImageData')

    // Trigger image load
    mockImage.onload?.(new Event('load'))

    await waitFor(() => {
      expect(mockDrawImage).toHaveBeenCalledWith(mockImage, 0, 0, 1920, 1080)
    })
  })

  it('should show error when image fails to load', async () => {
    render(
      <RemoteScreen
        sessionId="test-session"
      />
    )

    // Get the screen update handler
    const screenUpdateHandler = mockOn.mock.calls.find(
      call => call[0] === 'ScreenUpdate'
    )?.[1]

    // Create a mock image
    const mockImage = new Image()
    vi.spyOn(window, 'Image').mockImplementation(() => mockImage)

    // Simulate screen update
    screenUpdateHandler?.('base64ImageData')

    // Trigger image error
    mockImage.onerror?.(new Event('error'))

    await waitFor(() => {
      expect(screen.getByText('Failed to load screen data')).toBeInTheDocument()
    })
  })

  it('should display FPS counter', async () => {
    render(
      <RemoteScreen
        sessionId="test-session"
      />
    )

    expect(screen.getByText(/FPS/)).toBeInTheDocument()
  })

  it('should handle mouse move when control is enabled', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={true}
        onMouseMove={mockOnMouseMove}
      />
    )

    const canvas = screen.getByRole('img', { hidden: true })
    
    // Mock getBoundingClientRect
    canvas.getBoundingClientRect = vi.fn(() => ({
      left: 0,
      top: 0,
      width: 1920,
      height: 1080,
      right: 1920,
      bottom: 1080,
      x: 0,
      y: 0,
      toJSON: () => {},
    }))

    fireEvent.mouseMove(canvas, { clientX: 960, clientY: 540 })

    expect(mockSendMessage).toHaveBeenCalledWith('MouseMove', 'test-session', 960, 540)
    expect(mockOnMouseMove).toHaveBeenCalledWith(960, 540)
  })

  it('should not handle mouse move when control is disabled', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={false}
        onMouseMove={mockOnMouseMove}
      />
    )

    const canvas = screen.getByRole('img', { hidden: true })
    fireEvent.mouseMove(canvas, { clientX: 960, clientY: 540 })

    expect(mockSendMessage).not.toHaveBeenCalled()
    expect(mockOnMouseMove).not.toHaveBeenCalled()
  })

  it('should handle mouse click when control is enabled', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={true}
        onMouseClick={mockOnMouseClick}
      />
    )

    const canvas = screen.getByRole('img', { hidden: true })
    
    // Mock getBoundingClientRect
    canvas.getBoundingClientRect = vi.fn(() => ({
      left: 0,
      top: 0,
      width: 1920,
      height: 1080,
      right: 1920,
      bottom: 1080,
      x: 0,
      y: 0,
      toJSON: () => {},
    }))

    fireEvent.click(canvas, { clientX: 100, clientY: 100, button: 0 })

    expect(mockSendMessage).toHaveBeenCalledWith('MouseClick', 'test-session', 100, 100, 0)
    expect(mockOnMouseClick).toHaveBeenCalledWith(100, 100, 0)
  })

  it('should handle keyboard events when control is enabled', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={true}
        onKeyPress={mockOnKeyPress}
      />
    )

    fireEvent.keyDown(window, { key: 'a', code: 'KeyA' })

    expect(mockSendMessage).toHaveBeenCalledWith('KeyDown', 'test-session', 'a', 'KeyA')
    expect(mockOnKeyPress).toHaveBeenCalledWith('a', 'KeyA')
  })

  it('should handle key up events', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={true}
      />
    )

    fireEvent.keyUp(window, { key: 'a', code: 'KeyA' })

    expect(mockSendMessage).toHaveBeenCalledWith('KeyUp', 'test-session', 'a', 'KeyA')
  })

  it('should not handle keyboard events when control is disabled', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={false}
        onKeyPress={mockOnKeyPress}
      />
    )

    fireEvent.keyDown(window, { key: 'a', code: 'KeyA' })

    expect(mockSendMessage).not.toHaveBeenCalled()
    expect(mockOnKeyPress).not.toHaveBeenCalled()
  })

  it('should prevent context menu on right click when control is enabled', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={true}
      />
    )

    const canvas = screen.getByRole('img', { hidden: true })
    const event = new MouseEvent('contextmenu', { bubbles: true, cancelable: true })
    
    fireEvent(canvas, event)

    expect(event.defaultPrevented).toBe(true)
  })

  it('should show crosshair cursor when control is enabled', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={true}
      />
    )

    const container = screen.getByTestId('remote-screen-container')
    expect(container).toHaveClass('cursor-crosshair')
  })

  it('should show default cursor when control is disabled', () => {
    render(
      <RemoteScreen
        sessionId="test-session"
        controlEnabled={false}
      />
    )

    const container = screen.getByTestId('remote-screen-container')
    expect(container).toHaveClass('cursor-default')
  })
})