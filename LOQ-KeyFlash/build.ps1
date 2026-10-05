$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
dotnet restore
dotnet build -c Release
Write-Host "Build complete: bin\Release\net8.0-windows\win-x64\LOQ-KeyFlash.exe"
