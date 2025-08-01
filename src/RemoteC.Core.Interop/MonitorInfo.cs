using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RemoteC.Shared.Models;

namespace RemoteC.Core.Interop
{
    /// <summary>
    /// Monitor management utilities for FFI conversion
    /// </summary>
    public static class MonitorManager
    {
        /// <summary>
        /// Convert FFI monitor info to shared MonitorInfo
        /// </summary>
        internal static MonitorInfo FromFFI(RemoteCCore.MonitorInfoFFI ffi)
        {
            return new MonitorInfo
            {
                Id = Marshal.PtrToStringAnsi(ffi.id) ?? string.Empty,
                Name = Marshal.PtrToStringAnsi(ffi.name) ?? string.Empty,
                Index = (int)ffi.index,
                IsPrimary = ffi.is_primary != 0,
                Bounds = new Rectangle
                {
                    X = ffi.x,
                    Y = ffi.y,
                    Width = (int)ffi.width,
                    Height = (int)ffi.height
                },
                WorkArea = new Rectangle
                {
                    X = ffi.work_x,
                    Y = ffi.work_y,
                    Width = (int)ffi.work_width,
                    Height = (int)ffi.work_height
                },
                ScaleFactor = ffi.scale_factor,
                RefreshRate = (int)ffi.refresh_rate,
                BitDepth = (int)ffi.bit_depth,
                Orientation = (MonitorOrientation)ffi.orientation
            };
        }

        /// <summary>
        /// Enumerate all available monitors
        /// </summary>
        public static List<MonitorInfo> EnumerateMonitors()
        {
            var monitors = new List<MonitorInfo>();
            IntPtr listPtr = IntPtr.Zero;

            try
            {
                listPtr = RemoteCCore.remotec_enumerate_monitors();
                if (listPtr == IntPtr.Zero)
                {
                    throw new RemoteCoreException("Failed to enumerate monitors");
                }

                var list = Marshal.PtrToStructure<RemoteCCore.MonitorListFFI>(listPtr);
                if (list.count > 0 && list.monitors != IntPtr.Zero)
                {
                    var monitorSize = Marshal.SizeOf<RemoteCCore.MonitorInfoFFI>();
                    for (int i = 0; i < list.count; i++)
                    {
                        var monitorPtr = IntPtr.Add(list.monitors, i * monitorSize);
                        var ffiMonitor = Marshal.PtrToStructure<RemoteCCore.MonitorInfoFFI>(monitorPtr);
                        monitors.Add(FromFFI(ffiMonitor));
                    }
                }

                return monitors;
            }
            finally
            {
                if (listPtr != IntPtr.Zero)
                {
                    RemoteCCore.remotec_free_monitor_list(listPtr);
                }
            }
        }

        /// <summary>
        /// Get virtual desktop bounds
        /// </summary>
        public static Rectangle GetVirtualDesktopBounds()
        {
            if (RemoteCCore.remotec_get_virtual_desktop_bounds(out int x, out int y, out uint width, out uint height) == 0)
            {
                return new Rectangle
                {
                    X = x,
                    Y = y,
                    Width = (int)width,
                    Height = (int)height
                };
            }
            throw new RemoteCoreException("Failed to get virtual desktop bounds");
        }

        /// <summary>
        /// Get monitor at a specific point
        /// </summary>
        public static MonitorInfo? GetMonitorAtPoint(int x, int y)
        {
            IntPtr monitorPtr = IntPtr.Zero;

            try
            {
                monitorPtr = RemoteCCore.remotec_get_monitor_at_point(x, y);
                if (monitorPtr == IntPtr.Zero)
                {
                    return null;
                }

                var ffiMonitor = Marshal.PtrToStructure<RemoteCCore.MonitorInfoFFI>(monitorPtr);
                return FromFFI(ffiMonitor);
            }
            finally
            {
                if (monitorPtr != IntPtr.Zero)
                {
                    RemoteCCore.remotec_free_monitor_info(monitorPtr);
                }
            }
        }

        /// <summary>
        /// Get complete virtual desktop information
        /// </summary>
        public static VirtualDesktopInfo GetVirtualDesktop()
        {
            var monitors = EnumerateMonitors();
            var bounds = GetVirtualDesktopBounds();
            
            var primaryIndex = -1;
            for (int i = 0; i < monitors.Count; i++)
            {
                if (monitors[i].IsPrimary)
                {
                    primaryIndex = i;
                    break;
                }
            }

            if (primaryIndex == -1 && monitors.Count > 0)
            {
                primaryIndex = 0;
            }

            return new VirtualDesktopInfo
            {
                Monitors = monitors,
                TotalBounds = bounds,
                PrimaryIndex = primaryIndex
            };
        }
    }
}