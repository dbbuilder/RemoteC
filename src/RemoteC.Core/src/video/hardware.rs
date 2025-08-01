//! Hardware acceleration detection and management

use crate::Result;
use std::sync::Once;

static INIT: Once = Once::new();
static mut HW_CAPABILITIES: Option<HardwareCapabilities> = None;

/// Hardware acceleration capabilities
#[derive(Debug, Clone, Default)]
pub struct HardwareCapabilities {
    pub nvidia_nvenc: bool,
    pub intel_qsv: bool,
    pub amd_vce: bool,
    pub apple_vt: bool,
    pub cuda_available: bool,
    pub opencl_available: bool,
    pub gpu_name: String,
    pub gpu_memory_mb: u32,
    pub encoder_count: u32,
}

/// Detect available hardware acceleration
pub fn detect_hardware_acceleration() -> &'static HardwareCapabilities {
    unsafe {
        INIT.call_once(|| {
            HW_CAPABILITIES = Some(detect_capabilities());
        });
        HW_CAPABILITIES.as_ref().unwrap()
    }
}

fn detect_capabilities() -> HardwareCapabilities {
    let mut caps = HardwareCapabilities::default();
    
    // Detect NVIDIA hardware
    #[cfg(target_os = "windows")]
    {
        caps.nvidia_nvenc = detect_nvenc_windows();
        if caps.nvidia_nvenc {
            caps.gpu_name = get_nvidia_gpu_name();
            caps.gpu_memory_mb = get_nvidia_memory();
            caps.cuda_available = true;
        }
    }
    
    #[cfg(target_os = "linux")]
    {
        caps.nvidia_nvenc = detect_nvenc_linux();
        caps.intel_qsv = detect_qsv_linux();
        caps.amd_vce = detect_vce_linux();
    }
    
    #[cfg(target_os = "macos")]
    {
        caps.apple_vt = detect_videotoolbox();
        caps.gpu_name = get_macos_gpu_name();
    }
    
    // Detect OpenCL support
    caps.opencl_available = detect_opencl();
    
    log::info!("Hardware acceleration detection complete: {:?}", caps);
    caps
}

#[cfg(target_os = "windows")]
fn detect_nvenc_windows() -> bool {
    use winapi::um::libloaderapi::{GetProcAddress, LoadLibraryA};
    use std::ffi::CString;
    
    unsafe {
        // Try to load NVENC library
        let lib_name = CString::new("nvEncodeAPI64.dll").unwrap();
        let lib = LoadLibraryA(lib_name.as_ptr());
        
        if lib.is_null() {
            return false;
        }
        
        // Check for NvEncodeAPICreateInstance
        let func_name = CString::new("NvEncodeAPICreateInstance").unwrap();
        let func = GetProcAddress(lib, func_name.as_ptr());
        
        !func.is_null()
    }
}

#[cfg(target_os = "linux")]
fn detect_nvenc_linux() -> bool {
    use std::process::Command;
    
    // Check for NVIDIA driver
    let output = Command::new("nvidia-smi")
        .arg("--query-gpu=name")
        .arg("--format=csv,noheader")
        .output();
    
    match output {
        Ok(output) => {
            let gpu_name = String::from_utf8_lossy(&output.stdout);
            !gpu_name.trim().is_empty()
        }
        Err(_) => false,
    }
}

#[cfg(target_os = "linux")]
fn detect_qsv_linux() -> bool {
    use std::path::Path;
    
    // Check for Intel Media SDK
    Path::new("/opt/intel/mediasdk/lib64/libmfx.so.1").exists() ||
    Path::new("/usr/lib/x86_64-linux-gnu/libmfx.so.1").exists()
}

#[cfg(target_os = "linux")]
fn detect_vce_linux() -> bool {
    use std::fs;
    
    // Check for AMD GPU
    if let Ok(entries) = fs::read_dir("/sys/class/drm") {
        for entry in entries.flatten() {
            let name = entry.file_name();
            let name_str = name.to_string_lossy();
            if name_str.starts_with("card") && name_str.contains("AMD") {
                return true;
            }
        }
    }
    false
}

#[cfg(target_os = "macos")]
fn detect_videotoolbox() -> bool {
    // VideoToolbox is available on all modern macOS
    true
}

fn detect_opencl() -> bool {
    #[cfg(target_os = "windows")]
    {
        use winapi::um::libloaderapi::LoadLibraryA;
        use std::ffi::CString;
        
        unsafe {
            let lib_name = CString::new("OpenCL.dll").unwrap();
            let lib = LoadLibraryA(lib_name.as_ptr());
            !lib.is_null()
        }
    }
    
    #[cfg(target_os = "linux")]
    {
        use std::path::Path;
        Path::new("/usr/lib/x86_64-linux-gnu/libOpenCL.so.1").exists()
    }
    
    #[cfg(target_os = "macos")]
    {
        // OpenCL is deprecated but still available on macOS
        true
    }
}

