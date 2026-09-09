namespace MeshDriveSync;
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var argument = args.FirstOrDefault(x => x.StartsWith("--profile=", StringComparison.OrdinalIgnoreCase));
        string? domain = argument?.Split('=', 2)[1];
        if (string.IsNullOrWhiteSpace(domain))
        {
            using var selector = new ProfileSelectorForm();
            if (selector.ShowDialog() != DialogResult.OK) return;
            domain = selector.SelectedDomain;
        }
        domain = ProfileContext.NormalizeDomain(domain!);
        if (!ProfileContext.IsValidDomain(domain)) { MessageBox.Show("Perfil de domínio inválido."); return; }
        ProfileContext.Domain = domain;
        using var mutex = new Mutex(true, "MeshDriveSync-v2.1.0-" + domain, out var first);
        if (!first) { MessageBox.Show($"O domínio '{domain}' já está em execução."); return; }
        Application.Run(new MainForm(args.Contains("--background")));
    }
}
