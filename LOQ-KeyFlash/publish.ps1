$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
Write-Host "Portable app created: bin\Release\net8.0-windows\win-x64\publish\LOQ-KeyFlash.exe"
