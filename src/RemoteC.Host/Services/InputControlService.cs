using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Host.Services;

/// <summary>
/// Service for simulating input events
/// </summary>
public interface IInputControlService
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task MoveMouseAsync(int x, int y);
    Task MouseClickAsync(MouseButton button, int x, int y);
    Task MouseWheelAsync(int delta);
    Task KeyPressAsync(VirtualKeyCode key, KeyModifiers modifiers);
    Task KeyDownAsync(VirtualKeyCode key, KeyModifiers modifiers);
    Task KeyUpAsync(VirtualKeyCode key, KeyModifiers modifiers);
    Task<string> GetClipboardContentAsync();
    Task SetClipboardContentAsync(string content);
    Task DisposeAsync();
}

public class InputControlService : IInputControlService
{
    private readonly ILogger<InputControlService> _logger;
    private readonly IRemoteControlProvider _remoteControlProvider;
    private bool _isInitialized;
    private readonly SemaphoreSlim _inputLock = new(1, 1);

    // Windows API constants
    private const int MOUSEEVENTF_MOVE = 0x0001;
    private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const int MOUSEEVENTF_LEFTUP = 0x0004;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const int MOUSEEVENTF_RIGHTUP = 0x0010;
    private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const int MOUSEEVENTF_WHEEL = 0x0800;
    private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

    private const int KEYEVENTF_KEYDOWN = 0x0000;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const int KEYEVENTF_UNICODE = 0x0004;

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public InputControlService(
        ILogger<InputControlService> logger,
        IRemoteControlProvider remoteControlProvider)
    {
        _logger = logger;
        _remoteControlProvider = remoteControlProvider;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
            return;

        _logger.LogInformation("Initializing input control service");
        _isInitialized = true;
        await Task.CompletedTask;
    }

    public async Task MoveMouseAsync(int x, int y)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Input control service not initialized");

        await _inputLock.WaitAsync();
        try
        {
            // For Phase 1, use provider
            // For Phase 2+, use direct API or Rust
            var inputEvent = new MouseInputEvent
            {
                Action = MouseAction.Move,
                X = x,
                Y = y,
                Timestamp = DateTime.UtcNow
            };

            await _remoteControlProvider.SendInputAsync("current-session", inputEvent);
            
            // Fallback to direct API
            SetCursorPos(x, y);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving mouse to ({X}, {Y})", x, y);
        }
        finally
        {
            _inputLock.Release();
        }
    }

    public async Task MouseClickAsync(MouseButton button, int x, int y)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Input control service not initialized");

        await _inputLock.WaitAsync();
        try
        {
            // Move to position first
            await MoveMouseAsync(x, y);
            
            // Perform click
            int downFlag = button switch
            {
                MouseButton.Left => MOUSEEVENTF_LEFTDOWN,
                MouseButton.Right => MOUSEEVENTF_RIGHTDOWN,
                MouseButton.Middle => MOUSEEVENTF_MIDDLEDOWN,
                _ => MOUSEEVENTF_LEFTDOWN
            };

            int upFlag = button switch
            {
                MouseButton.Left => MOUSEEVENTF_LEFTUP,
                MouseButton.Right => MOUSEEVENTF_RIGHTUP,
                MouseButton.Middle => MOUSEEVENTF_MIDDLEUP,
                _ => MOUSEEVENTF_LEFTUP
            };

            mouse_event(downFlag, 0, 0, 0, IntPtr.Zero);
            await Task.Delay(50); // Small delay for click registration
            mouse_event(upFlag, 0, 0, 0, IntPtr.Zero);

            _logger.LogDebug("Mouse click {Button} at ({X}, {Y})", button, x, y);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clicking mouse button {Button}", button);
        }
        finally
        {
            _inputLock.Release();
        }
    }

    public async Task MouseWheelAsync(int delta)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Input control service not initialized");

        await _inputLock.WaitAsync();
        try
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, delta, IntPtr.Zero);
            _logger.LogDebug("Mouse wheel scrolled by {Delta}", delta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scrolling mouse wheel");
        }
        finally
        {
            _inputLock.Release();
        }
    }

    public async Task KeyPressAsync(VirtualKeyCode key, KeyModifiers modifiers)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Input control service not initialized");

        await _inputLock.WaitAsync();
        try
        {
            // Handle modifiers
            await ApplyModifiersAsync(modifiers, true);
            
            // Send key press
            var vk = (byte)key;
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
            await Task.Delay(50);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
            
            // Release modifiers
            await ApplyModifiersAsync(modifiers, false);

            _logger.LogDebug("Key pressed: {Key} with modifiers {Modifiers}", key, modifiers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pressing key {Key}", key);
        }
        finally
        {
            _inputLock.Release();
        }
    }

    public async Task KeyDownAsync(VirtualKeyCode key, KeyModifiers modifiers)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Input control service not initialized");

        await _inputLock.WaitAsync();
        try
        {
            await ApplyModifiersAsync(modifiers, true);
            var vk = (byte)key;
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
            _logger.LogDebug("Key down: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending key down {Key}", key);
        }
        finally
        {
            _inputLock.Release();
        }
    }

    public async Task KeyUpAsync(VirtualKeyCode key, KeyModifiers modifiers)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Input control service not initialized");

        await _inputLock.WaitAsync();
        try
        {
            var vk = (byte)key;
            keybd_event(vk, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
            await ApplyModifiersAsync(modifiers, false);
            _logger.LogDebug("Key up: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending key up {Key}", key);
        }
        finally
        {
            _inputLock.Release();
        }
    }

    public async Task<string> GetClipboardContentAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // TODO: Implement clipboard access
                // Clipboard API requires STA thread in Windows Forms
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clipboard content");
                return string.Empty;
            }
        });
    }

    public async Task SetClipboardContentAsync(string content)
    {
        await Task.Run(() =>
        {
            try
            {
                // TODO: Implement clipboard access
                // Clipboard API requires STA thread in Windows Forms
                _logger.LogDebug("Clipboard content set");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting clipboard content");
            }
        });
    }

    private async Task ApplyModifiersAsync(KeyModifiers modifiers, bool press)
    {
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            keybd_event((byte)VirtualKeyCode.CONTROL, 0, press ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP, IntPtr.Zero);
            await Task.Delay(10);
        }
        
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            keybd_event((byte)VirtualKeyCode.SHIFT, 0, press ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP, IntPtr.Zero);
            await Task.Delay(10);
        }
        
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            keybd_event((byte)VirtualKeyCode.MENU, 0, press ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP, IntPtr.Zero);
            await Task.Delay(10);
        }
        
        if (modifiers.HasFlag(KeyModifiers.Windows))
        {
            keybd_event((byte)VirtualKeyCode.LWIN, 0, press ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP, IntPtr.Zero);
            await Task.Delay(10);
        }
    }

    public async Task DisposeAsync()
    {
        _logger.LogInformation("Disposing input control service");
        _inputLock?.Dispose();
        await Task.CompletedTask;
    }
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4,
    Windows = 8
}

public enum MouseButton
{
    Left,
    Right,
    Middle,
    X1,
    X2
}

public class InputEventData
{
    public Guid SessionId { get; set; }
    public InputType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public MouseButton Button { get; set; }
    public int Delta { get; set; }
    public VirtualKeyCode Key { get; set; }
    public KeyModifiers Modifiers { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum InputType
{
    MouseMove,
    MouseClick,
    MouseDown,
    MouseUp,
    MouseWheel,
    KeyPress,
    KeyDown,
    KeyUp
}