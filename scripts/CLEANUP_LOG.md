# Script Cleanup Log

## Essential Scripts to Keep

### Build Scripts
- `build.bat` - Main Windows build script
- `build.sh` - Main Linux/WSL build script

### Development Scripts
- `start-dev-ui.bat` - Start UI in development mode (no Azure AD)
- `start-server-windows.bat` - Start API server
- `start-host-windows.bat` - Start host application

### Installation Scripts
- `install-web-dependencies.bat` - Install npm dependencies

### Utility Scripts
- `check-and-kill-port.bat` - Kill processes on specific ports

## Scripts to Remove (Redundant/Obsolete)

### Redundant Server Start Scripts
- `start-dev-server.bat` - Duplicate of start-server-windows.bat
- `start-dev-server.ps1` - PowerShell version, not needed
- `start-server-dev.bat` - Another duplicate
- `start-server-here.bat` - Redundant
- `start-server-network.bat` - Specific network config, use main script
- `start-server-port-17001.bat` - Port is default anyway
- `start-no-hangfire.bat` - Hangfire removed from project
- `start-dev-simple.bat` - Too many variations

### Redundant Host Scripts
- `start-host-direct.bat` - Use main start-host-windows.bat
- `start-host-network.bat` - Network config should be in appsettings
- `start-host-network-v2.bat` - Another network variant

### Old Fix Scripts (Issues Already Resolved)
- `fix-build-and-test.ps1` - Build issues resolved
- `fix-build-issues.ps1` - Build issues resolved
- `fix-cross-project-reference.ps1` - References fixed
- `fix-screen-capture-tests.ps1` - Tests fixed
- `fix-test-packages.ps1` - Packages fixed
- `disable-hangfire-patch.ps1` - Hangfire removed

### Redundant Test Scripts
- `test-after-fix.ps1` - Generic test script
- `test-screen-capture-direct.ps1` - Specific test
- `test-screen-capture-windows.ps1` - Duplicate
- `test-screencapture-minimal.ps1` - Another duplicate
- `isolate-test-build.ps1` - Debug script

### Redundant Build Scripts
- `build-and-test-direct.ps1` - Use main build script
- `check-build-simple.ps1` - Use main build script
- `check-build-windows.ps1` - Duplicate
- `diagnose-and-fix-build.ps1` - Debug script

### Other Redundant Scripts
- `quick-fix-and-start.bat` - Too vague
- `install-and-start-ui.bat` - Combines two operations
- `start-all-dev.bat` - Better to start components individually
- `start-all-windows.bat` - Better to start components individually

## Scripts to Update

### Rename for Clarity
- `fix-web-dependencies.bat` → Keep as recovery script
- `start-ui-windows.bat` → `start-ui-production.bat`