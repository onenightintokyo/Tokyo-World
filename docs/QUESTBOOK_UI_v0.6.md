# Tokyo World — Questbook UI i FancyMenu v0.6

## Co zmieniono

- **FancyMenu**: menu `Customization / Tools / Help` jest ukryte domyślnie dzięki `modpack_mode = true` w lokalnym pliku `config/fancymenu/options.txt`. Aktualizator modyfikuje tylko odpowiednie ustawienia i zapisuje kopię zmienionego pliku. Nie nadpisuje całej konfiguracji FancyMenu.
- **FTB Quests**: 24 rozdziały i 326 questów uporządkowane w **6 zwijanych grup**. Nazwy grup są kolorowe, pogrubione i numerowane. Rozdziały mają uporządkowaną kolejność oraz numery rozdziałów w swoich grupach. Ikony pochodzą z właściwych modów.
- **Błąd formatu**: usunięto dosłowny znak `&` z nazwy `Thermal & Metallurgy`, który FTB Quests traktował jako początek kodu formatowania.
- Zmieniono wyłącznie nazwy, kolejność i grupowanie rozdziałów. **Wszystkie ID questów i ich wymagania pozostają bez zmian.**

## Kategorie

| Kolor | Nazwa | Liczba rozdziałów |
|---|---|---:|
| Złoty | 01 POCZĄTEK | 3 |
| Błękitny | 02 TECHNOLOGIA | 6 |
| Żółty | 03 ENERGIA I MAGAZYNY | 5 |
| Zielony | 04 FARMING I BUDOWANIE | 5 |
| Fioletowy | 05 MAGIA I ALCHIMIA | 2 |
| Czerwony | 06 EKSPLORACJA I BOSSOWIE | 3 |

## Instalacja

1. Zamknij Minecraft.
2. W PowerShell wykonaj:

```powershell
git -C "C:\Tokyo-World" pull --ff-only origin main
```

3. Uruchom Tokyo World w Prism Launcher. Skrypt `tools/Update-TokyoWorld.ps1` synchronizuje nowe definicje FTB Quests i domyślnie ukrywa pasek edycji FancyMenu.
4. W konsoli szukaj `[Tokyo World] FancyMenu editing bar: HIDDEN (modpack mode)`.
5. Uruchom świat i sprawdź, czy 6 grup jest widocznych z poprawnymi nazwami.

## Jak ponownie wejść do edytora FancyMenu

Zamknij grę i uruchom w głównym folderze repozytorium `FANCYMENU_EDYCJA.cmd`. Uruchom Minecraft ponownie. Pasek edycji będzie dostępny.

Po zakończeniu modyfikacji zamknij Minecraft, uruchom `FANCYMENU_GRA.cmd` i włącz grę — wróci ukryty pasek.

Oba skróty ustawiają lokalny znacznik `minecraft/.tokyo-world-state/fancymenu_edit_mode`. Aktualizator rozpoznaje ten znacznik przy następnym uruchomieniu. Nie trzeba edytować pliku GitHub.

## Uwaga

FancyMenu `modpack_mode` ukrywa również skróty do edytora. Jest to zgodne z oficjalną dokumentacją FancyMenu.

Nie mogliśmy przetestować tych zmian wewnątrz działającej gry. Jeżeli grupy nie pojawią się albo książka ma błąd, prześlij `minecraft/logs/latest.log` po wejściu do świata.
