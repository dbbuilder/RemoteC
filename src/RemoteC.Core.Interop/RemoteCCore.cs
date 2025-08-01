using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RemoteC.Core.Interop
{
    /// <summary>
    /// P/Invoke bindings for RemoteC Core Rust library
    /// </summary>
    public static class RemoteCCore
    {
        private const string LibraryName = "remotec_core";
        
        static RemoteCCore()
        {
            Console.WriteLine($"Loading native library: {LibraryName}");
            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"Base directory: {AppDomain.CurrentDomain.BaseDirectory}");
            
            // Set up custom library resolution
            NativeLibrary.SetDllImportResolver(typeof(RemoteCCore).Assembly, DllImportResolver);
        }
        
        private static IntPtr DllImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
        {
            Console.WriteLine($"Resolving library: {libraryName}");
            
            if (libraryName == LibraryName)
            {
                string libraryPath;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    libraryPath = "remotec_core.dll";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    libraryPath = "libremotec_core.so";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    libraryPath = "libremotec_core.dylib";
                }
                else
                {
                    return IntPtr.Zero;
                }
                
                // Try multiple locations
                var searchPaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, libraryPath),
                    Path.Combine(Environment.CurrentDirectory, libraryPath),
                    Path.Combine(Environment.CurrentDirectory, "bin", "Debug", "net8.0", libraryPath),
                    Path.Combine(Environment.CurrentDirectory, "bin", "net8.0", "win-x64", libraryPath),
                    libraryPath
                };
                
                foreach (var path in searchPaths)
                {
                    Console.WriteLine($"Trying path: {path}");
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"Found library at: {path}");
                        if (NativeLibrary.TryLoad(path, out IntPtr handle))
                        {
                            return handle;
                        }
                    }
                }
            }
            
            return IntPtr.Zero;
        }

        /// <summary>
        /// Initialize the RemoteC Core library
        /// </summary>
        /// <returns>0 on success, negative value on error</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int remotec_init();

        /// <summary>
        /// Get the version of RemoteC Core
        /// </summary>
        /// <returns>Version string pointer (must be freed)</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr remotec_version();

        /// <summary>
        /// Free a string returned by the library
        /// </summary>
        /// <param name="str">String pointer to free</param>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void remotec_free_string(IntPtr str);

        /// <summary>
        /// Get version as managed string
        /// </summary>
        public static string GetVersion()
        {
            var ptr = remotec_version();
            if (ptr == IntPtr.Zero)
                return string.Empty;

            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            }
            finally
            {
                remotec_free_string(ptr);
            }
        }

        /// <summary>
        /// Initialize the library with exception handling
        /// </summary>
        public static void Initialize()
        {
            var result = remotec_init();
            if (result != 0)
            {
                throw new RemoteCoreException($"Failed to initialize RemoteC Core: {result}");
            }
        }
        
        #region Screen Capture
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr remotec_capture_create();
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int remotec_capture_start(IntPtr handle);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int remotec_capture_get_frame(IntPtr handle, ref FrameData frameData);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int remotec_capture_stop(IntPtr handle);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void remotec_capture_destroy(IntPtr handle);
        
        #endregion
        
        #region Input Simulation
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr remotec_input_create();
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int remotec_input_mouse_move(IntPtr handle, int x, int y);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int remotec_input_mouse_click(IntPtr handle, uint button);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int remotec_input_key_press(IntPtr handle, uint keyCode);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void remotec_input_destroy(IntPtr handle);
        
        #endregion
        
        #region Transport
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr remotec_transport_create(uint protocol);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void remotec_transport_destroy(IntPtr handle);
        
        #endregion
        
        /// <summary>
        /// FFI-safe frame data structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct FrameData
        {
            public uint width;
            public uint height;
            public IntPtr data;
            public UIntPtr data_len;
            public ulong timestamp;
        }
    }

    /// <summary>
    /// Exception thrown by RemoteC Core operations
    /// </summary>
    public class RemoteCoreException : Exception
    {
        public RemoteCoreException(string message) : base(message) { }
        public RemoteCoreException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}