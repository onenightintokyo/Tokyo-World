@echo off
chcp 65001 >nul
title Tokyo World - Chapter Expansion
echo =========================================================
echo   TOKYO WORLD: 15 NOWYCH MODOW - CHAPTER EXPANSION
echo =========================================================
echo Zamknij Minecrafta i wykonaj kopie swiata przed instalacja.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Chapter
if errorlevel 1 (echo Nie udalo sie sprawdzic modow.& pause& exit /b 1)
echo.
set /p ANSWER=Czy pobrac wszystkie dostepne mody? [T/N]: 
if /I not "%ANSWER%"=="T" (echo Anulowano.& pause& exit /b 0)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Chapter -Apply
if errorlevel 1 (echo Nieudana instalacja. Pokaz log.) else (echo Instalacja zakonczona. Sprawdz log gry.)
pause
