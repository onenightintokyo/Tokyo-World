# Tokyo World — automatyczne aktualizacje (Windows, Prism Launcher)

## Co to robi?

Skrypt `tools/Update-TokyoWorld.ps1` pobiera zmiany z prywatnego repozytorium GitHub (przez uwierzytelnione `git pull`) oraz kopiuje pliki do katalogu Minecrafta.

Synchronizowane katalogi:
- `config/` — ustawienia modów i docelowo `config/ftbquests/quests/`
- `defaultconfigs/` — domyślne konfiguracje nowych światów
- `kubejs/` — receptury, tagi, skrypty, assety
- `resourcepacks/` — grafiki i tekstury
- `shaderpacks/` — shadery, jeśli dodamy je do projektu

Skrypt **nie synchronizuje modów `.jar`**, nie usuwa plików i nie nadpisuje save'ów. Zmieniane pliki lokalne kopiuje przed podmianą do folderu `.tokyo-world-backups/`.

## Pierwsza instalacja

1. Zainstaluj [Git for Windows](https://git-scm.com/downloads/win).
2. Zaloguj się na GitHub (np. w GitHub Desktop) i sklonuj prywatne repozytorium `onenightintokyo/Tokyo-World` na komputer.
3. W Prism Launcher otwórz instancję **Tokyo World → Edit → Settings → Custom Commands** i zaznacz użycie własnych komend dla tej instancji.
4. W polu **Pre-launch command** wklej poniższą komendę, podstawiając **prawdziwą lokalizację sklonowanego repozytorium**:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\TWOJ_LOGIN\Documents\GitHub\Tokyo-World\tools\Update-TokyoWorld.ps1"
```

**Ważne:** `TWOJ_LOGIN` to tylko przykład; skopiuj dokładną ścieżkę pliku `Update-TokyoWorld.ps1` z Eksploratora Windows. Jeśli klonujesz repozytorium w innym miejscu, wklej tę inną ścieżkę. Nie wklejaj hasła ani tokenu GitHub do komendy.

5. Uruchom instancję. Przed startem gry skrypt sprawdzi `main`, pobierze zmiany i skopiuje nowe pliki.
6. Przy pierwszym `git pull` może być konieczne zalogowanie się w przeglądarce. Prywatne repozytorium wymaga dostępu do odczytu także dla znajomych, którzy mają korzystać z tego samego sposobu aktualizacji.

## Test bez uruchamiania gry

W PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\sciezka\do\Tokyo-World\tools\Update-TokyoWorld.ps1" -GameDir "C:\sciezka\do\instancji\minecraft"
```

Zmień obie ścieżki na swoje rzeczywiste lokalizacje.

## Istotne ograniczenia

- Zmiany z GitHub zaczną się pojawiać u graczy dopiero po skonfigurowaniu tego skryptu w Prism.
- W repozytorium obecnie nie ma jeszcze pełnego zestawu modów, gotowej księgi FTB Quests ani pełnego menu FancyMenu. Skrypt nie tworzy ich samodzielnie.
- Nie zmienia wersji Forge ani Minecrafta.
- Zmiany usuwające pliki w repozytorium nie są automatycznie usuwane z gry (bezpieczny tryb synchronizacji).
- `defaultconfigs` wpływa przede wszystkim na nowe światy; istniejący świat może mieć własne `serverconfig`, który wymaga osobnej migracji.
- Przy niestabilnym połączeniu, skrypt ostrzega i wykorzystuje ostatnią lokalną wersję.
- Zanim dodamy automatyczną instalację i aktualizację `.jar`, przygotujemy osobny manifest **Packwiz**, przetestujemy modpack oraz udostępnimy pliki manifestu przez odpowiedni serwer HTTP(S). Prywatne pliki GitHub nie są automatycznie publicznie dostępne przez `raw.githubusercontent.com`.

## Kolejne kroki po stronie projektu

1. Przenieść komplet sprawdzonych konfiguracji i questbook do repozytorium.
2. Zbudować i przetestować manifest `pack.toml` i `index.toml` w Packwiz.
3. Uruchomić bezpieczny hosting manifestu (np. osobna publiczna dystrybucja bez sekretów).
4. Zastąpić lub rozszerzyć komendę pre-launch tak, aby instalator Packwiz aktualizował także mody.

Dokumentacja Packwiz: https://packwiz.infra.link/tutorials/installing/packwiz-installer/
