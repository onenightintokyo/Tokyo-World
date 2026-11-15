# Tokyo World

**Minecraft 1.20.1 / Forge 47.4.28 — Tech • Automation • Adventure • Farming**

Autorska paczka technologiczna dla znajomych, rozwijana etapami. Mody techniczne są rdzeniem rozgrywki, a pozostałe systemy uzupełniają eksplorację, budowanie i rozgrywkę For Fun.

## Status — Questbook v0.1 (test)

- 4 rozdziały: Początek, Create, Mekanism, Farming
- 30 questów (zależności, nagrody, tłumaczenia PL/EN)
- Grafiki rozdziałów z istniejącego pakietu zasobów `TokyoWorld_Visuals.zip`
- Automatyczna synchronizacja plików przy uruchomieniu przez `tools/Update-TokyoWorld.ps1`
- Aktualizator aktywuje istniejące tło FancyMenu i paczkę zasobów, o ile pliki są obecne w instancji

**To jeszcze nie jest pełny, przetestowany modpack.** Questbook czeka na pierwszy test w grze. Skrypt obecnie synchronizuje pliki projektowe, **nie pobiera automatycznie nowych modów .jar**.

## Test

1. Zrób kopię zapasową świata i zamknij Minecraft.
2. Uruchom instancję w Prism Launcher. W konsoli sprawdź `[Tokyo World] Done. Files copied:`.
3. Wejdź do świata i otwórz FTB Quests. Powinny być 4 rozdziały i 30 zadań.
4. Jeśli coś się nie wczyta, przekaż `logs/latest.log`.

## Dokumentacja

- [Questbook](docs/QUESTBOOK_v0.1.md)
- [Automatyczne aktualizacje](docs/AUTOMATYCZNE_AKTUALIZACJE.md)

## Następne kroki

Weryfikacja questbooka w Minecraft, rozdziały Thermal / Immersive Engineering / Industrial Foregoing, kompletne przeróbki receptur, osobne rozdziały eksploracji i magii, a następnie pełny manifest Packwiz do automatycznych aktualizacji modów.
