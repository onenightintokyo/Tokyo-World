# Tokyo World — Expert Fun Modpack

## Tokyo World — narracyjna progresja i kolejna seria modów

**Nowy questbook:** 518 questów, 36 rozdziałów, **8 fabularnych kategorii**. Poprawiono opisy, połączono zadania w przechodzące przez rozgałęzienia drzewka i dodano 36 prób praktycznych w zaawansowanych projektach. Zachowano identyfikatory i dawne nagrody.

**Chapter Expansion:** 15 nowych propozycji Forge 1.20.1 (Tinkers' Construct, Rats, EvilCraft, Nature's Aura, Productive Trees, Bumblezone i inne). Mody są w manifeście; instalacja przez `INSTALUJ_ROZDZIALY.cmd` albo aktualizator niezależnego launchera z włączonym pełnym zestawem. Backup świata i test zgodności nadal wymagane.

[Opis nowych drzewek, kategorii oraz instrukcja instalacji](docs/TOKYO_WORLD_QUEST_OVERHAUL_v1.md)


## Tokyo World Launcher v1.2 — automatyczna naprawa bazowych modów

**Poprawiony EXE:** [GitHub Actions — działająca kompilacja v1.2](https://github.com/onenightintokyo/Tokyo-World/actions/runs/43756562340).

Jeżeli exact-mod installer nie znajduje części 66 bazowych JAR-ów na Modrinth, launcher:
1. automatycznie sprawdza własny folder mods i istniejącą lokalną instancję Minecrafta;
2. automatycznie wykrywa ZIP oryginalnego Tokyo World w Pobrane/Pulpit/Dokumenty;
3. w razie potrzeby pokazuje okno wyboru ZIP;
4. importuje wyłącznie pliki o zgodnych sumach SHA-1, bez nadpisywania światów i innych modów.

Launcher nadal uruchamia Minecraft i Forge samodzielnie, bez Prism. Stara instancja może być jedynie **jednorazowym źródłem importu**. Zmiana nie wymaga usuwania już pobranych plików ani ponownej instalacji od zera.

[Instrukcja naprawy błędu brakujących modów](docs/TOKYO_WORLD_BASE_MOD_RECOVERY_v1.2.md)


## Tokyo World Launcher v1.1 — uproszczony wygląd

**Gotowy Windows EXE:** [Pobierz z udanej kompilacji GitHub Actions](https://github.com/onenightintokyo/Tokyo-World/actions/runs/43755226737).

- Ekran w klimacie Tokyo World (Tokio, czerwone słońce, torii i neony).
- **Nick, RAM (2–24 GB), tryb Microsoft/Offline i jeden przycisk GRAJ.**
- Po autoryzacji Microsoft launcher aktualizuje nick na nazwę zalogowanego profilu Minecraft.
- Token GitHub, katalog instalacji i konfiguracja modułów są ukryte pod ikoną ⚙.
- **Nie osadzamy prywatnego tokenu w EXE.** Dodano mechanizm publikowania do osobnego publicznego repozytorium klienta, aby znajomi mogli pobierać aktualizacje bez kluczy (wymaga jednorazowego skonfigurowania przez właściciela).
- [Instrukcja nowego launchera i dystrybucji dla znajomych](docs/TOKYO_WORLD_SIMPLE_LAUNCHER_v1.1.md).

**Status:** wersja 1.1 pomyślnie skompilowana na Windows, lecz wymaga testu logowania i instalacji modów w docelowym systemie.


## NOWY: Tokyo World Launcher 1.0 (bez Prism)

**Samodzielny, skompilowany EXE dla Windows x64:** `TokyoWorldLauncher.exe` (GitHub Actions build udany).

- [Instrukcja instalacji, kont i aktualizacji](docs/TOKYO_WORLD_STANDALONE_LAUNCHER.md)
- [Pobierz EXE z GitHub Actions — run 43721308731](https://github.com/onenightintokyo/Tokyo-World/actions/runs/43721308731)
- Kod C#: [launcher-standalone](launcher-standalone)
- Wybór folderu instalacji, automatyczna instalacja Minecraft 1.20.1 / Forge 47.4.28, Java, synchronizacja GitHub, podstawowe i opcjonalne mody, konta Microsoft / nickname offline
- Nie wymaga Prism, Python ani uruchamiania PowerShell; program sam pobiera potrzebne pliki. Ponieważ repozytorium jest prywatne, dostęp do GitHub wymaga autoryzacji użytkownika (fine-grained token Contents: Read, zaszyfrowany w Windows DPAPI)
- Hash-lock 66 bazowych modów: [modpacks/base_mod_hashes.txt](modpacks/base_mod_hashes.txt). Przy braku dokładnej wersji na Modrinth można zaimportować pierwotne ZIP.

**Status:** EXE pomyślnie skompilowano w Windows CI. Pierwsza instalacja całego modpacka, logowanie Microsoft i start Forge na komputerze użytkownika nie są jeszcze przetestowane. Nie nadpisuje zapisów świata; zalecane jest wybranie nowego pustego folderu.


**Minecraft 1.20.1 · Forge 47.4.28 · Technology · Automation · Magic · Exploration**

Modpack Tokyo World dla znajomych, z rozbudowaną progresją technologiczną, automatyką fabryk, magią, bossami i własnymi questami.

## NOWOŚĆ v0.9 — launcher Tokyo World

**`TOKYO_LAUNCHER.cmd`** — graficzny launcher Windows w czarno-czerwonym stylu.

- **Premium / Microsoft:** wybór konta z Prism Launcher; logowanie Microsoft obsługuje oficjalny Prism.
- **Offline / Nick:** dowolny poprawny nick Minecraft w oficjalnym trybie offline Prism. Tryb nie zastępuje licencji i nie wpuszcza na serwery premium.
- Przycisk graj, aktualizacja z GitHub, nowe mody i folder gry.
- Bez Pythona, haseł i kopiowania tokenów. Korzysta z istniejącej instancji Prism.

**`INSTALUJ_TOKYO_WAVE.cmd`** — 36 nowych kandydatów na mody (11 technicznych, 8 mobów/bossów, 8 eksploracyjnych i 9 usprawnień). Instalator dobiera dostępne wydania dla Forge 1.20.1, pomija już posiadane dodatki i sprawdza SHA-512. Nie gwarantuje zgodności wszystkich addonów Create 6.0.8.

[Instrukcja startu launchera i modów WAVE v0.9](docs/TOKYO_WORLD_LAUNCHER_WAVE_v0.9.md)

## Wcześniejsze aktualizacje

- [MEGA v0.8 — duża rozbudowa](docs/TOKYO_WORLD_MEGA_v0.8.md)
- [Questbook v0.7 — 518 zadań / 36 rozdziałów](docs/TOKYO_WORLD_v0.7.md)
- [UI FancyMenu i księgi questów](docs/QUESTBOOK_UI_v0.6.md)
- [Automatyczna aktualizacja](docs/AUTOMATYCZNE_AKTUALIZACJE.md)

**Status:** Kod launchera i lista modów są zapisane na GitHubie, ale nie zostały jeszcze przetestowane na komputerze użytkownika. Przed dodaniem nowych modów zrób kopię instancji i świata. RAM pozostaje według ustawień Prism.
