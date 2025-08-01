import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { MultiMonitorDisplay } from '../components/RemoteControl/MultiMonitorDisplay';
import { RemoteControlService } from '../services/remoteControlService';
import { MonitorInfo, VirtualDesktopBounds } from '../types/monitor';

// Mock the remote control service
jest.mock('../services/remoteControlService');

describe('MultiMonitorDisplay', () => {
  const mockRemoteControlService = RemoteControlService as jest.Mocked<typeof RemoteControlService>;
  
  const mockMonitors: MonitorInfo[] = [
    {
      id: 'monitor1',
      name: 'Primary Display',
      isPrimary: true,
      bounds: { x: 0, y: 0, width: 1920, height: 1080 },
      scaleFactor: 1.0,
      refreshRate: 60
    },
    {
      id: 'monitor2',
      name: 'Secondary Display',
      isPrimary: false,
      bounds: { x: 1920, y: 0, width: 2560, height: 1440 },
      scaleFactor: 1.25,
      refreshRate: 144
    }
  ];

  const mockVirtualBounds: VirtualDesktopBounds = {
    x: 0,
    y: 0,
    width: 4480,
    height: 1440,
    monitorCount: 2
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockRemoteControlService.getMonitors.mockResolvedValue(mockMonitors);
    mockRemoteControlService.getVirtualDesktopBounds.mockResolvedValue(mockVirtualBounds);
  });

  test('renders monitor selector with all available monitors', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByText('Primary Display')).toBeInTheDocument();
      expect(screen.getByText('Secondary Display')).toBeInTheDocument();
    });

    // Should show primary indicator
    expect(screen.getByTestId('primary-indicator')).toBeInTheDocument();
  });

  test('switches to selected monitor on click', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByText('Secondary Display')).toBeInTheDocument();
    });

    // Click on secondary monitor
    fireEvent.click(screen.getByText('Secondary Display'));

    await waitFor(() => {
      expect(mockRemoteControlService.selectMonitor).toHaveBeenCalledWith(
        'test-session',
        'monitor2'
      );
    });

    // Should show active indicator on selected monitor
    expect(screen.getByTestId('monitor2-active')).toHaveClass('active');
  });

  test('displays monitor preview thumbnails', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      const previews = screen.getAllByTestId(/monitor-preview/);
      expect(previews).toHaveLength(2);
    });

    // Check preview aspect ratios
    const preview1 = screen.getByTestId('monitor1-preview');
    expect(preview1).toHaveStyle({ aspectRatio: '1920 / 1080' });

    const preview2 = screen.getByTestId('monitor2-preview');
    expect(preview2).toHaveStyle({ aspectRatio: '2560 / 1440' });
  });

  test('shows monitor arrangement in minimap', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByTestId('monitor-minimap')).toBeInTheDocument();
    });

    const minimap = screen.getByTestId('monitor-minimap');
    const monitorRects = minimap.querySelectorAll('.monitor-rect');
    
    expect(monitorRects).toHaveLength(2);
    
    // Check relative positions
    const rect1 = monitorRects[0].getBoundingClientRect();
    const rect2 = monitorRects[1].getBoundingClientRect();
    expect(rect2.left).toBeGreaterThan(rect1.right);
  });

  test('handles monitor with different DPI scaling', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      const monitor2Info = screen.getByTestId('monitor2-info');
      expect(monitor2Info).toHaveTextContent('125% scaling');
      expect(monitor2Info).toHaveTextContent('2560×1440');
    });
  });

  test('shows all monitors option', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByText('All Monitors')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('All Monitors'));

    await waitFor(() => {
      expect(mockRemoteControlService.selectAllMonitors).toHaveBeenCalledWith(
        'test-session'
      );
    });

    // Should show combined resolution
    expect(screen.getByTestId('current-view-info')).toHaveTextContent('4480×1440');
  });

  test('handles monitor disconnection gracefully', async () => {
    const { rerender } = render(<MultiMonitorDisplay sessionId="test-session" />);

    // Simulate monitor disconnect
    mockRemoteControlService.getMonitors.mockResolvedValue([mockMonitors[0]]);
    
    // Trigger refresh
    fireEvent.click(screen.getByTestId('refresh-monitors'));

    await waitFor(() => {
      expect(screen.queryByText('Secondary Display')).not.toBeInTheDocument();
      expect(screen.getByText('Primary Display')).toBeInTheDocument();
    });

    // Should show notification
    expect(screen.getByText('Monitor configuration changed')).toBeInTheDocument();
  });

  test('supports keyboard navigation between monitors', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByText('Primary Display')).toBeInTheDocument();
    });

    const monitorList = screen.getByRole('list', { name: 'Monitors' });
    
    // Focus first monitor
    fireEvent.focus(monitorList);
    fireEvent.keyDown(monitorList, { key: 'ArrowDown' });

    expect(screen.getByText('Secondary Display')).toHaveFocus();

    // Select with Enter
    fireEvent.keyDown(screen.getByText('Secondary Display'), { key: 'Enter' });

    await waitFor(() => {
      expect(mockRemoteControlService.selectMonitor).toHaveBeenCalledWith(
        'test-session',
        'monitor2'
      );
    });
  });

  test('displays monitor details on hover', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByText('Secondary Display')).toBeInTheDocument();
    });

    const monitor2 = screen.getByTestId('monitor2-item');
    fireEvent.mouseEnter(monitor2);

    await waitFor(() => {
      const tooltip = screen.getByRole('tooltip');
      expect(tooltip).toHaveTextContent('Dell U2415');
      expect(tooltip).toHaveTextContent('2560×1440 @ 144Hz');
      expect(tooltip).toHaveTextContent('125% scaling');
    });
  });

  test('allows pinning frequently used monitor configuration', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByText('Secondary Display')).toBeInTheDocument();
    });

    // Select monitor 2
    fireEvent.click(screen.getByText('Secondary Display'));

    // Pin configuration
    fireEvent.click(screen.getByTestId('pin-configuration'));

    await waitFor(() => {
      expect(mockRemoteControlService.saveMonitorPreference).toHaveBeenCalledWith({
        sessionId: 'test-session',
        monitorId: 'monitor2',
        isPinned: true
      });
    });

    expect(screen.getByTestId('pinned-indicator')).toBeInTheDocument();
  });

  test('supports picture-in-picture for secondary monitor', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByText('Secondary Display')).toBeInTheDocument();
    });

    // Enable PiP for monitor 2
    const monitor2 = screen.getByTestId('monitor2-item');
    fireEvent.contextMenu(monitor2);

    const pipOption = screen.getByText('Show in Picture-in-Picture');
    fireEvent.click(pipOption);

    await waitFor(() => {
      expect(mockRemoteControlService.enablePictureInPicture).toHaveBeenCalledWith(
        'test-session',
        'monitor2'
      );
    });

    // Should show PiP indicator
    expect(screen.getByTestId('pip-active-monitor2')).toBeInTheDocument();
  });
});

