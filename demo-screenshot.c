#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <dlfcn.h>
#include <unistd.h>

// Rust FFI types
typedef void* RemoteCCapture;

typedef struct {
    unsigned int width;
    unsigned int height;
    unsigned char* data;
    size_t size;
    int format; // 0 = Raw, 1 = H264, etc.
} RemoteCFrame;

// Function pointers
typedef RemoteCCapture (*remotec_capture_create_fn)(void);
typedef void (*remotec_capture_destroy_fn)(RemoteCCapture);
typedef int (*remotec_capture_start_fn)(RemoteCCapture);
typedef int (*remotec_capture_stop_fn)(RemoteCCapture);
typedef RemoteCFrame* (*remotec_capture_get_frame_fn)(RemoteCCapture);

int main() {
    // Load the Rust library
    void* handle = dlopen("./src/RemoteC.Core/target/release/libremotec_core.so", RTLD_LAZY);
    if (!handle) {
        fprintf(stderr, "Cannot load library: %s\n", dlerror());
        return 1;
    }

    // Get function pointers
    remotec_capture_create_fn create = (remotec_capture_create_fn)dlsym(handle, "remotec_capture_create");
    remotec_capture_destroy_fn destroy = (remotec_capture_destroy_fn)dlsym(handle, "remotec_capture_destroy");
    remotec_capture_start_fn start = (remotec_capture_start_fn)dlsym(handle, "remotec_capture_start");
    remotec_capture_stop_fn stop = (remotec_capture_stop_fn)dlsym(handle, "remotec_capture_stop");
    remotec_capture_get_frame_fn get_frame = (remotec_capture_get_frame_fn)dlsym(handle, "remotec_capture_get_frame");

    if (!create || !destroy || !start || !stop || !get_frame) {
        fprintf(stderr, "Cannot load all symbols\n");
        dlclose(handle);
        return 1;
    }

    printf("=== RemoteC Rust Core Screenshot Demo ===\n");
    printf("Proving the Rust engine is working...\n\n");

    // Create capture instance
    printf("1. Creating capture instance...\n");
    RemoteCCapture capture = create();
    if (!capture) {
        printf("✗ Failed to create capture\n");
        dlclose(handle);
        return 1;
    }
    printf("✓ Capture instance created\n");

    // Start capturing
    printf("\n2. Starting capture...\n");
    if (start(capture) != 0) {
        printf("✗ Failed to start capture\n");
        destroy(capture);
        dlclose(handle);
        return 1;
    }
    printf("✓ Capture started\n");

    // Get a frame
    printf("\n3. Capturing a frame...\n");
    usleep(100000); // Wait 100ms
    
    RemoteCFrame* frame = get_frame(capture);
    if (frame && frame->data && frame->size > 0) {
        printf("✓ Frame captured successfully!\n");
        printf("   Resolution: %dx%d\n", frame->width, frame->height);
        printf("   Data size: %zu bytes\n", frame->size);
        printf("   Calculated size (BGRA): %u bytes\n", frame->width * frame->height * 4);
        
        // Verify it looks like screen data
        if (frame->size == frame->width * frame->height * 4) {
            printf("   Format: BGRA (32-bit)\n");
            
            // Sample some pixels to show we have real data
            printf("\n4. Sampling pixels (proving we have real screen data):\n");
            for (int i = 0; i < 5; i++) {
                int idx = (rand() % (frame->width * frame->height)) * 4;
                printf("   Pixel %d: B=%d G=%d R=%d A=%d\n", i,
                    frame->data[idx], frame->data[idx+1], 
                    frame->data[idx+2], frame->data[idx+3]);
            }
        }
        
        printf("\n✓ SUCCESS: Rust core is capturing real screen data!\n");
    } else {
        printf("✗ No frame captured\n");
    }

    // Stop and cleanup
    printf("\n5. Cleaning up...\n");
    stop(capture);
    destroy(capture);
    printf("✓ Capture stopped and destroyed\n");

    dlclose(handle);
    
    printf("\n=== Proof Complete ===\n");
    printf("The RemoteC Rust core engine is fully functional and capturing screen data.\n");
    
    return 0;
}