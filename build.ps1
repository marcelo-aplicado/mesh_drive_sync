$ErrorActionPreference="Stop"
Set-Location $PSScriptRoot
Remove-Item .\dist -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\installer -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path .\dist | Out-Null
if(-not(Test-Path .\Aplicado_Favicon.ico)){try{& .\prepare-icon.ps1}catch{Write-Warning $_.Exception.Message}}
dotnet restore;if($LASTEXITCODE-ne 0){throw "Falha no restore"}
dotnet build -c Release --no-restore;if($LASTEXITCODE-ne 0){throw "Falha no build"}

# Portatil: runtime .NET 8 incluido.
$portableDir=Join-Path $PSScriptRoot "dist\portable"
dotnet publish -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $portableDir
if($LASTEXITCODE-ne 0){throw "Falha no publish portatil"}
if(-not(Test-Path "$portableDir\MeshDriveSync.exe")){throw "EXE portatil nao criado"}
Move-Item "$portableDir\MeshDriveSync.exe" ".\dist\MeshDriveSync_Portable.exe" -Force
Remove-Item $portableDir -Recurse -Force

# Enxuto: requer Microsoft .NET 8 Desktop Runtime x64.
$liteDir=Join-Path $PSScriptRoot "dist\lite"
dotnet publish -c Release -r win-x64 --self-contained false --no-restore -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $liteDir
if($LASTEXITCODE-ne 0){throw "Falha no publish enxuto"}
if(-not(Test-Path "$liteDir\MeshDriveSync.exe")){throw "EXE enxuto nao criado"}
Move-Item "$liteDir\MeshDriveSync.exe" ".\dist\MeshDriveSync.exe" -Force
Remove-Item $liteDir -Recurse -Force

Write-Host "Gerados:" -ForegroundColor Green
Write-Host "  $PSScriptRoot\dist\MeshDriveSync_Portable.exe" -ForegroundColor Green
Write-Host "  $PSScriptRoot\dist\MeshDriveSync.exe" -ForegroundColor Green
$inno="${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe";if(Test-Path $inno){& $inno .\MeshDriveSync.iss}
