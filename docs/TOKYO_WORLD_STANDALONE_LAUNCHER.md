# Tokyo World — niezależny launcher Windows 1.0

**Bez Prism Launcher i bez osobnej instalacji .NET.** Projekt źródłowy: `launcher-standalone/`. Po kompilacji GitHub Actions dostarcza jeden plik `TokyoWorldLauncher.exe` dla Windows x64.

## Pobierz EXE

W prywatnym repozytorium otwórz:

**Actions → Build Tokyo World Standalone Launcher → ostatni pomyślny run → Artifacts → TokyoWorld-Standalone-Windows-x64**

Pobierz ZIP artefaktu i wypakuj `TokyoWorldLauncher.exe` w dowolne miejsce. Ten program jest samowystarczalny jako aplikacja Windows (.NET 10 self-contained single file). Dane Minecrafta, Forge, modów i gry zostaną pobrane osobno do folderu, który wybierzesz przy pierwszym uruchomieniu — jednego pliku EXE nie należy mylić z grą dostarczaną bez instalacji.

## Pierwszy start

1. Uruchom `TokyoWorldLauncher.exe`.
2. Wybierz katalog instalacji, np. `D:\Gry\TokyoWorld`. Nie wybieraj głównego folderu dysku ani Windows.
3. Repozytorium `onenightintokyo/Tokyo-World` jest obecnie **prywatne**. Aby launcher automatycznie pobierał aktualizacje, musisz podać własny **GitHub fine-grained personal access token** z dostępem `Contents: Read-only` do tego jednego repozytorium. Token nie jest umieszczony w pliku EXE i jest zapisywany na komputerze użytkownika w formie zaszyfrowanej za pomocą Windows DPAPI. Użytkownicy nieposiadający dostępu do repozytorium nie pobiorą jego prywatnych plików samym tokenem bez uprawnień; dla publicznej dystrybucji potrzebne jest osobne publiczne repozytorium z paczką.
4. Wybierz **Premium (Microsoft)** albo **Offline (nick)**.
5. Zaznacz pełny zestaw MEGA/WAVE albo tylko rdzeń technologiczny.
6. Kliknij **POBIERZ / AKTUALIZUJ** albo **GRAJ W TOKYO WORLD**.

## Co instaluje samodzielnie

- Minecraft Java Edition **1.20.1** z oficjalnych zasobów Mojang za pomocą CmlLib.Core.
- Forge **47.4.28** za pomocą CmlLib.Core.Installer.Forge (automatyczna instalacja może otworzyć stronę Forge).
- Potrzebne biblioteki, zasoby gry, dźwięki i Java (zarządza nimi CmlLib.Core).
- Pliki `config/`, `defaultconfigs/`, `kubejs/`, `resourcepacks/`, `shaderpacks/` z podanego repozytorium GitHub — porównywane po Git blob SHA-1 i nadpisywane wyłącznie dla zarządzanych plików. Przy aktualizacji kopiuje starsze wersje do `.tokyo/backups/`.
- Pierwotne **66 plików JAR** na podstawie dokładnych nazw i sum SHA-1 z `modpacks/base_mod_hashes.txt`. Pobierane tylko gdy ich dokładne wydanie jest na Modrinth i ma tę samą sumę.
- Pozostałe mody z `modpacks/tokyo_modules.json` (pełna paczka lub rdzeń) z publicznego Modrinth, przy weryfikacji Forge 1.20.1 i sum SHA-512.
- Grafiki FTB Quests z `TokyoWorld_Visuals.zip` oraz tryb modpacka FancyMenu.
- Foldery `mods/`, `saves/`, `logs/`, `screenshots/` itd.

**Ważny wyjątek dotyczący 66 modów bazowych:** nie wszystkie dokładne pliki muszą być obecnie dostępne na Modrinth. Jeśli pojawi się komunikat o brakujących oryginalnych modach, wybierz **IMPORTUJ STARĄ PACZKĘ ZIP** i wskaż własny eksport `Tokyo_World_Forge_1.20.1.zip`. Importuje wyłącznie JAR-y z katalogu `mods` bez nadpisywania istniejących; pozostałe pliki nadal aktualizuje z GitHuba. Program nie pobiera cudzych JAR-ów z nieautoryzowanych mirrorów.

## Konta Microsoft i offline

**Premium:** CmlLib.Core.Auth.Microsoft pokazuje oficjalny interfejs logowania Microsoft (WebView2). Wymaga konta Microsoft mającego uprawnienia do Minecraft Java Edition. Program nie pyta o hasło. Biblioteka uwierzytelniająca może przechowywać sesję lokalnie, żeby umożliwiać jej odświeżanie. W WebView2 musi być zainstalowany Microsoft Edge WebView2 Runtime.

**Offline:** wybierasz dowolny nick zgodny z formatem Minecraft (3–16 znaków, litery, cyfry, podkreślenie). Tryb zapewnia lokalną sesję bez autoryzacji Microsoft, jest przeznaczony do rozgrywki w pojedynkę lub na serwerach ze świadomie włączonym trybem offline. Nie daje dostępu do serwerów weryfikujących konta premium. Nie zastępuje licencji Minecraft Java Edition.

## Aktualizacje

Przy każdym kliknięciu **GRAJ** launcher wykonuje kontrolę GitHuba i brakujących modów przed uruchomieniem. Nie wymaga Git CLI, PowerShell ani Prism Launcher. Nie usuwa automatycznie obcych plików JAR lub światów. Sam EXE jest aktualizowany poprzez pobranie nowszego artefaktu GitHub Actions, a nie przez dynamiczną zmianę uruchomionego pliku EXE.

## Parametry

- Folder gry: wybrany przez użytkownika.
- RAM: domyślnie **4096 MB**, ręczna zmiana w UI.
- Światy: nie są usuwane przez synchronizator; pliki GitHub są ograniczone do katalogów zarządzanych.
- Log instalatora: widoczny w oknie.
- Brakujące mody: `.tokyo/missing-mods.txt`.
- Ustawienia: `%APPDATA%\TokyoWorldLauncher\launcher-settings.json` (token zaszyfrowany Windows DPAPI).

## Testy

Projekt ma automatyczne budowanie Windows na GitHub Actions (`.github/workflows/tokyo-launcher-standalone.yml`). Prawidłowe przejście kompilacji nie oznacza jeszcze, że przetestowano logowanie Microsoft, wszystkie moduły, konfiguracje i uruchamianie Forge na urządzeniu użytkownika. Przed migracją ważnego świata zalecane jest wypróbowanie pustej instancji w nowym folderze.

## Biblioteki

- CmlLib.Core — https://github.com/CmlLib/CmlLib.Core
- CmlLib.Core.Installer.Forge — https://github.com/CmlLib/CmlLib.Core.Installer.Forge
- CmlLib.Core.Auth.Microsoft — https://github.com/CmlLib/CmlLib.Core.Auth.Microsoft

Licencje bibliotek pozostają własnością odpowiednich autorów. Minecraft i Forge pobierane są ze źródeł swoich autorów; paczka nie dystrybuuje plików gry w EXE.
