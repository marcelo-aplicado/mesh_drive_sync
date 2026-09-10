using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MeshDriveSync;

public sealed class SyncEngine : IDisposable
{
    readonly MappingProfile s;
    readonly WebDavClient dav;
    readonly Action<ProgressInfo> report;
    readonly StateStore state;
    readonly SemaphoreSlim gate = new(1, 1);
    readonly FileSystemWatcher watch;
    readonly CancellationTokenSource cts = new();
    System.Threading.Timer? safetyTimer;
    System.Threading.Timer? eventTimer;

    public SyncEngine(MappingProfile settings, string password, Action<ProgressInfo> progress)
    {
        s = settings;
        s.LocalFolder = s.EffectiveLocalFolder;
        state = StateStore.Load(s.StateFile);
        report = progress;
        dav = new WebDavClient(s, password);
        Directory.CreateDirectory(s.LocalFolder);
        watch = new FileSystemWatcher(s.LocalFolder)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
        };
        watch.Changed += Changed;
        watch.Created += Changed;
        watch.Deleted += Changed;
        watch.Renamed += Changed;
        watch.Error += (_, _) => QueueSync();
    }

    void Changed(object? sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains(".meshsync-", StringComparison.OrdinalIgnoreCase)) return;
        QueueSync();
    }

    void QueueSync()
    {
        eventTimer?.Dispose();
        eventTimer = new System.Threading.Timer(
            async _ => await Sync(), null, Math.Max(1, s.DebounceSeconds) * 1000, Timeout.Infinite);
    }

    public void Start()
    {
        safetyTimer = new System.Threading.Timer(
            async _ => await Sync(), null, 0, Math.Max(60, s.IntervalSeconds) * 1000);
    }

    bool Selected(string relative)
    {
        if (relative.Equals(".Trash", StringComparison.OrdinalIgnoreCase) ||
            relative.StartsWith(".Trash/", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.SyncAll) return true;
        var root = relative.Split('/')[0];
        return s.SelectedFolders.Contains(root, StringComparer.OrdinalIgnoreCase);
    }

    static bool IsInside(string path, string parent) =>
        path.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(parent.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);

    public async Task Sync()
    {
        if (!await gate.WaitAsync(0)) return;
        var up = 0; var down = 0; var conflicts = 0; var errors = 0;
        try
        {
            report(new("Sincronizando...", "", 0, 0, 0, 0));
            var remoteItems = await dav.Items(s.SelectedFolders, s.SyncAll, cts.Token);
            var remoteFiles = remoteItems.Where(x => !x.Directory).ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
            var remoteFolders = new HashSet<string>(remoteItems.Where(x => x.Directory).Select(x => x.Path), StringComparer.OrdinalIgnoreCase);
            var localFolders = new HashSet<string>(Directory.EnumerateDirectories(s.LocalFolder, "*", SearchOption.AllDirectories)
                .Select(Rel).Where(Selected), StringComparer.OrdinalIgnoreCase);

            // Pastas que existiam no estado, continuam remotas e sumiram localmente foram excluídas localmente.
            // Move apenas a pasta ancestral; os descendentes seguem junto no MOVE WebDAV.
            var movedFolders = new List<string>();
            foreach (var folder in remoteFolders.OrderBy(x => x.Count(c => c == '/')).ToList())
            {
                if (localFolders.Contains(folder) || !state.Folders.Contains(folder) || movedFolders.Any(parent => IsInside(folder, parent))) continue;
                try
                {
                    var trashed = await dav.MoveToTrash(folder, cts.Token);
                    movedFolders.Add(folder);
                    Log($"Pasta movida para lixeira: {folder} -> {trashed}");
                }
                catch (Exception ex) { errors++; Log(folder + ": " + ex); }
            }

            if (movedFolders.Count > 0)
            {
                remoteFolders.RemoveWhere(folder => movedFolders.Any(parent => IsInside(folder, parent)));
                foreach (var file in remoteFiles.Keys.Where(file => movedFolders.Any(parent => IsInside(file, parent))).ToList())
                {
                    remoteFiles.Remove(file);
                    state.Files.Remove(file);
                }
                state.Folders.RemoveWhere(folder => movedFolders.Any(parent => IsInside(folder, parent)));
            }

            // Cria no servidor pastas locais novas, inclusive vazias.
            foreach (var folder in localFolders.OrderBy(x => x.Count(c => c == '/')))
            {
                if (remoteFolders.Contains(folder)) continue;
                try
                {
                    await dav.EnsureFolder(folder, cts.Token);
                    remoteFolders.Add(folder);
                    state.Folders.Add(folder);
                }
                catch (Exception ex) { errors++; Log(folder + ": " + ex); }
            }

            // Cria localmente pastas remotas novas, inclusive vazias.
            foreach (var folder in remoteFolders.OrderBy(x => x.Count(c => c == '/')))
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(s.LocalFolder, folder.Replace('/', Path.DirectorySeparatorChar)));
                    state.Folders.Add(folder);
                }
                catch (Exception ex) { errors++; Log(folder + ": " + ex); }
            }

            var localFiles = Directory.EnumerateFiles(s.LocalFolder, "*", SearchOption.AllDirectories)
                .Where(f => Selected(Rel(f)) && !Ignored(f)).ToDictionary(Rel, StringComparer.OrdinalIgnoreCase);

            foreach (var rel in remoteFiles.Keys.Union(localFiles.Keys).OrderBy(x => x))
            {
                try
                {
                    report(new("Sincronizando...", rel, up, down, conflicts, errors));
                    state.Files.TryGetValue(rel, out var old);
                    var hasLocal = localFiles.TryGetValue(rel, out var localFile);
                    var hasRemote = remoteFiles.TryGetValue(rel, out var remoteFile);
                    if (hasLocal && hasRemote)
                    {
                        var hash = await Hash(localFile!);
                        if (old == null)
                        {
                            if (File.GetLastWriteTimeUtc(localFile!) > remoteFile!.Modified.GetValueOrDefault(DateTime.MinValue))
                            { var et = await dav.Upload(rel, localFile!, remoteFile.Etag, cts.Token); Save(rel, hash, et ?? remoteFile.Etag); up++; }
                            else
                            { await dav.Download(rel, localFile!, cts.Token); Save(rel, await Hash(localFile!), remoteFile.Etag); down++; }
                        }
                        else
                        {
                            var localChanged = hash != old.Hash;
                            var remoteChanged = remoteFile!.Etag != null && old.Etag != null && remoteFile.Etag != old.Etag;
                            if (localChanged && remoteChanged)
                            { File.Copy(localFile!, localFile! + ".conflito-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")); await dav.Download(rel, localFile!, cts.Token); Save(rel, await Hash(localFile!), remoteFile.Etag); conflicts++; }
                            else if (localChanged)
                            { var et = await dav.Upload(rel, localFile!, remoteFile.Etag, cts.Token); Save(rel, hash, et ?? remoteFile.Etag); up++; }
                            else if (remoteChanged)
                            { await dav.Download(rel, localFile!, cts.Token); Save(rel, await Hash(localFile!), remoteFile.Etag); down++; }
                        }
                    }
                    else if (hasLocal)
                    { var hash = await Hash(localFile!); Save(rel, hash, await dav.Upload(rel, localFile!, null, cts.Token)); up++; }
                    else if (hasRemote)
                    {
                        if (old != null)
                        { var trashed = await dav.MoveToTrash(rel, cts.Token); state.Files.Remove(rel); Log($"Movido para lixeira: {rel} -> {trashed}"); }
                        else
                        { var target = Path.Combine(s.LocalFolder, rel.Replace('/', Path.DirectorySeparatorChar)); await dav.Download(rel, target, cts.Token); Save(rel, await Hash(target), remoteFile!.Etag); down++; }
                    }
                }
                catch (Exception ex) { errors++; Log(rel + ": " + ex); }
            }
            state.Save(s.StateFile);
            report(new(errors > 0 ? "Concluído com erros" : "Sincronizado", "", up, down, conflicts, errors, errors > 0));
        }
        catch (Exception ex)
        {
            errors++; Log(ex.ToString());
            report(new(ex.Message, "", up, down, conflicts, errors, true));
        }
        finally { gate.Release(); }
    }

    string Rel(string path) => Path.GetRelativePath(s.LocalFolder, path).Replace('\\', '/');
    bool Ignored(string file) => s.Ignore.Any(pattern => Regex.IsMatch(Path.GetFileName(file), "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase));
    static async Task<string> Hash(string file) { using var sha = SHA256.Create(); await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); return Convert.ToHexString(await sha.ComputeHashAsync(stream)); }
    void Save(string path, string hash, string? etag) => state.Files[path] = new() { Hash = hash, Etag = etag };
    void Log(string text) { Directory.CreateDirectory(Path.GetDirectoryName(s.LogFile)!); File.AppendAllText(s.LogFile, DateTime.Now + " " + text + Environment.NewLine); }
    public void Dispose() { cts.Cancel(); eventTimer?.Dispose(); safetyTimer?.Dispose(); watch.Dispose(); dav.Dispose(); gate.Dispose(); cts.Dispose(); }
}
