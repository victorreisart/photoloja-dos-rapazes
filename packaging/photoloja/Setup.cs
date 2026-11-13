using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Instalador do PhotoLoja dos rapazes")]
[assembly: AssemblyProduct("PhotoLoja dos rapazes")]
[assembly: AssemblyVersion("0.23.3.0")]

internal sealed class Setup : Form
{
    private const string Product = "PhotoLoja dos rapazes";
    private readonly Color ink = Color.FromArgb(10, 27, 50);
    private readonly Color blue = Color.FromArgb(46, 113, 225);
    private readonly TextBox folder = new TextBox();
    private readonly ProgressBar progress = new ProgressBar();
    private readonly Button choose;
    private readonly Button install;
    private readonly Button launch;
    private readonly Label status;
    private bool busy;
    private Image logo;

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Setup());
    }

    private Setup()
    {
        Text = "Instalar " + Product;
        ClientSize = new Size(700, 525);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(242, 247, 255);
        ForeColor = ink;
        Font = new Font("Segoe UI", 10);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("PhotoLoja.logo"))
        using (var source = Image.FromStream(resource))
            logo = new Bitmap(source);
        Controls.Add(new PictureBox { Image = logo, SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(34, 28, 62, 62) });
        LabelAt(Product, 34, 104, 632, 50, 27, true);
        LabelAt("Instale o editor de imagens no seu computador.", 36, 161, 625, 30, 11, false);
        Card("Camadas e máscaras", "Edite sem perder o original", 36);
        Card("Pincéis", "Ajuste pressão e traço", 248);
        Card("Arquivos PSD", "Abra e salve com camadas", 460);

        LabelAt("Pasta de instalação", 36, 291, 630, 24, 10, true);
        folder.SetBounds(36, 319, 495, 32);
        folder.Text = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\PhotoLoja", "InstallDir", null)
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "PhotoLoja");
        Controls.Add(folder);
        choose = ButtonAt("Escolher…", 543, 316, 121, false);
        choose.Click += delegate {
            using (var dialog = new FolderBrowserDialog { Description = "Pasta de instalação do PhotoLoja", SelectedPath = folder.Text })
                if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
        };

        progress.SetBounds(36, 373, 628, 8);
        Controls.Add(progress);
        status = LabelAt("Instalação para sua conta do Windows.", 36, 391, 630, 28, 10, false);
        install = ButtonAt("Instalar PhotoLoja", 36, 435, 178, true);
        install.Click += async delegate { await InstallAsync(); };
        launch = ButtonAt("Abrir PhotoLoja", 225, 435, 155, false);
        launch.Visible = false;
        launch.Click += delegate {
            try { Process.Start(Path.Combine(folder.Text, "photoloja.exe")); Close(); }
            catch (Exception error) { status.Text = error.Message; }
        };
        LabelAt("PhotoLoja dos rapazes 0.23.3 · Windows x64", 36, 489, 630, 24, 9, false);
        AcceptButton = install;
    }

    private Label LabelAt(string text, int x, int y, int width, int height, int size, bool bold)
    {
        var label = new Label { Text = text, Bounds = new Rectangle(x, y, width, height),
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = ink };
        Controls.Add(label);
        return label;
    }

    private void Card(string title, string detail, int x)
    {
        var card = new Panel { BackColor = Color.White, Bounds = new Rectangle(x, 211, 204, 61) };
        card.Controls.Add(new Label { Text = title, Bounds = new Rectangle(12, 9, 181, 21), Font = new Font("Segoe UI", 10, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = detail, Bounds = new Rectangle(12, 32, 181, 22), Font = new Font("Segoe UI", 9) });
        Controls.Add(card);
    }

    private Button ButtonAt(string title, int x, int y, int width, bool primary)
    {
        var button = new Button { Text = title, Bounds = new Rectangle(x, y, width, 37),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? blue : Color.White, ForeColor = primary ? Color.White : ink };
        button.FlatAppearance.BorderColor = primary ? blue : Color.FromArgb(191, 212, 246);
        Controls.Add(button);
        return button;
    }

    private async Task InstallAsync()
    {
        if (busy) return;
        string path;
        try
        {
            path = Path.GetFullPath(folder.Text.Trim());
            if (!Path.IsPathRooted(folder.Text.Trim()) || path == Path.GetPathRoot(path))
                throw new ArgumentException("Escolha uma pasta de instalação válida.");
        }
        catch (Exception error) { status.Text = error.Message; return; }
        busy = true;
        folder.Enabled = choose.Enabled = install.Enabled = false;
        progress.Style = ProgressBarStyle.Marquee;
        status.Text = "Instalando o PhotoLoja…";
        try
        {
            int result = await Task.Run(() => InstallPackage(path));
            if (result != 0 && result != 3010 && result != 1641)
                throw new InvalidOperationException("O Windows Installer retornou o código " + result + ".");
            folder.Text = path;
            progress.Style = ProgressBarStyle.Blocks;
            progress.Value = 100;
            status.Text = result == 0 ? "Instalação concluída." : "Instalação concluída. O Windows solicitou reinicialização.";
            launch.Visible = File.Exists(Path.Combine(path, "photoloja.exe"));
        }
        catch (Exception error)
        {
            progress.Style = ProgressBarStyle.Blocks;
            progress.Value = 0;
            status.Text = error.Message;
            folder.Enabled = choose.Enabled = install.Enabled = true;
        }
        finally { busy = false; }
    }

    private static int InstallPackage(string destination)
    {
        string scratch = Path.Combine(Path.GetTempPath(), "PhotoLoja-" + Guid.NewGuid().ToString("N"));
        string msi = Path.Combine(scratch, "PhotoLoja.msi");
        Directory.CreateDirectory(scratch);
        try
        {
            using (var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("PhotoLoja.msi"))
            using (var output = new FileStream(msi, FileMode.CreateNew))
                payload.CopyTo(output);
            using (var process = Process.Start(new ProcessStartInfo(
                Path.Combine(Environment.SystemDirectory, "msiexec.exe"),
                "/i \"" + msi + "\" /qn /norestart INSTALLFOLDER=\"" + destination.TrimEnd('\\') + "\"")
                { UseShellExecute = false, CreateNoWindow = true }))
            {
                process.WaitForExit();
                return process.ExitCode;
            }
        }
        finally
        {
            try { File.Delete(msi); Directory.Delete(scratch); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (busy) e.Cancel = true;
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && logo != null) { logo.Dispose(); logo = null; }
    }
}