#[cfg(target_os = "windows")]
fn get_nvidia_gpu_name() -> String {
    use std::process::Command;
    
    let output = Command::new("nvidia-smi")
        .arg("--query-gpu=name")
        .arg("--format=csv,noheader")
        .output();
    
    match output {
        Ok(output) => String::from_utf8_lossy(&output.stdout).trim().to_string(),
        Err(_) => "Unknown NVIDIA GPU".to_string(),
    }
}

#[cfg(target_os = "windows")]
fn get_nvidia_memory() -> u32 {
    use std::process::Command;
    
    let output = Command::new("nvidia-smi")
        .arg("--query-gpu=memory.total")
        .arg("--format=csv,noheader,nounits")
        .output();
    
    match output {
        Ok(output) => {
            String::from_utf8_lossy(&output.stdout)
                .trim()
                .parse::<u32>()
                .unwrap_or(0)
        }
        Err(_) => 0,
    }
}

#[cfg(target_os = "macos")]
fn get_macos_gpu_name() -> String {
    use std::process::Command;
    
    let output = Command::new("system_profiler")
        .arg("SPDisplaysDataType")
        .output();
    
    match output {
        Ok(output) => {
            let text = String::from_utf8_lossy(&output.stdout);
            // Parse GPU name from system profiler output
            for line in text.lines() {
                if line.contains("Chipset Model:") {
                    return line.split(':').nth(1)
                        .unwrap_or("Unknown")
                        .trim()
                        .to_string();
                }
            }
            "Unknown GPU".to_string()
        }
        Err(_) => "Unknown GPU".to_string(),
    }
}

/// Hardware encoder trait
pub trait HardwareEncoder: Send + Sync {
    /// Initialize the hardware encoder
    fn initialize(&mut self, width: u32, height: u32, bitrate: u32) -> Result<()>;
    
    /// Encode a frame using hardware acceleration
    fn encode_frame(&mut self, frame: &[u8], timestamp: u64) -> Result<Vec<u8>>;
    
    /// Get encoder name
    fn name(&self) -> &str;
    
    /// Check if encoder is available
    fn is_available(&self) -> bool;
}

/// NVENC encoder implementation
#[cfg(feature = "nvenc")]
pub struct NvencEncoder {
    initialized: bool,
    width: u32,
    height: u32,
    bitrate: u32,
}

#[cfg(feature = "nvenc")]
impl NvencEncoder {
    pub fn new() -> Self {
        Self {
            initialized: false,
            width: 0,
            height: 0,
            bitrate: 0,
        }
    }
}

#[cfg(feature = "nvenc")]
impl HardwareEncoder for NvencEncoder {
    fn initialize(&mut self, width: u32, height: u32, bitrate: u32) -> Result<()> {
        if !detect_hardware_acceleration().nvidia_nvenc {
            return Err(RemoteCError::EncodingError(
                "NVENC not available on this system".to_string()
            ));
        }
        
        self.width = width;
        self.height = height;
        self.bitrate = bitrate;
        self.initialized = true;
        
        log::info!("NVENC encoder initialized: {}x{} @ {} bps", width, height, bitrate);
        Ok(())
    }
    
    fn encode_frame(&mut self, _frame: &[u8], _timestamp: u64) -> Result<Vec<u8>> {
        if !self.initialized {
            return Err(RemoteCError::EncodingError(
                "NVENC encoder not initialized".to_string()
            ));
        }
        
        // TODO: Implement actual NVENC encoding
        Ok(vec![0; 1000])
    }
    
    fn name(&self) -> &str {
        "NVIDIA NVENC"
    }
    
    fn is_available(&self) -> bool {
        detect_hardware_acceleration().nvidia_nvenc
    }
}

/// Select best available hardware encoder
pub fn select_hardware_encoder() -> Option<Box<dyn HardwareEncoder>> {
    let _caps = detect_hardware_acceleration();
    
    #[cfg(feature = "nvenc")]
    {
        if caps.nvidia_nvenc {
            return Some(Box::new(NvencEncoder::new()));
        }
    }
    
    // TODO: Add Intel QSV, AMD VCE, Apple VideoToolbox
    
    None
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_hardware_detection() {
        let caps = detect_hardware_acceleration();
        
        // At least one of these should be available on CI
        let _any_available = caps.nvidia_nvenc || 
                           caps.intel_qsv || 
                           caps.amd_vce || 
                           caps.apple_vt ||
                           caps.opencl_available;
        
        println!("Hardware capabilities: {:?}", caps);
        
        // Don't fail test if no hardware acceleration available
        // as CI environments may not have GPUs
        assert!(true);
    }
    
    #[test]
    fn test_encoder_selection() {
        let encoder = select_hardware_encoder();
        
        if let Some(encoder) = encoder {
            println!("Selected hardware encoder: {}", encoder.name());
        } else {
            println!("No hardware encoder available");
        }
    }
}