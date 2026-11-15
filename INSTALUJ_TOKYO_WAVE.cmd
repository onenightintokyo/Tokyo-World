@echo off
chcp 65001 >nul
title Tokyo World - nowa fala modow
echo ===============================================================
echo                  TOKYO WORLD WAVE v0.9
echo ===============================================================
echo 36 nowych modow: technologia, moby, biomy i wygoda.
echo Zamknij Minecraft. Zrob kopie instancji i swiata.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Wave
if errorlevel 1 (echo Blad podgladu. Niczego nie zmieniono.& pause& exit /b 1)
echo.
set /p CONFIRM=Zainstalowac znalezione mody? [T/N]: 
if /I not "%CONFIRM%"=="T" (echo Anulowano.& pause& exit /b 0)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Wave -Apply
if errorlevel 1 (echo Blad instalacji. Pokaz log.) else (echo Gotowe. Testuj na kopii instancji.)
pause
