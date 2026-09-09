namespace MeshDriveSync;
public static class ProfileContext
{
    public static string Domain { get; set; } = "";
    public static string Data => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeshDriveSync", "profiles");
    public static string NormalizeDomain(string input)
    {
        var value = input.Trim().ToLowerInvariant();
        if (value.StartsWith("http://") || value.StartsWith("https://"))
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) value = uri.Host;
        }
        value = value.Trim().TrimEnd('.');
        if (value.Contains('/')) value = value.Split('/')[0];
        if (value.Contains('\\')) value = value.Split('\\')[0];
        return value;
    }
    public static bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain) || domain.Length > 253 || !domain.Contains('.')) return false;
        return domain.Split('.').All(part => part.Length is > 0 and <= 63 && part[0] != '-' && part[^1] != '-' && part.All(c => char.IsLetterOrDigit(c) || c == '-'));
    }
    public static string Config => Path.Combine(Data, Domain + ".settings.json");
    public static string State => Path.Combine(Data, Domain + ".state.json");
    public static string Log => Path.Combine(Data, Domain + ".log");
    public static string ServerUrl(string domain) => "https://" + domain + "/drive/";
    public static string DefaultLocalFolder(string domain) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Mesh Drive", domain);
    public static List<string> List()
    {
        Directory.CreateDirectory(Data);
        return Directory.EnumerateFiles(Data, "*.settings.json").Select(Path.GetFileName).Select(x => x![..^14]).OrderBy(x => x).ToList();
    }
    public static void Delete(string domain)
    {
        foreach (var suffix in new[] { ".settings.json", ".state.json", ".log" })
        { var file = Path.Combine(Data, domain + suffix); if (File.Exists(file)) File.Delete(file); }
    }
}
