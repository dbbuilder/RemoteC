//! Test module for RemoteC Core
//! 
//! This module contains comprehensive tests for all RemoteC Core functionality.
//! Tests are organized by feature area and follow TDD methodology.

pub mod frame_encoding_tests;

// Re-export common test utilities
pub use frame_encoding_tests::*;