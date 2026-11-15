# TOKYO WORLD MEGA — zbiorcza rozbudowa v0.8

## Cel

Jedna instalacja zamiast kilku małych etapów. Modpack pozostaje na **Minecraft 1.20.1 / Forge 47.4.28**. Nowa lista zawiera **66 kandydatów na dodatkowe mody**, z przewagą technologii, energetyki, automatyzacji i cyfrowych magazynów.

To jest **zestaw kandydatów do sprawdzenia**, a nie potwierdzenie, że wszystkie 66 modów można zainstalować na aktualnym kliencie. Występują mody dostępne tylko dla innych loaderów, z wydaniami beta lub niezgodnymi z Create 6.0.8. Instalator je pomija, podaje powód i zapisuje raport.

## Co dodano

### Technologia i automatyzacja (27)

CC: Tweaked, Refined Storage, MEGA Cells, AdvancedAE, Super Factory Manager, Iron Furnaces, Iron Jetpacks, Integrated Tunnels, RFTools Storage, RFTools Control, Botany Pots, Botany Trees, Compact Machines, Entangled, Building Gadgets, Mining Gadgets, Mob Grinding Utils, Flux Networks, LaserIO, Applied Botanics, Ars Énergistique, ExtendedAE Plus, AE2 Wireless Terminals Library, Create Crafts & Additions, Create: Handcrafted, Create Ore Excavation, Create: New Age.

### Magia (8)

Occultism, Blood Magic, Iron's Spells 'n Spellbooks, Forbidden & Arcanus, Hexerei, Apotheosis, Artifacts, Reliquary.

### Eksploracja, wymiary i bossowie (18)

The Aether, Blue Skies, Deeper and Darker, The Undergarden, Twilight Forest, When Dungeons Arise, Dungeons Plus, Dungeon Crawl, Repurposed Structures, Valhelsia Structures, YUNG's Better Mineshafts / Nether Fortresses / Ocean Monuments / Desert Temples / Jungle Temples / Witch Huts / Bridges / Extras.

### Budowanie i rolnictwo (13)

Chipped, Rechiseled, FramedBlocks, Handcrafted, Decorative Blocks, Macaw's Bridges / Furniture / Doors / Roofs / Windows, Farmer's Respite, Cooking for Blockheads, Delightful.

## Jedna instalacja MEGA

1. **Zamknij Minecrafta.** Dla pewności zduplikuj instancję Tokyo World w Prism Launcher (i zrób kopię zapasową swojego świata). To ważne przy jednoczesnym dodawaniu wielu modów.
2. Otwórz PowerShell i pobierz najnowszą wersję:

```powershell
git -C "C:\Tokyo-World" pull --ff-only origin main
```

3. Uruchom przez dwuklik w folderze `C:\Tokyo-World` plik **`INSTALUJ_TOKYO_MEGA.cmd`**.
4. Skrypt **najpierw pokazuje plan** brakujących modów i wymaganych zależności. Jeżeli pasuje, potwierdź `T` — skrypt pobierze i sprawdzi JAR-y.
5. Przed uruchomieniem oryginalnego świata sprawdź wynik w Prism Launcher na kopii instancji.

## Zabezpieczenia

- Nie usuwa istniejących modów i nie nadpisuje istniejących plików `.jar` ani światów.
- Przed pobraniem każdego nowego wydania wymaga **Forge i Minecraft 1.20.1**.
- Każdy nowy JAR musi posiadać prawidłową sumę SHA-512 Modrinth.
- Weryfikuje wymagane zależności udostępnione przez Modrinth.
- Gdy projekt nie ma zgodnego wydania albo strona Modrinth nie działa, **MEGA pomija go z raportem** `.tokyo-world-mega-skipped.txt` w folderze Minecrafta, zamiast zatrzymywać całą listę.
- Pobieranie ma do 3 prób na plik; pobrane i zweryfikowane JAR-y przechowuje w `.tokyo-world-mod-staging` na wypadek ponowienia po błędzie. Staging jest czyszczony po udanej instalacji.
- Gdy pobranie jednego niezbędnego pliku trwale się nie powiedzie, proces instalacji wycofuje dodane pliki; nie powinien pozostawić połowicznie zmienionego katalogu `mods`.
- **Zgodność modów między sobą, szczególnie addonów Create, nie jest gwarantowana** wyłącznie przez sprawdzenie loadera i wersji gry.

## Uwaga o działaniu gry

Masz już około 99 modów. 66 dodatkowych kandydatów i ich zależności to duża zmiana. Nie zmieniamy RAM w konfiguracji, ale wymagania pamięciowe i obciążenie procesora mogą zauważalnie wzrosnąć. Jeśli nowe mody okażą się zbyt ciężkie, testuj na kopii instancji i usuń wybrane dodatki przed grą na głównym świecie.

Zmiany w generowaniu struktur będą widoczne głównie w nowo odkrywanych chunkach.

## Weryfikacja

Po zakończeniu instalacji wyślij wynik z okna MEGA i log `logs/latest.log` po uruchomieniu. Na tej podstawie uzgodnimy dokładny zestaw, zrobimy kolejne questy tylko dla rzeczywiście zainstalowanych modów i dopasujemy ich konfigurację do Expert Fun.

## Repozytorium

- `modpacks/tokyo_modules.json` — deklaracje 66 modów
- `tools/Install-TokyoModules.ps1` — planowanie, pobieranie, weryfikacja, pomijanie brakujących i rollback
- `INSTALUJ_TOKYO_MEGA.cmd` — instalacja jednym uruchomieniem, z podglądem
