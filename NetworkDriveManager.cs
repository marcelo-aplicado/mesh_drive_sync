using Microsoft.Win32;using System.ComponentModel;using System.Runtime.InteropServices;
namespace MeshDriveSync;
public static class NetworkDriveManager{
 const int ResourceTypeDisk=1,ConnectUpdateProfile=1,NoError=0;
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct NETRESOURCE{public int Scope,Type,DisplayType,Usage;public string? LocalName,RemoteName,Comment,Provider;}
 [DllImport("mpr.dll",CharSet=CharSet.Unicode)]static extern int WNetAddConnection2(ref NETRESOURCE resource,string? password,string? username,int flags);
 [DllImport("mpr.dll",CharSet=CharSet.Unicode)]static extern int WNetCancelConnection2(string name,int flags,bool force);
 public static IReadOnlyList<string> AvailableLetters(){var used=new HashSet<string>(DriveInfo.GetDrives().Select(d=>d.Name[..1]),StringComparer.OrdinalIgnoreCase);return Enumerable.Range('D','Z'-'D'+1).Select(x=>((char)x).ToString()).Where(x=>!used.Contains(x)).ToList();}
 public static bool IsDriveAvailable(string letter)=>AvailableLetters().Contains(letter.Trim().TrimEnd(':'),StringComparer.OrdinalIgnoreCase);
 public static string SelectLetter(MappingProfile p){var preferred=p.PreferredDriveLetter.Trim().TrimEnd(':').ToUpperInvariant();if(preferred.Length!=1)throw new IOException("Selecione uma letra de unidade válida.");if(!IsDriveAvailable(preferred))throw new IOException($"A unidade {preferred}: já está em uso. O mapeamento foi ignorado.");return preferred;}
 public static void Connect(MappingProfile p,string password){var letter=SelectLetter(p);var nr=new NETRESOURCE{Type=ResourceTypeDisk,LocalName=letter+":",RemoteName=p.UncPath};var rc=WNetAddConnection2(ref nr,password,p.Username,ConnectUpdateProfile);if(rc!=NoError)throw new Win32Exception(rc);p.ActiveDriveLetter=letter;SetLabel(p);}
 static void SetLabel(MappingProfile p){try{var keyName="##"+p.Host+"@SSL#drive"+(string.IsNullOrEmpty(p.RemoteFolder)?"":"#"+p.RemoteFolder.Replace('/','#').Replace('\\','#'));using var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2\"+keyName);key.SetValue("_LabelFromReg",p.Name,RegistryValueKind.String);}catch{}try{var shell=Type.GetTypeFromProgID("Shell.Application");dynamic? app=shell==null?null:Activator.CreateInstance(shell);dynamic? item=app?.NameSpace(17)?.ParseName(p.ActiveDriveLetter+":");if(item!=null)item.Name=p.Name;}catch{}}
 public static void Disconnect(MappingProfile p){var l=string.IsNullOrWhiteSpace(p.ActiveDriveLetter)?p.PreferredDriveLetter:p.ActiveDriveLetter;if(!string.IsNullOrWhiteSpace(l))WNetCancelConnection2(l.TrimEnd(':')+":",ConnectUpdateProfile,true);p.ActiveDriveLetter="";}
 public static bool IsConnected(MappingProfile p){var l=string.IsNullOrWhiteSpace(p.ActiveDriveLetter)?p.PreferredDriveLetter:p.ActiveDriveLetter;try{return !string.IsNullOrWhiteSpace(l)&&Directory.Exists(l.TrimEnd(':')+":\\");}catch{return false;}}
}
