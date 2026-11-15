@echo off
chcp 65001 >nul
title Tokyo World - Factory
echo Tokyo World: opcjonalny modul Factory
echo Wymagany zamkniety Minecraft i kopia zapasowa swiata.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Factory
if errorlevel 1 (echo Blad planowania: instalacja anulowana.& pause& exit /b 1)
echo.
set /p do_install=Kontynuowac instalacje tego profilu? [T/N]: 
if /I not "%do_install%"=="T" (echo Bez zmian.& pause& exit /b 0)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Factory -Apply
if errorlevel 1 (echo Instalacja nie powiodla sie. Pokaz log.) else (echo Moduly dodane. Wlacz gre i sprawdz log przed nastepna grupa.)
pause
