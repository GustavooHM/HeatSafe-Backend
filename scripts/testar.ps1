$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$localDotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if (Test-Path $localDotnet) {
    $dotnet = $localDotnet
} elseif ($dotnetCommand) {
    $dotnet = $dotnetCommand.Source
} else {
    throw 'Nao encontrei o executavel dotnet. Verifique a instalacao do SDK .NET 10.'
}

Write-Host '== SDK instalado ==' -ForegroundColor Cyan
& $dotnet --version
if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel consultar o SDK .NET.' }

Write-Host '== Restaurando pacotes ==' -ForegroundColor Cyan
& $dotnet restore .\HeatSafe.sln
if ($LASTEXITCODE -ne 0) { throw 'Falha ao restaurar pacotes NuGet.' }

Write-Host '== Compilando solucao ==' -ForegroundColor Cyan
& $dotnet build .\HeatSafe.sln --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build falhou. Consulte as mensagens acima.' }

Write-Host '== Executando testes via VSTest ==' -ForegroundColor Cyan
& $dotnet test .\HeatSafe.Tests\HeatSafe.Tests.csproj --no-build --configuration Debug --logger 'console;verbosity=normal'
if ($LASTEXITCODE -ne 0) { throw 'A execucao dos testes falhou. Nao considere a validacao aprovada.' }

Write-Host 'Validacao concluida: restore, build e testes passaram.' -ForegroundColor Green
