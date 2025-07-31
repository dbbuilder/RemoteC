# RemoteC TODO List

## Current Priority Tasks

### High Priority
- [ ] Get remote control working with Rust provider
- [ ] Test remote session creation and connection
- [ ] Verify screen sharing and input control
- [ ] Fix any SignalR connection issues

### Medium Priority
- [ ] Test provider switching functionality

## Completed Tasks ✅

### Phase 1 - UI/Authentication
- [x] Fix login re-prompting when switching tabs
- [x] Fix data not showing in UI pages
- [x] Debug API connection and auth token issues

### Phase 2 - Rust Performance Engine
- [x] Implement Rust performance engine (Phase 2)
- [x] Create FFI interface for .NET/Rust communication
- [x] Build and test Rust core library
- [x] Replace ControlR with custom Rust implementation
- [x] Add provider configuration UI
- [x] Build and test API with Rust provider

### Rust Build Fixes
- [x] Fix YUVBuffer API usage (no y_mut, u_mut, v_mut methods)
- [x] Fix slice indexing u32 vs usize type errors
- [x] Fix type mismatch u16 vs u32 in mouse input
- [x] Fix dmDisplayOrientation field not found on DEVMODEW
- [x] Fix unsafe function call requires unsafe block
- [x] Fix InputEngine trait implementation for all platforms
- [x] Fix literal out of range for i16 (0x8000)

## Code TODOs from Source Files

### Rust Core (RemoteC.Core)

#### Linux Platform Support
- [ ] Implement X11/Wayland capture (`src/capture/linux.rs:29`)
- [ ] Implement frame capture for Linux (`src/capture/linux.rs:48`)
- [ ] Implement monitor enumeration using X11 RandR or Wayland protocols (`src/capture/linux.rs:63`)
- [ ] Implement display info using XRandR (`src/capture/linux.rs:92`)
- [ ] Implement display info using Wayland protocols (`src/capture/linux.rs:107`)
- [ ] Implement input using X11/XTest (`src/input/linux.rs:16,21,26,31`)

#### macOS Platform Support
- [ ] Implement Core Graphics capture (`src/capture/macos.rs:29`)
- [ ] Implement frame capture for macOS (`src/capture/macos.rs:48`)
- [ ] Implement monitor enumeration using Core Graphics Display APIs (`src/capture/macos.rs:63`)
- [ ] Implement display info using Core Graphics (`src/capture/macos.rs:94`)
- [ ] Implement input using Core Graphics (`src/input/macos.rs:16,21,26,31`)

#### Video Encoding
- [ ] Implement actual H.265 encoding (`src/video/h265.rs:47`)
- [ ] Implement actual H.265 decoding (`src/video/h265.rs:105`)
- [ ] Implement actual VP9 encoding using libvpx (`src/video/vp9.rs:47`)
- [ ] Implement actual VP9 decoding (`src/video/vp9.rs:104`)
- [ ] Implement actual VP8 encoding using libvpx (`src/video/vp8.rs:47`)
- [ ] Implement actual VP8 decoding (`src/video/vp8.rs:104`)
- [ ] Implement actual NVENC encoding (`src/video/hardware.rs:288`)
- [ ] Add Intel QSV, AMD VCE, Apple VideoToolbox (`src/video/hardware.rs:312`)
- [ ] Check openh264 API for quality settings (`src/video/h264.rs:44`)
- [ ] Implement flush for H.264 encoder (`src/video/h264.rs:149`)
- [ ] Implement actual H.264 decoding (`src/video/h264.rs:234`)

#### Windows Platform
- [ ] dmDisplayOrientation is not available in current winapi version (`src/capture/windows.rs:421`)

#### Dependencies
- [ ] Add ffmpeg-next, vpx, opus dependencies (`Cargo.toml:25`)
- [ ] Add WebRTC when implementing transport (`Cargo.toml:31`)

### API (RemoteC.Api)

#### Command Execution Service
- [ ] Get actual user for command execution (`Services/CommandExecutionService.cs:192`)

#### Remote Control Provider Factory
- [ ] Pass configuration to Rust provider (`Services/RemoteControlProviderFactory.cs:67`)

#### Rust Provider
- [ ] Get actual statistics from Rust transport (`Core.Interop/RustRemoteControlProvider.cs:212`)

## Future Enhancements

### Performance
- [ ] Achieve <50ms latency performance target
- [ ] Add performance benchmarks and optimize
- [ ] Complete H.264 encoding implementation with OpenH264
- [ ] Implement QUIC transport layer

### Code Quality
- [ ] Remove unused imports and variables
- [ ] Automate Rust DLL copy to API output directory
- [ ] Consider switching from winapi to windows crate
- [ ] Readdress enigo integration for input control

### Additional Features
- [ ] Add WebRTC transport option
- [ ] Implement actual hardware-accelerated encoding
- [ ] Add comprehensive integration tests
- [ ] Implement session recording with encryption
- [ ] Add multi-monitor support improvements

## Platform Support Matrix

| Feature | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Screen Capture | ✅ | ❌ | ❌ |
| Input Control | ✅ | ❌ | ❌ |
| H.264 Encoding | 🟡 | ❌ | ❌ |
| Hardware Accel | ❌ | ❌ | ❌ |

Legend: ✅ Complete | 🟡 Partial | ❌ Not Implemented

## Notes

- The Rust provider is now the default remote control engine
- ControlR remains available as a fallback option
- Provider can be switched via appsettings.json or the Settings UI
- Cross-compilation from WSL to Windows is working using MinGW toolchain
- The Rust DLL needs to be manually copied to the API bin directory (automation needed)

## Testing Checklist

Before marking the remote control as fully functional:

1. [ ] Test session creation via API
2. [ ] Verify SignalR connection for real-time updates
3. [ ] Test screen capture functionality
4. [ ] Verify mouse/keyboard input control
5. [ ] Test provider switching between Rust and ControlR
6. [ ] Verify performance metrics collection
7. [ ] Test error handling and graceful degradation
8. [ ] Validate security (authentication, authorization)
9. [ ] Test concurrent session handling
10. [ ] Verify resource cleanup on session end