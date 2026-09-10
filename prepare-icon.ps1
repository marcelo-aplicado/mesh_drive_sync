$ErrorActionPreference="Stop"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/marcelo-aplicado/mesh_branding/main/Aplicado_Favicon.svg" -OutFile ".\Aplicado_Favicon.svg"
$magick=Get-Command magick.exe -ErrorAction SilentlyContinue
if($magick){& $magick.Source -background none -density 512 ".\Aplicado_Favicon.svg" -define icon:auto-resize=256,128,64,48,32,16 ".\Aplicado_Favicon.ico"}else{Write-Warning "ImageMagick não encontrado"}
