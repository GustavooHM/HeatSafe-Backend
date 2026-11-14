@echo off
setlocal
cd /d "%~dp0\HeatSafe.Api"
set DOTNET=%USERPROFILE%\.dotnet\dotnet.exe
if not exist "%DOTNET%" set DOTNET=dotnet
"%DOTNET%" run --launch-profile HeatSafe.Api
pause
