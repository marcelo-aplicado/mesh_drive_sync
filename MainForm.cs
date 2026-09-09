using Microsoft.Win32;

namespace MeshDriveSync;

public sealed class MainForm : Form
{
    static readonly Color Bg = Color.FromArgb(20, 23, 28);
    static readonly Color Surface = Color.FromArgb(30, 34, 41);
    static readonly Color Surface2 = Color.FromArgb(40, 45, 54);
    static readonly Color Accent = Color.FromArgb(40, 120, 220);
    static readonly Color Success = Color.FromArgb(52, 199, 89);
    static readonly Color Danger = Color.FromArgb(255, 69, 58);
    static readonly Color Muted = Color.FromArgb(165, 172, 184);

    readonly TextBox server = Input();
    readonly TextBox user = Input();
    readonly TextBox password = Input(true);
    readonly TextBox local = Input();
    readonly CheckedListBox folders = new() { CheckOnClick = true, Dock = DockStyle.Fill, BackColor = Surface2, ForeColor = Color.White, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), IntegralHeight = false };
    readonly NumericUpDown debounce = Number(1, 300, 10);
    readonly NumericUpDown interval = Number(60, 86400, 600);
    readonly CheckBox startup = Check("Iniciar com o Windows");
    readonly CheckBox syncAll = Check("Sincronizar tudo, incluindo arquivos da raiz");
    readonly Label status = new() { AutoSize = true, Text = "Parado", Font = new Font("Segoe UI Semibold", 13), ForeColor = Muted };
    readonly Label currentFile = new() { AutoSize = true, Text = "Nenhuma atividade recente", ForeColor = Muted, Font = new Font("Segoe UI", 9) };
    readonly Label uploadValue = CardValue("0");
    readonly Label downloadValue = CardValue("0");
    readonly Label conflictValue = CardValue("0");
    readonly Label errorValue = CardValue("0");
    readonly ListBox activity = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Surface, ForeColor = Color.Gainsboro, Font = new Font("Consolas", 9), HorizontalScrollbar = true };
    readonly NotifyIcon tray = new() { Visible = true };
    AppSettings settings;
    SyncEngine? engine;

    public MainForm(bool background)
    {
        settings = AppSettings.Load();
        Text = "Mesh Drive Sync v2.1.0 - " + ProfileContext.Domain;
        tray.Text = TrayText("Parado");
        Width = 980; Height = 760; MinimumSize = new Size(880, 680);
        StartPosition = FormStartPosition.CenterScreen; BackColor = Bg; ForeColor = Color.White;
        Font = new Font("Segoe UI", 10); ApplyIcon(); Build(); LoadValues();
        Shown += (_, _) => { if (background) Hide(); };
        FormClosing += (_, e) => { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } };
        BuildTrayMenu();
        var credential = CredentialManager.Read(Target(settings.ServerUrl));
        if (credential != null && (settings.SyncAll || settings.SelectedFolders.Count > 0))
        { settings.Username = credential.Value.User; user.Text = credential.Value.User; Start(credential.Value.Password); }
    }

    static TextBox Input(bool secret = false) => new() { Dock = DockStyle.Fill, BackColor = Surface2, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10), UseSystemPasswordChar = secret };
    static NumericUpDown Number(decimal min, decimal max, decimal value) => new() { Dock = DockStyle.Left, Width = 160, Minimum = min, Maximum = max, Value = value, BackColor = Surface2, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
    static CheckBox Check(string text) => new() { Text = text, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
    static Label CardValue(string text) => new() { Text = text, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 22) };
    static string Target(string url) => "MeshDriveSync:v2.1.0:" + ProfileContext.Domain + ":" + url;
    string TrayText(string state) { var text = $"Mesh Drive Sync - {ProfileContext.Domain} - {state}"; return text[..Math.Min(63, text.Length)]; }

    void ApplyIcon()
    {
        try { var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); if (icon != null) { Icon = (Icon)icon.Clone(); tray.Icon = (Icon)icon.Clone(); icon.Dispose(); } else tray.Icon = SystemIcons.Application; }
        catch { tray.Icon = SystemIcons.Application; }
    }

    void BuildTrayMenu()
    {
        tray.DoubleClick += (_, _) => ShowApp();
        tray.ContextMenuStrip = new ContextMenuStrip();
        tray.ContextMenuStrip.Items.Add("Abrir", null, (_, _) => ShowApp());
        tray.ContextMenuStrip.Items.Add("Abrir pasta local", null, (_, _) => OpenFolder());
        tray.ContextMenuStrip.Items.Add("Sincronizar agora", null, async (_, _) => { if (engine != null) await engine.Sync(); });
        tray.ContextMenuStrip.Items.Add("Parar sincronização", null, (_, _) => Stop());
        tray.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        tray.ContextMenuStrip.Items.Add("Abrir outro domínio", null, (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Application.ExecutablePath) { UseShellExecute = true }));
        tray.ContextMenuStrip.Items.Add("Sair", null, (_, _) => { tray.Visible = false; Application.Exit(); });
    }

    void Build()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10), Padding = new Point(18, 8) };
        var dashboard = new TabPage("Visão geral") { BackColor = Bg, ForeColor = Color.White, Padding = new Padding(24) };
        var config = new TabPage("Configurações") { BackColor = Bg, ForeColor = Color.White, Padding = new Padding(24) };
        var about = new TabPage("Sobre") { BackColor = Bg, ForeColor = Color.White, Padding = new Padding(24) };
        tabs.TabPages.Add(dashboard); tabs.TabPages.Add(config); tabs.TabPages.Add(about); Controls.Add(tabs);
        BuildDashboard(dashboard); BuildConfig(config); BuildAbout(about);
    }

    void BuildDashboard(Control page)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = Bg };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 125)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); page.Controls.Add(root);
        var header = Panel(Surface, 18); var title = new Label { Text = "Mesh Drive Sync", Font = new Font("Segoe UI Semibold", 24), ForeColor = Color.White, AutoSize = true, Left = 20, Top = 16 }; var domain = new Label { Text = ProfileContext.Domain, Font = new Font("Segoe UI", 10), ForeColor = Muted, AutoSize = true, Left = 23, Top = 58 }; status.Left = 680; status.Top = 32; header.Controls.Add(title); header.Controls.Add(domain); header.Controls.Add(status); root.Controls.Add(header, 0, 0);
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(0, 14, 0, 10) }; for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); cards.Controls.Add(Card("Enviados", uploadValue, Accent), 0, 0); cards.Controls.Add(Card("Recebidos", downloadValue, Color.FromArgb(88, 166, 255)), 1, 0); cards.Controls.Add(Card("Conflitos", conflictValue, Color.FromArgb(255, 159, 10)), 2, 0); cards.Controls.Add(Card("Erros", errorValue, Danger), 3, 0); root.Controls.Add(cards, 0, 1);
        var actionBar = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Bg, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 8, 0, 8) };
        actionBar.Controls.Add(ActionButton("Salvar e iniciar", Accent, (_, _) => SaveStart())); actionBar.Controls.Add(ActionButton("Parar", Danger, (_, _) => Stop())); actionBar.Controls.Add(ActionButton("Sincronizar agora", Surface2, async (_, _) => { if (engine != null) await engine.Sync(); })); actionBar.Controls.Add(ActionButton("Abrir pasta", Surface2, (_, _) => OpenFolder())); root.Controls.Add(actionBar, 0, 2);
        var logCard = Panel(Surface, 14); var logTitle = new Label { Text = "Atividade recente", AutoSize = true, Font = new Font("Segoe UI Semibold", 12), ForeColor = Color.White, Left = 16, Top = 13 }; currentFile.Left = 180; currentFile.Top = 16; activity.Left = 16; activity.Top = 45; activity.Width = 860; activity.Height = 300; activity.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; logCard.Controls.Add(logTitle); logCard.Controls.Add(currentFile); logCard.Controls.Add(activity); root.Controls.Add(logCard, 0, 3);
    }

    void BuildConfig(Control page)
    {
        var card = Panel(Surface, 18); card.Dock = DockStyle.Fill; page.Controls.Add(card);
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 3, RowCount = 11, BackColor = Surface };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230)); form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145)); card.Controls.Add(form); var row = 0;
        AddField(form, "Servidor WebDAV", server, row++); AddField(form, "Usuário", user, row++); AddField(form, "Senha", password, row++); AddField(form, "Pasta local", local, row++, "Selecionar", ChooseFolder);
        form.Controls.Add(FieldLabel("Pastas do Mesh Drive"), 0, row); form.Controls.Add(folders, 1, row); var connect = ActionButton("Conectar e listar", Accent, async (_, _) => await ListFolders()); connect.Dock = DockStyle.Top; form.Controls.Add(connect, 2, row++);
        AddField(form, "Aguardar após alteração (s)", debounce, row++); AddField(form, "Verificação de segurança (s)", interval, row++); form.Controls.Add(syncAll, 1, row++); form.Controls.Add(startup, 1, row++);
        var footer = new FlowLayoutPanel { AutoSize = true, BackColor = Surface }; footer.Controls.Add(ActionButton("Salvar e iniciar", Accent, (_, _) => SaveStart())); footer.Controls.Add(ActionButton("Parar", Danger, (_, _) => Stop())); form.Controls.Add(footer, 1, row);
    }

    void BuildAbout(Control page)
    {
        var card = Panel(Surface, 24); card.Dock = DockStyle.Top; card.Height = 260; page.Controls.Add(card);
        card.Controls.Add(new Label { Text = "Mesh Drive Sync", Font = new Font("Segoe UI Semibold", 24), ForeColor = Color.White, AutoSize = true, Left = 24, Top = 24 });
        card.Controls.Add(new Label { Text = "Versão 2.1.0", Font = new Font("Segoe UI", 11), ForeColor = Muted, AutoSize = true, Left = 27, Top = 67 });
        card.Controls.Add(new Label { Text = "Domínio", ForeColor = Muted, AutoSize = true, Left = 27, Top = 112 }); card.Controls.Add(new Label { Text = ProfileContext.Domain, ForeColor = Color.White, AutoSize = true, Left = 140, Top = 112 });
        card.Controls.Add(new Label { Text = "Pasta local", ForeColor = Muted, AutoSize = true, Left = 27, Top = 145 }); var path = new Label { Text = settings.LocalFolder, ForeColor = Color.White, AutoSize = true, Left = 140, Top = 145 }; card.Controls.Add(path);
        var open = ActionButton("Abrir pasta local", Accent, (_, _) => OpenFolder()); open.Left = 24; open.Top = 190; card.Controls.Add(open);
    }

    static Panel Panel(Color color, int padding) => new() { Dock = DockStyle.Fill, BackColor = color, Padding = new Padding(padding), Margin = new Padding(5) };
    static Control Card(string title, Label value, Color color) { var p = Panel(Surface, 15); p.Margin = new Padding(5); var stripe = new Panel { BackColor = color, Dock = DockStyle.Left, Width = 5 }; var l = new Label { Text = title, AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 10), Left = 20, Top = 16 }; value.Left = 20; value.Top = 43; p.Controls.Add(stripe); p.Controls.Add(l); p.Controls.Add(value); return p; }
    static Button ActionButton(string text, Color color, EventHandler click) { var b = new Button { Text = text, AutoSize = true, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9), Margin = new Padding(0, 0, 10, 0), Padding = new Padding(12, 3, 12, 3), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; b.Click += click; return b; }
    static Label FieldLabel(string text) => new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gainsboro, Font = new Font("Segoe UI Semibold", 9) };
    static void AddField(TableLayoutPanel panel, string label, Control control, int row, string? buttonText = null, Action? click = null) { panel.Controls.Add(FieldLabel(label), 0, row); control.Margin = new Padding(3, 6, 8, 6); panel.Controls.Add(control, 1, row); if (buttonText != null) { var b = ActionButton(buttonText, Surface2, (_, _) => click?.Invoke()); b.Dock = DockStyle.Fill; panel.Controls.Add(b, 2, row); } }

    void LoadValues() { server.Text = settings.ServerUrl; user.Text = settings.Username; local.Text = settings.LocalFolder; debounce.Value = Math.Clamp(settings.DebounceSeconds, 1, 300); interval.Value = Math.Clamp(settings.IntervalSeconds, 60, 86400); startup.Checked = settings.StartWithWindows; syncAll.Checked = settings.SyncAll; foreach (var folder in settings.SelectedFolders) folders.Items.Add(folder, true); }
    void ReadValues() { settings.ServerUrl = server.Text.Trim(); settings.Username = user.Text.Trim(); settings.LocalFolder = local.Text.Trim(); settings.DebounceSeconds = (int)debounce.Value; settings.IntervalSeconds = (int)interval.Value; settings.StartWithWindows = startup.Checked; settings.SyncAll = syncAll.Checked; settings.SelectedFolders = folders.CheckedItems.Cast<string>().ToList(); }

    async Task ListFolders()
    {
        try { ReadValues(); var credential = password.TextLength > 0 ? (settings.Username, password.Text) : CredentialManager.Read(Target(settings.ServerUrl)) ?? ("", ""); if (credential.Item2.Length == 0) throw new Exception("Informe usuário e senha."); using var client = new WebDavClient(settings, credential.Item2); var selected = new HashSet<string>(settings.SelectedFolders, StringComparer.OrdinalIgnoreCase); var items = await client.RootFolders(CancellationToken.None); folders.Items.Clear(); foreach (var folder in items) folders.Items.Add(folder, selected.Contains(folder)); MessageBox.Show($"{items.Count} pasta(s) encontrada(s).", "Mesh Drive Sync"); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Falha", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    void SaveStart()
    {
        ReadValues(); if (!settings.SyncAll && settings.SelectedFolders.Count == 0) { MessageBox.Show("Selecione pelo menos uma pasta ou marque Sincronizar tudo."); return; }
        if (password.TextLength > 0) CredentialManager.Save(Target(settings.ServerUrl), settings.Username, password.Text);
        var credential = CredentialManager.Read(Target(settings.ServerUrl)); if (credential == null) { MessageBox.Show("Informe a senha."); return; }
        settings.Save(); SetStartup(); Directory.CreateDirectory(settings.LocalFolder); Start(credential.Value.Password); password.Clear();
    }

    void Start(string pass) { engine?.Dispose(); engine = new SyncEngine(settings, pass, UpdateProgress); engine.Start(); SetState("Sincronizando...", Accent); }
    void Stop() { engine?.Dispose(); engine = null; SetState("Parado", Muted); currentFile.Text = "Sincronização interrompida"; }
    void SetState(string text, Color color) { status.Text = text; status.ForeColor = color; tray.Text = TrayText(text); }
    void UpdateProgress(ProgressInfo p) { if (IsDisposed) return; BeginInvoke(() => { SetState(p.Status, p.Error ? Danger : p.Status == "Sincronizado" ? Success : Accent); uploadValue.Text = p.Up.ToString(); downloadValue.Text = p.Down.ToString(); conflictValue.Text = p.Conflicts.ToString(); errorValue.Text = p.Errors.ToString(); if (p.File.Length > 0) { currentFile.Text = p.File; activity.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {p.File}"); while (activity.Items.Count > 300) activity.Items.RemoveAt(activity.Items.Count - 1); } }); }
    void SetStartup() { using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"); if (settings.StartWithWindows) key.SetValue("MeshDriveSync-" + ProfileContext.Domain, "\"" + Application.ExecutablePath + "\" --profile=\"" + ProfileContext.Domain + "\" --background"); else key.DeleteValue("MeshDriveSync-" + ProfileContext.Domain, false); }
    void ChooseFolder() { using var dialog = new FolderBrowserDialog(); if (dialog.ShowDialog() == DialogResult.OK) local.Text = dialog.SelectedPath; }
    void OpenFolder() { if (Directory.Exists(local.Text)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(local.Text) { UseShellExecute = true }); }
    void ShowApp() { Show(); WindowState = FormWindowState.Normal; Activate(); }
    protected override void Dispose(bool disposing) { if (disposing) { engine?.Dispose(); tray.Dispose(); } base.Dispose(disposing); }
}
