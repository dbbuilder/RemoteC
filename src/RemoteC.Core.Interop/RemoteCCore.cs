using System;
using System.Runtime.InteropServices;

namespace RemoteC.Core.Interop
{
    /// <summary>
    /// P/Invoke bindings for RemoteC Core Rust library
    /// </summary>
    public static class RemoteCCore
    {
        private const string LibraryName = "remotec_core";

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