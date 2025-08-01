#include <stdio.h>
#include <stdlib.h>
#include <dlfcn.h>

// Function pointers for the Rust library
typedef struct {
    unsigned int width;
    unsigned int height;
    unsigned char* data;
    size_t size;
} ScreenCapture;

typedef ScreenCapture* (*capture_screen_fn)(void);
typedef void (*free_screen_capture_fn)(ScreenCapture*);

int main() {
    // Load the Rust library
    void* handle = dlopen("./src/RemoteC.Core/target/release/libremotec_core.so", RTLD_LAZY);
    if (!handle) {
        fprintf(stderr, "Cannot load library: %s\n", dlerror());
        return 1;
    }

    // Get function pointers
    capture_screen_fn capture_screen = (capture_screen_fn)dlsym(handle, "capture_screen");
    free_screen_capture_fn free_screen_capture = (free_screen_capture_fn)dlsym(handle, "free_screen_capture");

    if (!capture_screen || !free_screen_capture) {
        fprintf(stderr, "Cannot load symbols: %s\n", dlerror());
        dlclose(handle);
        return 1;
    }

    printf("=== RemoteC Screenshot Capture Test ===\n");
    printf("Using Rust core library: libremotec_core.so\n\n");

    // Capture the screen
    printf("Capturing screen...\n");
    ScreenCapture* capture = capture_screen();

    if (capture && capture->data) {
        printf("✓ Screen captured successfully!\n");
        printf("  Resolution: %dx%d\n", capture->width, capture->height);
        printf("  Data size: %zu bytes\n", capture->size);
        printf("  Pixel format: BGRA (4 bytes per pixel)\n");

        // Save a small preview (first 100x100 pixels) as PPM
        if (capture->width >= 100 && capture->height >= 100) {
            FILE* fp = fopen("/tmp/remotec-preview.ppm", "wb");
            if (fp) {
                fprintf(fp, "P6\n100 100\n255\n");
                
                // Convert BGRA to RGB for PPM format
                for (int y = 0; y < 100; y++) {
                    for (int x = 0; x < 100; x++) {
                        int idx = (y * capture->width + x) * 4;
                        // BGRA -> RGB
                        fputc(capture->data[idx + 2], fp); // R
                        fputc(capture->data[idx + 1], fp); // G
                        fputc(capture->data[idx + 0], fp); // B
                    }
                }
                
                fclose(fp);
                printf("\n✓ Preview saved to /tmp/remotec-preview.ppm\n");
                
                // Try to convert to PNG
                system("convert /tmp/remotec-preview.ppm /tmp/remotec-preview.png 2>/dev/null");
                if (system("test -f /tmp/remotec-preview.png") == 0) {
                    printf("✓ Converted to PNG: /tmp/remotec-preview.png\n");
                }
            }
        }

        // Free the capture
        free_screen_capture(capture);
    } else {
        printf("✗ Failed to capture screen\n");
        printf("  Make sure X11 display is available\n");
    }

    dlclose(handle);
    
    printf("\n=== Test Complete ===\n");
    return 0;
}