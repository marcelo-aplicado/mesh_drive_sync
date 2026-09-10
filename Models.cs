using System.Text.Json;
namespace MeshDriveSync;
public enum AccessMode { Unknown, ReadOnly, ReadWrite, NoAccess }
public sealed class MappingProfile {
 public string Id{get;set;}=Guid.NewGuid().ToString("N");
 public string Name{get;set;}="Meu Mesh Drive";
 public string Host{get;set;}="";
 public string RemoteFolder{get;set;}="";
 public string Username{get;set;}="";
 public string PreferredDriveLetter{get;set;}="M";
 public bool AutoSelectDriveLetter{get;set;}=true;
 public bool Enabled{get;set;}=true;
 public bool AutoReconnect{get;set;}=true;
 public bool ConnectAtLogon{get;set;}=true;
 public bool SyncEnabled{get;set;}=false;
 public string LocalFolder{get;set;}="";
 public string LocalBaseFolder{get;set;}="";
 public List<string> SelectedFolders{get;set;}=new();
 public bool SyncAll{get;set;}=true;
 public int IntervalSeconds{get;set;}=600;
 public int DebounceSeconds{get;set;}=10;
 public List<string> Ignore{get;set;}=new(){"*.tmp","~$*","Thumbs.db","*.meshsync-*"};
 public AccessMode Access{get;set;}=AccessMode.Unknown;
 public string ActiveDriveLetter{get;set;}="";
 public string RootServerUrl=>$"https://{Host.Trim().TrimEnd('/')}/drive/";
 public string ServerUrl=>RootServerUrl+(string.IsNullOrEmpty(RemoteFolder)?"":Uri.EscapeDataString(RemoteFolder.Trim('/'))+"/");
 public string UncPath=>$"\\\\{Host.Trim()}@SSL\\drive"+(string.IsNullOrEmpty(RemoteFolder)?"":"\\"+RemoteFolder.Trim('/').Replace('/','\\'));
 public string CredentialTarget=>$"MeshDriveSync:v3:{Host.Trim().ToLowerInvariant()}:{Username.Trim().ToLowerInvariant()}";
 public string DataFolder=>Path.Combine(AppSettings.Data,"profiles",Id);
 public string StateFile=>Path.Combine(DataFolder,"state.json");
 public string LogFile=>Path.Combine(DataFolder,"activity.log");
 public string EffectiveLocalFolder=>BuildLocalFolder(string.IsNullOrWhiteSpace(LocalBaseFolder)?Environment.GetFolderPath(Environment.SpecialFolder.UserProfile):LocalBaseFolder,Host,Name);
 public static string BuildLocalFolder(string baseFolder,string host,string name){var parts=new List<string>{baseFolder,"Mesh Drive"};if(!string.IsNullOrWhiteSpace(host))parts.Add(SafeName(host.Trim()));parts.Add(SafeName(string.IsNullOrWhiteSpace(name)?"Meu Mesh Drive":name.Trim()));return Path.Combine(parts.ToArray());}
 static string SafeName(string value)=>string.Join("_",value.Split(Path.GetInvalidFileNameChars(),StringSplitOptions.RemoveEmptyEntries)).Trim();
}
public sealed class AppSettings {
 public bool StartWithWindows{get;set;}=false;
 public bool StartMinimized{get;set;}=true;
 public int MappingCheckSeconds{get;set;}=60;
 public List<MappingProfile> Mappings{get;set;}=new();
 public static string Data=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MeshDriveSync");
 public static string Config=>Path.Combine(Data,"config.json");
 public static AppSettings Load(){try{return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Config))??new();}catch{return new();}}
 public void Save(){Directory.CreateDirectory(Data);File.WriteAllText(Config,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));}
}
public sealed class FileState{public string Hash{get;set;}="";public string? Etag{get;set;}}
public sealed class StateStore{public Dictionary<string,FileState> Files{get;set;}=new(StringComparer.OrdinalIgnoreCase);public HashSet<string> Folders{get;set;}=new(StringComparer.OrdinalIgnoreCase);public static StateStore Load(string file){try{return JsonSerializer.Deserialize<StateStore>(File.ReadAllText(file))??new();}catch{return new();}}public void Save(string file){Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file,JsonSerializer.Serialize(this));}}
public sealed record RemoteItem(string Path,bool Directory,DateTime? Modified,string? Etag);
public sealed record ProgressInfo(string Status,string File,int Up,int Down,int Conflicts,int Errors,bool Error=false);
