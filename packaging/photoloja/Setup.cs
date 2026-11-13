using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Instalador do PhotoLoja dos Rapazes")]
[assembly: AssemblyProduct("PhotoLoja dos Rapazes")]
[assembly: AssemblyVersion("0.23.5.0")]

internal sealed class Setup : Form
{
    private const string Product = "PhotoLoja dos Rapazes";
    private readonly Color ink = Color.FromArgb(10, 27, 50);
    private readonly Color blue = Color.FromArgb(46, 113, 225);
    private readonly Color line = Color.FromArgb(210, 223, 244);
    private readonly Panel surface;
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
        ClientSize = new Size(1080, 730);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(835, 690);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(242, 247, 255);
        ForeColor = ink;
        Font = new Font("Segoe UI", 10);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("PhotoLoja.logo"))
        using (var source = Image.FromStream(resource))
            logo = new Bitmap(source);
        surface = new Panel { Size = new Size(784, 645) };
        Controls.Add(surface);
        Resize += delegate { CenterContent(); };
        CenterContent();
        surface.Controls.Add(new PictureBox { Image = logo, SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(0, 0, 58, 58) });
        LabelAt(Product, 0, 88, 784, 54, 30, true);
        LabelAt("Instale seu editor de imagens", 0, 141, 784, 43, 23, true);
        LabelAt("Edite imagens, pinte e trabalhe com arquivos PSD no Windows.", 0, 204, 784, 32, 11, false);
        Card("◇", "Camadas e máscaras", "Edite sem perder o original", 0);
        Card("✎", "Pincéis", "Ajuste pressão e traço", 266);
        Card("▤", "Arquivos PSD", "Abra e salve com camadas", 532);

        LabelAt("Pasta de instalação", 0, 369, 784, 24, 10, true);
        var input = new RoundedPanel(Color.White, line) { Bounds = new Rectangle(0, 400, 640, 40) };
        surface.Controls.Add(input);
        folder.BorderStyle = BorderStyle.None;
        folder.SetBounds(13, 11, 614, 22);
        folder.Text = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\PhotoLoja", "InstallDir", null)
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "PhotoLoja");
        input.Controls.Add(folder);
        choose = ButtonAt("Escolher…", 654, 400, 130, false);
        choose.Click += delegate {
            using (var dialog = new FolderBrowserDialog { Description = "Pasta de instalação do PhotoLoja", SelectedPath = folder.Text })
                if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
        };

        progress.SetBounds(0, 479, 784, 5);
        surface.Controls.Add(progress);
        status = LabelAt("Instalação para sua conta do Windows.", 0, 498, 784, 28, 10, false);
        install = ButtonAt("Instalar PhotoLoja", 0, 543, 178, true);
        install.Click += async delegate { await InstallAsync(); };
        launch = ButtonAt("Abrir PhotoLoja", 192, 543, 155, false);
        launch.Visible = false;
        launch.Click += delegate {
            try { Process.Start(Path.Combine(folder.Text, "photoloja.exe")); Close(); }
            catch (Exception error) { status.Text = error.Message; }
        };
        LabelAt("PhotoLoja dos Rapazes 0.23.5 · Windows x64", 0, 610, 784, 24, 9, false);
        AcceptButton = install;
    }

    private Label LabelAt(string text, int x, int y, int width, int height, int size, bool bold)
    {
        var label = new Label { Text = text, Bounds = new Rectangle(x, y, width, height),
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = ink };
        surface.Controls.Add(label);
        return label;
    }

    private void CenterContent()
    {
        surface.Location = new Point(Math.Max(16, (ClientSize.Width - surface.Width) / 2),
            Math.Max(12, (ClientSize.Height - surface.Height) / 2));
    }

    private static GraphicsPath Rounded(int width, int height, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(width - diameter, 0, diameter, diameter, 270, 90);
        path.AddArc(width - diameter, height - diameter, diameter, diameter, 0, 90);
        path.AddArc(0, height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private sealed class RoundedPanel : Panel
    {
        private readonly Color fill;
        private readonly Color border;

        internal RoundedPanel(Color fill, Color border)
        {
            this.fill = fill;
            this.border = border;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var shape = Rounded(Width - 1, Height - 1, 10))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                e.Graphics.FillPath(brush, shape);
                e.Graphics.DrawPath(pen, shape);
            }
        }
    }

    private void Card(string icon, string title, string detail, int x)
    {
        var card = new RoundedPanel(Color.White, line) { Bounds = new Rectangle(x, 259, 252, 90) };
        card.Controls.Add(new Label { Text = icon, ForeColor = blue, BackColor = Color.White,
            Bounds = new Rectangle(16, 9, 42, 25), Font = new Font("Segoe UI Symbol", 13) });
        card.Controls.Add(new Label { Text = title, BackColor = Color.White,
            Bounds = new Rectangle(16, 38, 220, 22), Font = new Font("Segoe UI Semibold", 10) });
        card.Controls.Add(new Label { Text = detail, BackColor = Color.White,
            Bounds = new Rectangle(16, 62, 220, 20), Font = new Font("Segoe UI", 9) });
        surface.Controls.Add(card);
    }

    private Button ButtonAt(string title, int x, int y, int width, bool primary)
    {
        var button = new Button { Text = title, Bounds = new Rectangle(x, y, width, 40),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? blue : Color.White, ForeColor = primary ? Color.White : ink,
            Font = new Font("Segoe UI Semibold", 10) };
        button.FlatAppearance.BorderColor = primary ? blue : line;
        Action round = delegate {
            using (var path = Rounded(button.Width, button.Height, 8))
            {
                var old = button.Region;
                button.Region = new Region(path);
                if (old != null) old.Dispose();
            }
        };
        button.SizeChanged += delegate { round(); };
        round();
        surface.Controls.Add(button);
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
