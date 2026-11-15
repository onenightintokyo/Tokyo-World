@echo off
chcp 65001 >nul
title Tokyo World MEGA - duza paczka modow
echo ================================================================
echo              TOKYO WORLD MEGA EXPANSION
echo ================================================================
echo.
echo Profil MEGA: kilkadziesiat projektow z 4 dziedzin.
echo Minecraft: Forge 1.20.1
echo Nowe: technika, automatyka, magia, lokacje, bossowie, budowanie
echo.
echo UWAGA: Zamknij Minecraft i wykonaj backup swiata w Prism Launcher.
echo Sprawdz tez wolne miejsce na dysku i RAM dostepny podczas gry.
echo Nie nadpisujemy istniejacych plikow .jar i save'ow.
echo.
echo KROK 1 - sprawdzam wersje i zaleznosci (brak pobierania JAR)...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Mega
if errorlevel 1 (
  echo.
  echo BLAD podgladu. Niczego nie instalowano.
  pause
  exit /b 1
)
echo.
echo Mozesz pobrac wszystkie znalezione, zgodne mody jednym uruchomieniem.
echo Brakujace/niedostepne wersje sa zapisane w raporcie, nie blokuja reszty.
set /p ANSWER=Zainstalowac paczke MEGA? [T/N]:
if /I not "%ANSWER%"=="T" (
  echo Anulowano - bez zmian.
  pause
  exit /b 0
)
echo.
echo KROK 2 - pobieranie, weryfikacja SHA-512 i instalacja...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install-TokyoModules.ps1" -Profile Mega -Apply
if errorlevel 1 (
  echo.
  echo INSTALACJA NIEUDANA: JARy nie zostaly zmienione lub zostaly wycofane.
  echo Pobrane pliki z prawidlowa suma kontrolna beda uzyte przy kolejnej probie.
  echo Pokaz zawartosc okna, aby poprawic blad.
) else (
  echo.
  echo INSTALATOR ZAKONCZYL DZIALANIE.
  echo Uruchom Minecraft w Prism, sprawdz ladowanie modow i log.
  echo Jesli gra dziala, uruchom ja jeszcze raz aby sprawdzic questy.
)
echo.
pause
