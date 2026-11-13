//! PhotoLoja release checks and download confirmation.

use photocraft_engine::updates_cmds::Status;
use serde_json::{Value, json};

use crate::PhotocraftApp;

#[derive(Default)]
pub(crate) struct Runtime {
    startup_checked: bool,
    notified_version: Option<String>,
    save_first: bool,
}

pub(crate) fn check(app: &mut PhotocraftApp, _ctx: &egui::Context, params: Value) -> Result<Value, String> {
    let before = app.session.updates.status.clone();
    let result = app.run("help.checkForUpdates", params);
    if result.is_ok() || (app.session.updates.status != before && matches!(app.session.updates.status, Status::Failed { .. })) {
        app.updates_rt.startup_checked = true;
        app.updates_rt.notified_version = None;
        app.ui.updates_open = true;
    }
    result
}

pub(crate) fn tick(app: &mut PhotocraftApp, ctx: &egui::Context) {
    app.session.updates.poll();
    if app.session.updates.check.is_none() {
        return;
    }
    if !app.updates_rt.startup_checked {
        app.updates_rt.startup_checked = true;
        if matches!(app.session.updates.status, Status::Idle) {
            let _ = app.run("help.checkForUpdates", json!({}));
        }
    }
    if matches!(app.session.updates.status, Status::Checking) {
        ctx.request_repaint_after(std::time::Duration::from_millis(100));
    }
    if let Status::Available { release } = &app.session.updates.status
        && app.updates_rt.notified_version.as_ref() != Some(&release.version)
    {
        app.updates_rt.notified_version = Some(release.version.clone());
        app.ui.updates_open = true;
    }
}

pub(crate) fn toolbar(app: &mut PhotocraftApp, ui: &mut egui::Ui) {
    let status = &app.session.updates.status;
    let available = matches!(status, Status::Available { .. });
    let checking = matches!(status, Status::Checking);
    let (icon, tip) = if available { ("download", "Download update") } else { ("refresh-cw", "Check for updates") };
    if ui.add_enabled_ui(!checking, |ui| crate::icons::button(ui, icon, 28.0, available, tip)).inner.clicked() {
        if available {
            app.ui.updates_open = true;
        } else {
            let _ = check(app, ui.ctx(), json!({}));
        }
    }
}

pub(crate) fn show(app: &mut PhotocraftApp, ctx: &egui::Context) {
    if !app.ui.updates_open || app.discard.is_some() || !app.ui.dialogs.is_empty() || app.file_dialog_open() || app.save_options_open() {
        return;
    }
    let status = app.session.updates.status.clone();
    let mut download = None;
    let modal = egui::Modal::new(egui::Id::unique("photoloja-update")).show(ctx, |ui| {
        ui.set_width(360.0);
        if app.updates_rt.save_first {
            ui.heading("Salve os arquivos antes de atualizar");
            ui.label("Você tem arquivos com alterações não salvas. Salve-os antes de abrir o instalador para evitar perder seu trabalho.");
            if ui.button("Voltar ao editor").clicked() {
                app.ui.updates_open = false;
            }
            return;
        }
        ui.heading("Atualização do PhotoLoja");
        match status {
            Status::Available { release } => {
                ui.label(format!("A versão {} está disponível. Deseja baixar o instalador?", release.version));
                ui.horizontal(|ui| {
                    if ui.button("Baixar atualização").clicked() {
                        if app.has_unsaved_work() || app.saving() {
                            app.updates_rt.save_first = true;
                        } else {
                            download = Some(release.url);
                            app.ui.updates_open = false;
                        }
                    }
                    if ui.button("Agora não").clicked() {
                        app.ui.updates_open = false;
                    }
                });
            }
            status => {
                match status {
                    Status::Checking => { ui.spinner(); ui.label("Verificando no GitHub…"); }
                    Status::UpToDate => { ui.label("Você já está usando a versão mais recente."); }
                    Status::Failed { message } => { ui.label(message); }
                    _ => {}
                }
                if ui.button("Fechar").clicked() {
                    app.ui.updates_open = false;
                }
            }
        }
    });
    if modal.should_close() {
        app.ui.updates_open = false;
    }
    if !app.ui.updates_open {
        app.updates_rt.save_first = false;
    }
    if let Some(url) = download {
        crate::links::open(app, ctx, &url);
    }
}