// Test monitor auto-selection logic
describe('Monitor Auto-Selection', () => {
  test('auto-selects primary monitor on initial load', async () => {
    render(<MultiMonitorDisplay sessionId="test-session" autoSelect={true} />);

    await waitFor(() => {
      expect(mockRemoteControlService.selectMonitor).toHaveBeenCalledWith(
        'test-session',
        'monitor1'
      );
    });
  });

  test('remembers last selected monitor in session', async () => {
    // Mock saved preference
    mockRemoteControlService.getMonitorPreference.mockResolvedValue({
      monitorId: 'monitor2',
      isPinned: false
    });

    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(mockRemoteControlService.selectMonitor).toHaveBeenCalledWith(
        'test-session',
        'monitor2'
      );
    });
  });
});

// Test responsive behavior
describe('Responsive Monitor Display', () => {
  test('shows compact view on mobile devices', async () => {
    // Mock mobile viewport
    global.innerWidth = 375;
    global.dispatchEvent(new Event('resize'));

    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByTestId('monitor-dropdown')).toBeInTheDocument();
      expect(screen.queryByTestId('monitor-minimap')).not.toBeInTheDocument();
    });
  });

  test('shows full view on desktop', async () => {
    // Mock desktop viewport
    global.innerWidth = 1920;
    global.dispatchEvent(new Event('resize'));

    render(<MultiMonitorDisplay sessionId="test-session" />);

    await waitFor(() => {
      expect(screen.getByTestId('monitor-minimap')).toBeInTheDocument();
      expect(screen.queryByTestId('monitor-dropdown')).not.toBeInTheDocument();
    });
  });
});