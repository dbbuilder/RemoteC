//! Logging module for RemoteC Core
//!
//! Provides logging functionality that can be used from both Rust and FFI.

use log::{Level, Log, Metadata, Record};
use std::ffi::{c_char, CStr};
use std::sync::Once;

static INIT: Once = Once::new();

/// FFI-safe log level
#[repr(C)]
pub enum LogLevel {
    Error = 1,
    Warn = 2,
    Info = 3,
    Debug = 4,
    Trace = 5,
}

impl From<LogLevel> for Level {
    fn from(level: LogLevel) -> Self {
        match level {
            LogLevel::Error => Level::Error,
            LogLevel::Warn => Level::Warn,
            LogLevel::Info => Level::Info,
            LogLevel::Debug => Level::Debug,
            LogLevel::Trace => Level::Trace,
        }
    }
}

/// Simple logger that prints to stdout
struct SimpleLogger;

impl Log for SimpleLogger {
    fn enabled(&self, metadata: &Metadata) -> bool {
        metadata.level() <= Level::Debug
    }

    fn log(&self, record: &Record) {
        if self.enabled(record.metadata()) {
            println!(
                "[{}] {} - {}",
                record.level(),
                record.target(),
                record.args()
            );
        }
    }

    fn flush(&self) {}
}

/// Initialize logging
pub fn init_logging() {
    INIT.call_once(|| {
        log::set_boxed_logger(Box::new(SimpleLogger))
            .map(|()| log::set_max_level(log::LevelFilter::Debug))
            .expect("Failed to initialize logger");
    });
}

/// Log a message from FFI
#[no_mangle]
pub unsafe extern "C" fn remotec_log(
    level: LogLevel,
    target: *const c_char,
    message: *const c_char,
) {
    if target.is_null() || message.is_null() {
        return;
    }

    let target = match CStr::from_ptr(target).to_str() {
        Ok(s) => s,
        Err(_) => return,
    };

    let message = match CStr::from_ptr(message).to_str() {
        Ok(s) => s,
        Err(_) => return,
    };

    log::log!(target: target, level.into(), "{}", message);
}

/// Macro for logging errors with context
#[macro_export]
macro_rules! log_error {
    ($msg:expr) => {
        log::error!("{}", $msg);
    };
    ($fmt:expr, $($arg:tt)*) => {
        log::error!($fmt, $($arg)*);
    };
}

/// Macro for logging warnings with context
#[macro_export]
macro_rules! log_warn {
    ($msg:expr) => {
        log::warn!("{}", $msg);
    };
    ($fmt:expr, $($arg:tt)*) => {
        log::warn!($fmt, $($arg)*);
    };
}

/// Macro for logging info with context
#[macro_export]
macro_rules! log_info {
    ($msg:expr) => {
        log::info!("{}", $msg);
    };
    ($fmt:expr, $($arg:tt)*) => {
        log::info!($fmt, $($arg)*);
    };
}

/// Macro for logging debug with context
#[macro_export]
macro_rules! log_debug {
    ($msg:expr) => {
        log::debug!("{}", $msg);
    };
    ($fmt:expr, $($arg:tt)*) => {
        log::debug!($fmt, $($arg)*);
    };
}