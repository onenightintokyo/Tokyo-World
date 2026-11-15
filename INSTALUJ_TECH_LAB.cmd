@echo off
chcp 65001 >nul
title Tokyo World - Lab
echo Tokyo World - modul Lab
echo Zamknij Minecrafta i zrob kopie zapasowa swiata przed instalacja.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Lab -Apply
if errorlevel 1 (echo Instalacja nieudana. Nie usuwaj modow, pokaz wynik.) else (echo Gotowe. Przetestuj Minecraft.)
pause
