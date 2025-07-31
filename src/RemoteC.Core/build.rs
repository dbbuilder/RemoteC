use std::env;

fn main() {
    let target_os = env::var("CARGO_CFG_TARGET_OS").unwrap();
    
    match target_os.as_str() {
        "windows" => {
            println!("cargo:rustc-link-lib=user32");
            println!("cargo:rustc-link-lib=gdi32");
            println!("cargo:rustc-link-lib=dwmapi");
        }
        "linux" => {
            // X11 libraries commented out for now
            // println!("cargo:rustc-link-lib=X11");
            // println!("cargo:rustc-link-lib=Xext");
            // println!("cargo:rustc-link-lib=Xtst");
        }
        "macos" => {
            println!("cargo:rustc-link-lib=framework=CoreGraphics");
            println!("cargo:rustc-link-lib=framework=CoreFoundation");
            println!("cargo:rustc-link-lib=framework=ApplicationServices");
        }
        _ => {}
    }
}