@echo off
chcp 65001 >nul
title Tokyo World - Expanded
echo Tokyo World - instalacja profilu Expanded
echo Zrob kopie zapasowa swiata i zamknij Minecrafta przed instalacja.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Expanded -Apply
echo.
if errorlevel 1 (echo Instalacja przerwana. Przeslij log, nie usuwaj istniejacych modow.) else (echo Instalacja zakonczona - sprawdz komunikaty powyzej.)
pause
