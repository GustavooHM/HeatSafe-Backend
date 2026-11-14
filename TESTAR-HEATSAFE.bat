@echo off
setlocal
cd /d "%~dp0"
set "DOTNET=%USERPROFILE%\.dotnet\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=dotnet"

echo ========================================
echo HEATSAFE - RESTORE, BUILD E TESTES (VSTEST)
echo ========================================
"%DOTNET%" --version
if errorlevel 1 goto erro
"%DOTNET%" restore .\HeatSafe.sln
if errorlevel 1 goto erro
"%DOTNET%" build .\HeatSafe.sln --no-restore
if errorlevel 1 goto erro
"%DOTNET%" test .\HeatSafe.Tests\HeatSafe.Tests.csproj --no-build --configuration Debug --logger "console;verbosity=normal"
if errorlevel 1 goto erro
echo.
echo VALIDACAO CONCLUIDA: restore, build e testes passaram.
pause
exit /b 0
:erro
echo.
echo ERRO: a etapa acima falhou. Nao considere a validacao aprovada.
pause
exit /b 1
