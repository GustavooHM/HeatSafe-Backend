$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location (Join-Path $root 'HeatSafe.Api')

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnetCommand) {
    $dotnet = $dotnetCommand.Source
} else {
    $dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
    if (-not (Test-Path $dotnet)) { throw 'SDK .NET não encontrado. Verifique a instalação.' }
}

& $dotnet run
if ($LASTEXITCODE -ne 0) { throw 'A API encerrou com erro.' }
