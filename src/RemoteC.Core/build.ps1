# Build script for RemoteC Rust Core on Windows
param(
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "release",
    
    [Parameter(Mandatory=$false)]
    [string]$Target = "x86_64-pc-windows-msvc"
)

Write-Host "Building RemoteC Rust Core for Windows..." -ForegroundColor Green

# Check if Rust is installed
if (!(Get-Command cargo -ErrorAction SilentlyContinue)) {
    Write-Error "Rust is not installed. Please install from https://rustup.rs/"
    exit 1
}

# Add Windows target if not present
Write-Host "Checking Rust target: $Target"
$targets = rustup target list --installed
if (!($targets -contains $Target)) {
    Write-Host "Installing target: $Target"
    rustup target add $Target
}

# Set build flags
$env:RUSTFLAGS = "-C target-cpu=native"

# Build based on configuration
if ($Configuration -eq "release") {
    Write-Host "Building in Release mode..."
    cargo build --release --target $Target --features "windows,openh264"
    $buildPath = "target\$Target\release"
} else {
    Write-Host "Building in Debug mode..."
    cargo build --target $Target --features "windows,openh264"
    $buildPath = "target\$Target\debug"
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# Copy output to .NET interop project
$outputDll = "$buildPath\remotec_core.dll"
$destPath = "..\..\RemoteC.Core.Interop\runtimes\win-x64\native"

if (Test-Path $outputDll) {
    Write-Host "Build successful! Output: $outputDll"
    
    # Create destination directory if it doesn't exist
    if (!(Test-Path $destPath)) {
        New-Item -ItemType Directory -Force -Path $destPath | Out-Null
    }
    
    # Copy DLL
    Copy-Item $outputDll $destPath -Force
    Write-Host "Copied to: $destPath\remotec_core.dll" -ForegroundColor Green
    
    # Also copy to output directory for direct testing
    $testPath = "..\..\RemoteC.Api\bin\Debug\net8.0"
    if (Test-Path $testPath) {
        Copy-Item $outputDll $testPath -Force
        Write-Host "Also copied to: $testPath\remotec_core.dll"
    }
} else {
    Write-Error "Build output not found at: $outputDll"
    exit 1
}

Write-Host "Build completed successfully!" -ForegroundColor Green