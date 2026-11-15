@echo off
chcp 65001 >nul
title Tokyo World Launcher
setlocal
if not exist "%~dp0launcher\TokyoWorldLauncher.ps1" (
  echo [Tokyo World] Brak pliku launcher\TokyoWorldLauncher.ps1.
  echo Zaktualizuj repozytorium poleceniem git pull.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0launcher\TokyoWorldLauncher.ps1"
if errorlevel 1 (
  echo [Tokyo World] Launcher zakonczyl sie bledem. Pokaz komunikat.
  pause
)
endlocal
