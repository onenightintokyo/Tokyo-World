@echo off
chcp 65001 >nul
title Tokyo World - naprawa brakujacych bibliotek
echo ================================================================
echo     TOKYO WORLD - NAPRAWA STARTU MINECRAFT
echo ================================================================
echo Zamknij Minecrafta. Instalator uzupelni tylko 3 brakujace biblioteki.
echo Forge 1.20.1: irons_lib / playerAnimator / Extended AE.
echo Nie nadpisuje istniejacych JAR ani swiata.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Repair -Apply
if errorlevel 1 (
  echo.
  echo BLAD naprawy - pokaz ten komunikat w ChatGPT.
) else (
  echo.
  echo Naprawa zakonczona. Uruchom Tokyo World z Prism Launcher.
)
echo.
pause
