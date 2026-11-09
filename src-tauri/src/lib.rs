use std::net::TcpStream;
use std::time::Duration;
use tauri::{Manager, Url};
use tauri_plugin_shell::ShellExt;

fn wait_for_port(port: u16, attempts: u32) -> bool {
    for _ in 0..attempts {
        if TcpStream::connect_timeout(
            &format!("127.0.0.1:{port}").parse().unwrap(),
            Duration::from_millis(400),
        )
        .is_ok()
        {
            return true;
        }
        std::thread::sleep(Duration::from_millis(450));
    }
    false
}

#[tauri::command]
fn desktop_get_display(window: tauri::Window) -> serde_json::Value {
    let fs = window.is_fullscreen().unwrap_or(false);
    serde_json::json!({ "fullscreen": fs, "minWidth": 1024, "minHeight": 768 })
}

#[tauri::command]
fn desktop_set_display(window: tauri::Window, mode: String) -> serde_json::Value {
    if mode == "fullscreen" {
        let _ = window.set_fullscreen(true);
    } else {
        let _ = window.set_fullscreen(false);
        if let Ok(size) = window.outer_size() {
            if size.width < 1024 || size.height < 768 {
                let _ = window.set_size(tauri::Size::Physical(tauri::PhysicalSize {
                    width: size.width.max(1024),
                    height: size.height.max(768),
                }));
            }
        }
    }
    let fs = window.is_fullscreen().unwrap_or(false);
    serde_json::json!({ "fullscreen": fs, "minWidth": 1024, "minHeight": 768 })
}

#[tauri::command]
fn desktop_quit(app: tauri::AppHandle) {
    app.exit(0);
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_process::init())
        .invoke_handler(tauri::generate_handler![
            desktop_get_display,
            desktop_set_display,
            desktop_quit
        ])
        .setup(|app| {
            let handle = app.handle().clone();
            let port: u16 = 5000;
            // Если API уже запущен (dev) — просто переходим на него.
            if !wait_for_port(port, 4) {
                let save_dir = app
                    .path()
                    .app_data_dir()
                    .unwrap_or_else(|_| std::env::current_dir().unwrap_or_default())
                    .join("saves");
                let _ = std::fs::create_dir_all(&save_dir);
                let sidecar = app.shell().sidecar("GameApi");
                if let Ok(cmd) = sidecar {
                    let content_root = app
                        .path()
                        .resource_dir()
                        .ok()
                        .map(|p| p.join("binaries"))
                        .unwrap_or_else(|| std::env::current_dir().unwrap_or_default());
                    let _child = cmd
                        .env("ASPNETCORE_URLS", format!("http://127.0.0.1:{port}"))
                        .env("ASPNETCORE_CONTENTROOT", content_root.to_string_lossy().to_string())
                        .env("ASPNETCORE_ENVIRONMENT", "Production")
                        .env("RAID_SAVE_DIR", save_dir.to_string_lossy().to_string())
                        .spawn();
                }
            }
            if wait_for_port(port, 90) {
                if let Some(win) = handle.get_webview_window("main") {
                    let _ = win.navigate(Url::parse(&format!("http://127.0.0.1:{port}")).unwrap());
                }
            }
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
