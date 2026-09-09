$ErrorActionPreference="Stop"
Set-Location $PSScriptRoot
Remove-Item .\dist -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\installer -Recurse -Force -ErrorAction SilentlyContinue
if(-not(Test-Path .\Aplicado_Favicon.ico)){try{& .\prepare-icon.ps1}catch{Write-Warning $_.Exception.Message}}
dotnet restore;if($LASTEXITCODE-ne 0){throw "Falha no restore"}
dotnet build -c Release --no-restore;if($LASTEXITCODE-ne 0){throw "Falha no build"}
dotnet publish -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\dist;if($LASTEXITCODE-ne 0){throw "Falha no publish"}
if(-not(Test-Path .\dist\MeshDriveSync.exe)){throw "EXE não criado"}
Write-Host "Gerado: $PSScriptRoot\dist\MeshDriveSync.exe" -ForegroundColor Green
$inno="${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe";if(Test-Path $inno){& $inno .\MeshDriveSync.iss}
