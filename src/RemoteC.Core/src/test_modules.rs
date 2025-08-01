//! Test modules for RemoteC Core
//! 
//! This module imports test modules from the tests directory.

// Import the frame encoding tests
#[path = "../tests/frame_encoding_tests.rs"]
pub mod frame_encoding_tests;

// Import the basic frame encoding tests
#[path = "../tests/basic_frame_encoding_test.rs"]
pub mod basic_frame_encoding_test;

// Import the frame decoding tests
#[path = "../tests/frame_decoding_tests.rs"]
pub mod frame_decoding_tests;