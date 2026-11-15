# Tokyo World Launcher v1.4 — logo TW w programie i grze

## Co zostało wdrożone

Zamiast samej grafiki, projekt **TokyoWorldLauncher.exe** jest teraz kompilowany z osadzoną ikoną TW w stylu neonowego Tokio (metaliczne litery TW, torii, różowy blask, motyw technologii Minecraft).

1. **Plik EXE:** skompilowany zasób Windows `.ico` (16, 24, 32, 48, 64, 128 i 256 px), widoczny w Eksploratorze, skrócie i pasku zadań.
2. **Okno Tokyo World Launcher:** TW jest używane w pasku tytułu i jako małe logo w lewym górnym rogu grafiki launchera.
3. **Okno Minecraft / Forge:** program po uruchomieniu procesu Java wykrywa jego okno GLFW i wysyła systemowe komunikaty `WM_SETICON` dla małej oraz dużej ikony. Odświeża je co kilka sekund, gdyby Minecraft ustawił ponownie domyślną ikonę. **Program musi pozostać uruchomiony w tle gry, by mechanizm mógł ją ponawiać.**
4. **Zasoby gry:** przy synchronizacji program zapisuje `config/fancymenu/assets/tokyo_world_tw_logo.png` i `kubejs/assets/tokyoworld/textures/gui/tw_logo.png`. To zasoby do przyszłej aranżacji menu; sama obecność pliku nie dodaje jeszcze nowego widżetu logo FancyMenu.
5. **Brak zmian w modach/świecie:** zmiana nie modyfikuje JAR-ów Minecrafta, plików `saves`, ustawień RAM, questów ani identyfikatorów użytkowników.

## Pobieranie

**[Tokyo World v1.4 — kompilacja GitHub Actions](https://github.com/onenightintokyo/Tokyo-World/actions/runs/43764424900)**

Artefakt: `TokyoWorld-Standalone-Windows-x64`, zawiera `TokyoWorldLauncher.exe`.

1. Zamknij Minecrafta i poprzedni launcher.
2. Pobierz EXE v1.4, zastąp poprzedni EXE.
3. Uruchom nowy launcher. Sprawdź nowe logo na pasku tytułu, na ikonie pliku i pasku zadań.
4. Kliknij `GRAJ`. Gdy otworzy się okno Minecrafta, launcher powinien nadać mu własną ikonę TW.
5. Pozostaw launcher uruchomiony w tle. Jeżeli Minecraft nadal pokazuje domyślną ikonę, kliknij tekst statusu w launcherze, aby obejrzeć dziennik oraz prześlij zrzut paska zadań.

## Pliki projektu

- `launcher-standalone/Assets/TokyoWorldIcon.png.b64` — zakodowane źródło grafiki TW
- `launcher-standalone/Assets/PrepareIcons.ps1` — generator PNG oraz ICO dla Windows
- `launcher-standalone/TokyoWorldLauncher.csproj` — `ApplicationIcon` i osadzone zasoby
- `launcher-standalone/TokyoBranding.cs` — odczyt logo i ustawienie ikony GLFW przez Win32
- `launcher-standalone/MinecraftEngine.cs` — uruchomienie mechanizmu brandingu po startcie gry
- `launcher-standalone/Program.cs` — ikona okna oraz logo w interfejsie
- `.github/workflows/tokyo-launcher-standalone.yml` — build EXE z wygenerowaną ikoną

## Weryfikacja

Windows GitHub Actions pomyślnie skompilował projekt: run `43764424900`. Pobrany EXE jest poprawnym plikiem PE32+ x64 i zawiera w sekcji zasobów Windows grupę siedmiu ikon (PNG), a nie domyślną ikonę aplikacji.

**Granica testu:** nie uruchomiono Minecrafta na docelowym komputerze użytkownika. Win32 `WM_SETICON` powinno zmienić ikonę okna i paska zadań, ale działanie może zależeć od tego, kiedy GLFW tworzy lub odświeża okno, i wymaga sprawdzenia w grze. Nie zmienia się tu systemowo ikona launchera Mojang ani domyślne skojarzenia plików `.jar`.
