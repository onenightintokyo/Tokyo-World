# Tokyo World v0.9 — Launcher + WAVE 36

## 1. Tokyo World Launcher (Windows 11)

**Start:** `C:\Tokyo-World\TOKYO_LAUNCHER.cmd`

Jest to własny graficzny interfejs Windows (WinForms + PowerShell), a nie samodzielny silnik Minecraft. Korzysta z **Prism Launcher 11.1.1**, tego samego Minecrafta, tych samych światów, konfiguracji i komend synchronizacji GitHub.

Funkcje:
- **Premium (Microsoft):** wyświetla lokalne nazwy kont Microsoft z pliku `accounts.json` Prism i uruchamia `--launch Tokyo_World_Forge_1.20.1 --profile NICK`.
- **KONTA MICROSOFT:** otwiera oficjalny Prism Launcher, gdzie wybierasz *Konta → Zarządzaj kontami → Dodaj Microsoft*. Logujesz się w oficjalnej przeglądarce Microsoft. Tokyo World Launcher **nie pobiera i nie zapisuje haseł ani tokenów**.
- **Offline (Nick):** wpisujesz nick 3–16 znaków; program uruchamia Prism z `--launch Tokyo_World_Forge_1.20.1 --offline NICK`.
- **AKTUALIZUJ:** uruchamia dotychczasowy `tools/Update-TokyoWorld.ps1`.
- **NOWE MODY:** otwiera instalator profilu Wave.
- **FOLDER GRY:** otwiera katalog instancji Minecrafta.
- Zapamiętuje tylko tryb, nazwę konta, nick offline i lokalizację `prismlauncher.exe` w `%LOCALAPPDATA%\TokyoWorldLauncher\settings.json`.

**Ważne ograniczenie trybu offline:** to oficjalny tryb offline Prism, a nie obejście weryfikacji własności Minecrafta. Standardowy Prism może wymagać, by legalne konto Microsoft zostało wcześniej dodane. W trybie offline nie dołączysz do serwerów weryfikujących konta premium (*online-mode=true*). Jeżeli potrzebujesz samodzielnego launchera niezależnego od Prism, trzeba osobno zaimplementować i testować instalację gry, Forge, uwierzytelnianie Microsoft OAuth, token storage i aktualizację bibliotek.

Uruchomione gry dalej korzystają z 4 GB pamięci JVM zgodnie z Twoimi ustawieniami Prism — launcher sam nie zmienia pamięci RAM.

### Gdy nie wykrywa Prism Launcher

Na pierwszym starcie program automatycznie szuka pliku `prismlauncher.exe` w typowych lokalizacjach Windows. Jeżeli się nie uda, pojawi się okno wyboru pliku. Lokalizację zapisze jedynie lokalnie.

### Zmiana konta premium

Dodaj konto przez przycisk `KONTA MICROSOFT` i odśwież listę. Możesz również wpisać profil ręcznie; musi on odpowiadać profilowi konta skonfigurowanego w Prism.

## 2. WAVE — 36 kandydatów na nowe mody

Jedno kliknięcie w `C:\Tokyo-World\INSTALUJ_TOKYO_WAVE.cmd` albo w przycisk `NOWE MODY` z Tokyo World Launcher.

| Grupa | Liczba | Przykłady |
|---|---:|---|
| Industry | 11 | Storage Drawers, Create Deco, Create Connected, Copycats+, Slice & Dice, Immersive Aircraft, Tom's Storage, Iron Chests |
| Bosses | 8 | Alex's Mobs, Mowzie's Mobs, Born in Chaos, Mutant Monsters, Better Combat |
| World | 8 | Biomes O' Plenty, Regions Unexplored, Lootr, Waystones, Nature's Compass, Explorer's Compass |
| Quality | 9 | AppleSkin, Controlling, Mouse Tweaks, FTB Chunks, FTB Ultimine, Polymorph |

- Wszystkie 36 są **kandydatami do instalacji**: Modrinth w czasie wykonywania skryptu wybierze tylko zgodne wydania Minecraft 1.20.1 / Forge.
- Brak wydania, niedostępny projekt, zależności i sumy SHA-512 są obsługiwane przez wspólny instalator.
- Instalator pomija już zainstalowane mody; nie nadpisuje istniejących plików ani zapisów świata.
- Addony Create mogą być oznaczone Forge 1.20.1, ale nadal **nie muszą** być kompatybilne z Create 6.0.8. To sprawdzimy na kopii instancji.
- Profile `Wave` i `Mega` korzystają z tej samej deklaracji; `Wave` sprawdza tylko 36 nowych propozycji, nie wszystkie poprzednie moduły.
- Wymagania pamięciowe kolejnych modów mogą przekroczyć przydzielone obecnie 4 GB — nie zmieniamy go samoczynnie.

## 3. Instalacja

1. Zamknij grę i wykonaj kopię zapasową instancji oraz świata.
2. Pobierz z GitHub:
   `git -C "C:\Tokyo-World" pull --ff-only origin main`
3. Uruchom `C:\Tokyo-World\TOKYO_LAUNCHER.cmd`.
4. Wybierz Premium i dodaj konto Microsoft przez oficjalny Prism albo wybierz Offline i wpisz nick.
5. Kliknij `GRAJ W TOKYO WORLD`.
6. Zainstaluj WAVE osobno, jeśli chcesz kolejne dodatki; po instalacji sprawdź najnowszy `logs/latest.log`.

## 4. Stan testów

Zapisano kod i wykonano kontrolę statyczną. Windows PowerShell 5.1 oraz Prism GUI **nie były dostępne do uruchomienia w środowisku autora**, dlatego otwieranie okna, auto-wykrywanie Prism i start gry muszą zostać potwierdzone na komputerze użytkownika. W razie błędu prześlij pełny komunikat z konsoli.
