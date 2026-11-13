//! Release checks through `craft-update`, the library every Craft App shares. Only public release
//! metadata is requested.

use photocraft_engine::updates_cmds::{CheckFn, CheckResult, Release};

pub fn service() -> CheckFn {
    Box::new(|| {
        let (tx, rx) = std::sync::mpsc::channel();
        std::thread::Builder::new()
            .name("update check".into())
            .spawn(move || {
                let result = check();
                if let Err(e) = &result {
                    log::warn!("Update check: {e}");
                }
                let _ = tx.send(result);
            })
            .map_err(|e| format!("Could not start update check: {e}"))?;
        Ok(rx)
    })
}

fn check() -> CheckResult {
    let http = craft_update::Http::new("PhotoLoja/Update")?;
    let found = craft_update::check_for_update(&photocraft_ui_egui::UPDATE_APP, &http, photocraft_engine::build_info::VERSION)?;
    Ok(found.and_then(|release| {
        let version = release.tag.strip_prefix('v')?;
        let installer = format!("PhotoLoja-dos-Rapazes-{version}-Setup.exe");
        let asset = release.assets.into_iter().find(|asset| asset.name == installer)?;
        Some(Release { version: version.to_string(), url: asset.url })
    }))
}
