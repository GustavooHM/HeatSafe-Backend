@echo off
setlocal
cd /d "%~dp0"
echo ================================================
echo HEATSAFE - GERAR MIGRACAO DO MODELO ATUAL
echo ================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERRO: dotnet nao foi encontrado no PATH.
  echo Feche e reabra o VS Code ou configure o PATH do SDK.
  pause
  exit /b 1
)

echo [1/2] Instalando ferramenta local dotnet-ef 10.0.14...
dotnet tool install --tool-path .\.tools dotnet-ef --version 10.0.14
if errorlevel 1 if not exist ".\.tools\dotnet-ef.exe" (
  echo ERRO: nao foi possivel instalar dotnet-ef.
  pause
  exit /b 1
)

echo.
echo [2/2] Gerando migration para alinhar modelo e snapshot...
.\.tools\dotnet-ef.exe migrations add AlinharModeloAtual --project .\HeatSafe.Api\HeatSafe.Api.csproj --startup-project .\HeatSafe.Api\HeatSafe.Api.csproj --output-dir Data\Migrations
if errorlevel 1 (
  echo ERRO: migration nao criada. Copie a mensagem do terminal.
  pause
  exit /b 1
)

echo.
echo Migration criada e snapshot atualizado.
echo Revise a migration antes de aplicar. Se existir heatsafe.db, faca backup.
echo A API aplicara a migration na proxima inicializacao.
echo.
pause
exit /b 0
