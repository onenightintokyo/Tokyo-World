# Tokyo World — naprawa Delightful / End's Delight (Launcher v1.3)

## Powód błędu

W `latest.log` z 15 listopada 2026 Forge zgłasza:

```text
Unsupported installed optional dependencies:
Mod ID: 'ends_delight', Requested by: 'delightful',
Expected range: '[2.5,)', Actual version: '2.4'
```

To konflikt wersji: `Delightful-1.20.1-3.8.1.jar` wymaga **End's Delight co najmniej 2.5**, lecz launcher wcześniej zainstalował `ends_delight-1.20.1-2.4.jar` (wybierając ostatnie stabilne wydanie zamiast późniejszej wersji beta).

Poprawny oficjalny JAR:
- [End's Delight 2.6+forge.1.20.1](https://modrinth.com/mod/ends-delight/version/txuQ2GPU) — Modrinth Version ID `txuQ2GPU`, Forge 1.20.1;
- W manifeście `modpacks/tokyo_modules.json` `ends-delight` ma teraz przypięty ten `versionId`.

## Automatyczna naprawa w Launcherze v1.3

Po kliknięciu `GRAJ`, po synchronizacji i uzupełnieniu modów launcher:
1. Sprawdza wszystkie nazwy JAR `ends_delight-*.jar` w wybranym folderze `mods`.
2. Jeśli jest tylko wersja starsza niż 2.5, pobiera **2.6 Forge 1.20.1 z oficjalnego Modrinth CDN** i sprawdza SHA-512.
3. Dopiero po zweryfikowaniu nowej wersji przenosi niezgodne pliki do `.tokyo/backups/mod-conflicts/...`.
4. Aktywuje nową wersję. Przy błędzie podmiany przywraca stare pliki.
5. Jeśli istnieje już zgodna wersja, zostawia ją i chowa ewentualne starsze duplikaty.

W logu widać też dwa pliki Integrated Terminals: `IntegratedTerminals-1.20.1-1.7.0.jar` oraz `IntegratedTerminals-1.20.1-1.7.1.jar`. Launcher automatycznie przenosi starszą wersję do kopii i pozostawia 1.7.1.

**Uwaga:** poprawka w kodzie nie aktualizuje samoczynnie wcześniej skompilowanego EXE. Należy pobrać i uruchomić nowy EXE v1.3 z Windows GitHub Actions; samo podmienienie manifestu nie wystarczy.

## Szybka ręczna naprawa bez nowego EXE

1. Zamknij Minecraft.
2. Otwórz `%LOCALAPPDATA%\TokyoWorld\mods` (chyba że w launcherze wybrano inną lokalizację).
3. Zrób kopię problematycznych JAR-ów do osobnego folderu **poza katalogiem mods**:
   - `ends_delight-1.20.1-2.4.jar`
   - `IntegratedTerminals-1.20.1-1.7.0.jar`
4. Pobierz oficjalną wersję [End's Delight 2.6 dla Forge 1.20.1](https://modrinth.com/mod/ends-delight/version/txuQ2GPU).
5. Skopiuj JAR `ends_delight-2.6+forge.1.20.1.jar` do folderu `mods`, upewniając się, że stara wersja 2.4 nie została w `mods`.
6. Pozostaw `IntegratedTerminals-1.20.1-1.7.1.jar` i przetestuj start.

**Nie usuwaj** światów, questów ani innych modów.

## Testy

Zgodność wersji End's Delight 2.6 / Forge 1.20.1 potwierdzona stroną Modrinth. Kod naprawczy sprawdza projekt, loader, SHA-512, tworzy backup i wykonuje rollback. Kompilacja Windows nie zastępuje testu gry u użytkownika; w razie kolejnego błędu wyślij nowy `latest.log`.
