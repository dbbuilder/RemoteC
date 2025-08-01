using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace RemoteC.Host.Services
{
    /// <summary>
    /// Factory for creating platform-specific clipboard access implementations
    /// </summary>
    public static class ClipboardAccessFactory
    {
        public static IClipboardAccess Create(ILogger? logger = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger?.LogInformation("Creating Windows clipboard access");
                // For now, use cross-platform stub on all platforms to avoid Windows Forms dependency
                // In production, this would return WindowsClipboardAccess
                return new CrossPlatformClipboardAccess();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                logger?.LogInformation("Creating Linux clipboard access");
                return new LinuxClipboardAccess();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                logger?.LogInformation("Creating macOS clipboard access");
                return new MacOSClipboardAccess();
            }
            else
            {
                logger?.LogWarning("Unknown platform, using cross-platform clipboard stub");
                return new CrossPlatformClipboardAccess();
            }
        }
    }
}