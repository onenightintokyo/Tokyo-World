# Tokyo World v0.5 — aktualizacja testowa
**Minecraft 1.20.1 · Forge 47.4.28 · Friends · Expert Fun**

## Co zawiera ta aktualizacja

- **326 questów w 24 rozdziałach**, w tym nowe gałęzie AE2, Ender IO, Draconic Evolution i Cataclysm.
- W istniejących 20 rozdziałach połączono zadania w cztery równoległe ścieżki. Finał wymaga końcowych zadań z kilku ścieżek.
- Wyższe wymagania surowcowe na wybranych zadaniach oraz rozmaite nagrody, w tym doświadczenie, jedzenie, materiały i nagrody finałowe.
- **FancyMenu:** osobny aktywny układ `config/fancymenu/customization/tokyo_world.txt`. Stary układ zachowuje przyciski; aktualizator przenosi tło do nowego układu i zmienia etykiety w menu. Nie zmieniaj RAM.
- KubeJS: nowe receptury łączące Digital Miner z Create/Powah, urządzenia Ender IO z Mekanism/Create oraz Draconic Fusion Core z AE2/Mekanism.
- Walidacja składni `cucumber-tags.json` przed startem gry.
- Nowe **dobrowolne** profile modów `Lab` i `World` (patrz poniżej).

**Uwaga:** dodane pliki nie zostały jeszcze przetestowane w grze. Sprawdzono ich strukturę i zależności questów. Jeżeli loader odrzuci któryś plik, wyślij `latest.log`. Oryginalne światy nie są modyfikowane przez aktualizator.

## Instalacja poprawki (menu i questy)

1. Zamknij Minecraft i wykonaj backup istniejącego świata.
2. W PowerShell uruchom: `git -C "C:\Tokyo-World" pull --ff-only origin main`.
3. Uruchom Tokyo World przez Prism Launcher.
4. W logu powinno się pojawić `[Tokyo World] FancyMenu independent title layout installed: True` oraz `[Tokyo World] FancyMenu background PNG available: True`.
5. Otwórz księgę questów. Powinna zawierać **24 rozdziały i 326 questów**.
6. Jeśli tło nadal jest klasyczne, wejdź do **Customization → Current Screen Customizations → Enabled** (tylko gdy jest Disabled), następnie **Layouts** i sprawdź, czy **tokyo_world** jest włączony. Nie edytuj starego `title_screen_layout`, jeśli testujesz nowy. Wyślij zrzut ekranu z listy układów.

## Kolejne mody — tylko po backupie

Użytkownik potwierdził ~99 aktywnych modów; instalator nie próbuje ich usuwać ani aktualizować.

**INSTALUJ_TECH_LAB.cmd** (3 nowe dodatki):
- Applied Mekanistics (chemiczne komórki AE2, integracja z Mekanism)
- RFTools Utility (automatyczny crafting, spawner i teleportacja)
- RFTools Power (generatory i magazynowanie FE)

**INSTALUJ_DUNGEONY.cmd** (2 mody + wymagane biblioteki):
- YUNG's Better Dungeons
- YUNG's Better Strongholds

Mody YUNG zmieniają generowanie struktur w **nowych chunkach**; istniejące struktury w odkrytym terenie nie zmienią się automatycznie. Nie instaluj profili na jedynej kopii ważnego świata.

Oba pliki są w głównym folderze `C:\Tokyo-World`. **Nie wykonują się automatycznie przy Play**. Instalator pobiera tylko wydania oznaczone jako Forge 1.20.1, weryfikuje sumę SHA-512 pobieranych JAR-ów i zatrzymuje się w razie błędu. Nie potwierdza to zgodności wszystkich modów ze sobą.

## Co wykazał log z 15 listopada 2026

- Działa uruchamianie świata, FTB Quests wczytuje **278 wcześniejszych zadań**.
- FancyMenu 3.10.0 ładuje się, ale wcześniej nie pokazywał grafiki Tokyo World.
- Cucumber zgłasza błędny JSON z `cucumber-tags.json`, w oryginalnym eksporcie ten plik był uszkodzony.
- NuclearCraft zgłasza brakujące opcjonalne tagi płynów i problemy z ustawieniami rud.
- Ender IO ma brakujące tekstury części conduitów, a niektóre recepcje Cataclysm/AE2 raportują ostrzeżenia.

Te problemy są nadal do **potwierdzenia lub rozwiązania na podstawie nowego logu**. Same poprawki aktualizatora nie potwierdzają, że problem Cucumber zniknął podczas wczytywania gry.

## Kolejne testy

- Czy nowe menu ma tło i napisy Tokyo World?
- Czy loader FTB zgłasza `Loaded 2 chapter groups, 24 chapters, 326 quests`?
- Czy nowe receptury KubeJS są widoczne w JEI?
- Czy brakujące tagi NuclearCraft wciąż występują?
- Dopiero potem instalacja Tech Lab i nowa kontrola logu.

