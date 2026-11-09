use std::fs::OpenOptions;
use std::io::Write;
use std::net::TcpStream;
use std::time::Duration;
use tauri::{Manager, Url};
use tauri_plugin_shell::ShellExt;

fn log_path(app: &tauri::AppHandle) -> std::path::PathBuf {
    app.path()
        .app_data_dir()
        .unwrap_or_else(|_| std::env::current_dir().unwrap_or_default())
        .join("tauri-desktop.log")
}

fn log_msg(app: &tauri::AppHandle, msg: &str) {
    let p = log_path(app);
    if let Some(parent) = p.parent() {
        let _ = std::fs::create_dir_all(parent);
    }
    if let Ok(mut f) = OpenOptions::new().create(true).append(true).open(p) {
        let _ = writeln!(f, "{}", msg);
    }
}

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
            std::thread::spawn(move || {
                log_msg(&handle, "desktop setup started");
                // Если API уже запущен (dev) — просто переходим на него.
                if !wait_for_port(port, 4) {
                    let save_dir = handle
                        .path()
                        .app_data_dir()
                        .unwrap_or_else(|_| std::env::current_dir().unwrap_or_default())
                        .join("saves");
                    let _ = std::fs::create_dir_all(&save_dir);
                    log_msg(&handle, &format!("starting sidecar, save_dir={}", save_dir.display()));
                    match handle.shell().sidecar("GameApi") {
                        Ok(cmd) => {
                            let content_root = save_dir.join("appdata");
                            let _ = std::fs::create_dir_all(&content_root);
                            match cmd
                                .env("ASPNETCORE_URLS", format!("http://127.0.0.1:{port}"))
                                .env("ASPNETCORE_CONTENTROOT", content_root.to_string_lossy().to_string())
                                .env("ASPNETCORE_ENVIRONMENT", "Production")
                                .env("RAID_SAVE_DIR", save_dir.to_string_lossy().to_string())
                                .current_dir(save_dir.clone())
                                .spawn()
                            {
                                Ok((_rx, child)) => log_msg(&handle, &format!("sidecar spawned pid={:?}", child.pid())),
                                Err(e) => log_msg(&handle, &format!("sidecar spawn error: {e}")),
                            }
                        }
                        Err(e) => log_msg(&handle, &format!("sidecar create error: {e}")),
                    }
                } else {
                    log_msg(&handle, "API already running");
                }
                if wait_for_port(port, 100) {
                    log_msg(&handle, "API ready, navigating");
                    if let Some(win) = handle.get_webview_window("main") {
                        let url = Url::parse(&format!("http://127.0.0.1:{port}")).unwrap();
                        let _ = win.navigate(url);
                    } else {
                        log_msg(&handle, "main window not found");
                    }
                } else {
                    log_msg(&handle, "API timeout");
                    if let Some(win) = handle.get_webview_window("main") {
                        let html = format!("data:text/html;charset=utf-8,<html><body style='background:%231a1714;color:%23ff8b7a;font:16px Segoe UI;padding:24px'><h2 style='color:%23f3e7d3'>Raid Game</h2><p>Не удалось запустить сервер игры (127.0.0.1:{port}).</p><p>Лог: {}</p></body></html>", log_path(&handle).display());
                        let _ = win.navigate(Url::parse(&html).unwrap());
                    }
                }
            });
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
