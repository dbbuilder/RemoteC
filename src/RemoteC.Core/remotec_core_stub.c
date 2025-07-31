// Minimal stub implementation for Windows DLL
// This allows testing provider switching without full Rust compilation

#include <windows.h>
#include <stdint.h>
#include <string.h>

// Export functions that match the FFI interface
__declspec(dllexport) int32_t remotec_init() {
    return 0; // Success
}

__declspec(dllexport) void remotec_shutdown() {
    // No-op
}

__declspec(dllexport) void* remotec_capture_create(uint32_t monitor_id) {
    // Return a dummy handle
    return (void*)0x12345678;
}

__declspec(dllexport) void remotec_capture_destroy(void* capture) {
    // No-op
}

__declspec(dllexport) int32_t remotec_capture_frame(void* capture, uint8_t* buffer, uint32_t buffer_size) {
    // Fill with dummy data
    if (buffer && buffer_size > 0) {
        memset(buffer, 128, buffer_size);
    }
    return 0;
}

__declspec(dllexport) void* remotec_input_create() {
    return (void*)0x87654321;
}

__declspec(dllexport) void remotec_input_destroy(void* input) {
    // No-op
}

__declspec(dllexport) int32_t remotec_input_mouse_move(void* input, int32_t x, int32_t y) {
    return 0;
}

__declspec(dllexport) int32_t remotec_input_mouse_click(void* input, uint8_t button, uint8_t is_press) {
    return 0;
}

__declspec(dllexport) int32_t remotec_input_key_event(void* input, uint32_t keycode, uint8_t is_press) {
    return 0;
}

__declspec(dllexport) void* remotec_transport_create(const char* config_json) {
    return (void*)0xABCDEF00;
}

__declspec(dllexport) void remotec_transport_destroy(void* transport) {
    // No-op
}

__declspec(dllexport) int32_t remotec_transport_connect(void* transport, const char* address) {
    return 0;
}

__declspec(dllexport) int32_t remotec_transport_send(void* transport, const uint8_t* data, uint32_t size) {
    return 0;
}

__declspec(dllexport) int32_t remotec_transport_receive(void* transport, uint8_t* buffer, uint32_t buffer_size) {
    // Return no data received
    return 0;
}

// DLL entry point
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
        case DLL_PROCESS_ATTACH:
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }
    return TRUE;
}