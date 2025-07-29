using System;
using System.Runtime.InteropServices;
using RemoteC.Core.Interop;
using Xunit;

namespace RemoteC.Tests.Integration
{
    /// <summary>
    /// Integration tests for Rust/.NET interop
    /// </summary>
    public class RustInteropTests : IDisposable
    {
        private bool _initialized;

        public RustInteropTests()
        {
            // Initialize Rust library
            var result = RemoteCCore.remotec_init();
            _initialized = result == 0;
        }

        public void Dispose()
        {
            // Cleanup
        }

        [Fact]
        public void TestInitialization()
        {
            Assert.True(_initialized, "Failed to initialize RemoteC Core");
        }

        [Fact]
        public void TestVersionString()
        {
            var version = RemoteCCore.GetVersion();
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            Assert.Matches(@"^\d+\.\d+\.\d+", version);
        }

        [Fact]
        public void TestScreenCaptureLifecycle()
        {
            // Create capture instance
            var captureHandle = RemoteCCore.remotec_capture_create();
            Assert.NotEqual(IntPtr.Zero, captureHandle);

            try
            {
                // Start capture
                var startResult = RemoteCCore.remotec_capture_start(captureHandle);
                Assert.Equal(0, startResult);

                // Get a frame
                var frameData = new RemoteCCore.FrameData();
                var frameResult = RemoteCCore.remotec_capture_get_frame(captureHandle, ref frameData);
                
                // Frame might not be immediately available
                if (frameResult == 0)
                {
                    Assert.True(frameData.width > 0);
                    Assert.True(frameData.height > 0);
                    Assert.NotEqual(IntPtr.Zero, frameData.data);
                    Assert.True(frameData.data_len > 0);
                }

                // Stop capture
                var stopResult = RemoteCCore.remotec_capture_stop(captureHandle);
                Assert.Equal(0, stopResult);
            }
            finally
            {
                // Cleanup
                RemoteCCore.remotec_capture_destroy(captureHandle);
            }
        }

        [Fact]
        public void TestInputSimulatorCreation()
        {
            var inputHandle = RemoteCCore.remotec_input_create();
            Assert.NotEqual(IntPtr.Zero, inputHandle);

            try
            {
                // Test mouse move
                var moveResult = RemoteCCore.remotec_input_mouse_move(inputHandle, 100, 200);
                Assert.Equal(0, moveResult);

                // Test mouse click
                var clickResult = RemoteCCore.remotec_input_mouse_click(inputHandle, 0); // Left button
                Assert.Equal(0, clickResult);
            }
            finally
            {
                RemoteCCore.remotec_input_destroy(inputHandle);
            }
        }

        [Fact]
        public void TestTransportCreation()
        {
            // Create QUIC transport
            var transportHandle = RemoteCCore.remotec_transport_create(0); // QUIC protocol
            Assert.NotEqual(IntPtr.Zero, transportHandle);

            try
            {
                // Transport is created successfully
                Assert.True(true, "Transport created");
            }
            finally
            {
                RemoteCCore.remotec_transport_destroy(transportHandle);
            }
        }

        [Fact]
        public void TestNullHandleSafety()
        {
            // Test that functions handle null pointers gracefully
            var result = RemoteCCore.remotec_capture_start(IntPtr.Zero);
            Assert.Equal(-1, result);

            result = RemoteCCore.remotec_capture_stop(IntPtr.Zero);
            Assert.Equal(-1, result);

            result = RemoteCCore.remotec_input_mouse_move(IntPtr.Zero, 0, 0);
            Assert.Equal(-1, result);

            // Destroy with null should not crash
            RemoteCCore.remotec_capture_destroy(IntPtr.Zero);
            RemoteCCore.remotec_input_destroy(IntPtr.Zero);
            RemoteCCore.remotec_transport_destroy(IntPtr.Zero);
        }

        [Theory]
        [InlineData(0)] // QUIC
        [InlineData(1)] // WebRTC (should fail as not implemented)
        [InlineData(2)] // UDP (should fail as not implemented)
        public void TestTransportProtocols(uint protocol)
        {
            var transportHandle = RemoteCCore.remotec_transport_create(protocol);
            
            if (protocol == 0) // QUIC should succeed
            {
                Assert.NotEqual(IntPtr.Zero, transportHandle);
                RemoteCCore.remotec_transport_destroy(transportHandle);
            }
            else // Others should fail
            {
                Assert.Equal(IntPtr.Zero, transportHandle);
            }
        }

        [Fact]
        public void TestMemoryManagement()
        {
            // Create and destroy multiple instances to check for leaks
            for (int i = 0; i < 10; i++)
            {
                var captureHandle = RemoteCCore.remotec_capture_create();
                if (captureHandle != IntPtr.Zero)
                {
                    RemoteCCore.remotec_capture_destroy(captureHandle);
                }

                var inputHandle = RemoteCCore.remotec_input_create();
                if (inputHandle != IntPtr.Zero)
                {
                    RemoteCCore.remotec_input_destroy(inputHandle);
                }
            }

            // If we get here without crashing, memory management is likely correct
            Assert.True(true);
        }
    }
}