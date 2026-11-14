@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0scripts\testar-integracao-api.ps1"
if errorlevel 1 (
  echo.
  echo O teste de integracao encontrou um erro. Copie a mensagem e envie para revisao.
  pause
  exit /b 1
)
echo.
echo TESTE DE INTEGRACAO CONCLUIDO.
pause
