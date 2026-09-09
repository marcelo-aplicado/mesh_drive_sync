#define AppName "Mesh Drive Sync"
#define AppVersion "2.1.0"
[Setup]
AppId={{93EBCF58-27CB-4724-8A9D-58A70541A420}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Mesh Drive Sync
PrivilegesRequired=lowest
OutputDir=installer
OutputBaseFilename=MeshDriveSync-Setup-2.1.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
#if FileExists("Aplicado_Favicon.ico")
SetupIconFile=Aplicado_Favicon.ico
#endif
[Files]
Source: "dist\MeshDriveSync.exe"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\Mesh Drive Sync"; Filename: "{app}\MeshDriveSync.exe"
[Run]
Filename: "{app}\MeshDriveSync.exe"; Flags: nowait postinstall skipifsilent
