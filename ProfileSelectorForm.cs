namespace MeshDriveSync;

public sealed class ProfileSelectorForm : Form
{
    static readonly Color Bg = Color.FromArgb(20, 23, 28);
    static readonly Color Surface = Color.FromArgb(30, 34, 41);
    static readonly Color Accent = Color.FromArgb(40, 120, 220);
    static readonly Color Danger = Color.FromArgb(255, 69, 58);
    static readonly Color Muted = Color.FromArgb(165, 172, 184);
    readonly ListBox profiles = new() { Dock = DockStyle.Fill, BackColor = Surface, ForeColor = Color.White, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 12), IntegralHeight = false };
    public string? SelectedDomain { get; private set; }

    public ProfileSelectorForm()
    {
        Text = "Mesh Drive Sync - Domínios"; Width = 560; Height = 440; MinimumSize = new Size(500, 380); StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg; ForeColor = Color.White; Font = new Font("Segoe UI", 10); ApplyIcon(); Build(); RefreshProfiles();
    }

    void Build()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 3, ColumnCount = 1, BackColor = Bg };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); Controls.Add(root);
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(18) };
        header.Controls.Add(new Label { Text = "Mesh Drive Sync", Font = new Font("Segoe UI Semibold", 22), ForeColor = Color.White, AutoSize = true, Left = 18, Top = 13 });
        header.Controls.Add(new Label { Text = "Selecione um domínio ou cadastre uma nova conexão", Font = new Font("Segoe UI", 9), ForeColor = Muted, AutoSize = true, Left = 21, Top = 53 }); root.Controls.Add(header, 0, 0);
        var listCard = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(16), Margin = new Padding(0, 14, 0, 12) }; listCard.Controls.Add(profiles); root.Controls.Add(listCard, 0, 1);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Bg, FlowDirection = FlowDirection.LeftToRight };
        bar.Controls.Add(Button("Abrir", Accent, (_, _) => OpenSelected())); bar.Controls.Add(Button("Novo domínio", Surface, (_, _) => AddDomain())); bar.Controls.Add(Button("Excluir", Danger, (_, _) => DeleteSelected())); root.Controls.Add(bar, 0, 2);
        profiles.DoubleClick += (_, _) => OpenSelected();
    }

    void ApplyIcon() { try { var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); if (icon != null) { Icon = (Icon)icon.Clone(); icon.Dispose(); } } catch { } }
    static Button Button(string text, Color color, EventHandler click) { var b = new Button { Text = text, AutoSize = true, Height = 38, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9), Padding = new Padding(14, 3, 14, 3), Margin = new Padding(0, 0, 10, 0), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; b.Click += click; return b; }
    void RefreshProfiles() { profiles.Items.Clear(); foreach (var profile in ProfileContext.List()) profiles.Items.Add("  🌐  " + profile); }
    string? SelectedRaw() { if (profiles.SelectedIndex < 0) return null; return ProfileContext.List().ElementAtOrDefault(profiles.SelectedIndex); }
    void OpenSelected() { var domain = SelectedRaw(); if (domain == null) return; SelectedDomain = domain; DialogResult = DialogResult.OK; Close(); }

    void AddDomain()
    {
        using var dialog = new Form { Text = "Novo domínio Mesh", Width = 470, Height = 230, StartPosition = FormStartPosition.CenterParent, Icon = Icon, BackColor = Bg, ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
        var title = new Label { Text = "Adicionar domínio", Font = new Font("Segoe UI Semibold", 18), Left = 22, Top = 20, AutoSize = true };
        var label = new Label { Text = "Domínio, por exemplo: mesh.aplicado.com.br", ForeColor = Muted, Left = 24, Top = 65, AutoSize = true };
        var input = new TextBox { Left = 24, Top = 92, Width = 405, BackColor = Surface, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var create = Button("Criar perfil", Accent, (_, _) => { dialog.DialogResult = DialogResult.OK; dialog.Close(); }); create.Left = 305; create.Top = 140;
        dialog.Controls.Add(title); dialog.Controls.Add(label); dialog.Controls.Add(input); dialog.Controls.Add(create); dialog.AcceptButton = create;
        if (dialog.ShowDialog() != DialogResult.OK) return;
        var domain = ProfileContext.NormalizeDomain(input.Text);
        if (!ProfileContext.IsValidDomain(domain)) { MessageBox.Show("Informe um domínio válido, como mesh.aplicado.com.br."); return; }
        if (ProfileContext.List().Contains(domain, StringComparer.OrdinalIgnoreCase)) { MessageBox.Show("Esse domínio já está cadastrado."); return; }
        ProfileContext.Domain = domain;
        new AppSettings { ServerUrl = ProfileContext.ServerUrl(domain), Username = "", LocalFolder = ProfileContext.DefaultLocalFolder(domain) }.Save();
        RefreshProfiles(); profiles.SelectedIndex = ProfileContext.List().FindIndex(x => x.Equals(domain, StringComparison.OrdinalIgnoreCase));
    }

    void DeleteSelected()
    {
        var domain = SelectedRaw(); if (domain == null) return;
        if (MessageBox.Show($"Excluir o perfil '{domain}'?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        ProfileContext.Delete(domain); RefreshProfiles();
    }
}
