# Tokyo World — Technology Expansion v0.4 (TEST)

Minecraft **1.20.1**, Forge **47.4.28**. Wszystkie mody z dodatkowego etapu są dobierane tylko spośród wersji Forge 1.20.1. Zanim zaczniesz, **zrób kopię zapasową świata** i zamknij Minecrafta.

## Co jest już na GitHubie?

- Questbook: **278 zadań w 20 rozdziałach**, rozwinięty o technologię, magię, rolnictwo, logistykę, eksplorację i kosmos.
- KubeJS: kilka rzeczywiście zmienionych receptur łączących mody; receptura infuzora Mekanism jest w osobnym wcześniejszym skrypcie.
- FancyMenu: naprawa uszkodzonego zapisu tła, ukrycie logo Minecrafta na rzecz logo umieszczonego w grafice Tokyo World, rezerwowe globalne tło.
- Aktualizator: naprawa błędnego pola `resourcePacks` w `options.txt` (bez zmieniania RAM), automatyczne kopiowanie PNG z `TokyoWorld_Visuals.zip` do `kubejs/assets`.
- Mekanism: Digital Miner 1000 → 2000 FE/t (bazowo), 80 → 60 ticków na blok; Crusher 50 → 65 FE/t; wygodniejsze filtry Digital Miner.

## Krok 1 — sprawdź aktualizację graficzną i questy

1. Zamknij Minecraft.
2. Uruchom Tokyo World przez Prism Launcher (włączony pre-launch updater).
3. Konsola powinna pokazać `[Tokyo World] Quest GUI images ready` oraz `Done. Files copied`.
4. W menu głównym powinno być tło z napisem Tokyo World. W FTB Quests powinno być 20 rozdziałów.
5. Po aktualizacji uruchom Minecraft ponownie, jeśli pierwsze uruchomienie używało starego skryptu sprzed `git pull`.

## Instalacja jednym dwuklikiem (wygodniejsza niż PowerShell)

Po tym, jak Prism pobierze najnowsze zmiany z GitHuba, zamknij Minecraft i otwórz folder `C:\Tokyo-World`. Znajdziesz w nim:

- `INSTALUJ_TECH_CORE.cmd` — podstawowe technologie
- `INSTALUJ_TECH_EXPANDED.cmd` — dodatkowe technologie i reaktory (zawiera Core)
- `INSTALUJ_BOSSOWIE.cmd` — Cataclysm

Pliki CMD tylko uruchamiają opisany poniżej instalator PowerShell, który pobiera zweryfikowane mody. **Zawsze rób kopię świata przed zmianą zestawu modów.** Jeżeli instalator zakończy się błędem, nie usuwaj już obecnych modów — przekaż komunikat z okna.

## Krok 2 — nowe mody techniczne

**Nie są jeszcze instalowane automatycznie przy Play.** Instalator jest **opcjonalny**, pobiera mody z Modrinth i deklarowane wymagane zależności, filtruje do Forge 1.20.1, weryfikuje SHA-512 i nie nadpisuje istniejących modów. Domyślnie wypisuje tylko plan. Nie uruchamiaj profili rozszerzających na jedynym egzemplarzu ważnego świata bez kopii zapasowej.

Po pierwszym uruchomieniu gry (żeby skrypt został pobrany do C:\\Tokyo-World):

**Zobacz plan bez instalacji:**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Tokyo-World\tools\Install-TokyoModules.ps1" -Profile Core
```

**Zainstaluj Tech Core:**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Tokyo-World\tools\Install-TokyoModules.ps1" -Profile Core -Apply
```

**Dopiero po udanym teście uruchom rozszerzenie:**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Tokyo-World\tools\Install-TokyoModules.ps1" -Profile Expanded -Apply
```

**Opcjonalnie bossowie:**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Tokyo-World\tools\Install-TokyoModules.ps1" -Profile Adventure -Apply
```

Jeśli repozytorium znajduje się w innym miejscu, zamień `C:\Tokyo-World` na faktyczną lokalizację. Jeśli podczas pobierania wybrana wersja nie jest dostępna, instalator **przerwie** działanie; nie wyłącza zabezpieczeń ani nie instaluje błędnej wersji.

### Profile

**Core**: Applied Energistics 2, Ender IO, Mekanism Generators/Tools/Additions, Thermal Expansion, PneumaticCraft. 

**Expanded**: Core + Integrated Crafting/Terminals, RFTools Builder, Extreme Reactors, NuclearCraft Neoteric, Draconic Evolution, Create: Steam 'n' Rails (wersja pod Create 6).

**Adventure**: L_Ender's Cataclysm.

Mody *nie są jeszcze sprawdzone w uruchomionej instancji z całą zawartością*, dlatego dodatkowe pliki przygotowano jako instalację na żądanie, a nie wymuszony update. Pozostałe planowane mody (m.in. dodatkowe struktury, wymiar Twilight Forest, AE2 addony) dodamy stopniowo po testach zależności.

## Raportowanie błędów

Jeżeli gra nie startuje lub zniknęły grafiki questów, prześlij `minecraft/logs/latest.log` i zrzut ekranu konsoli Prism. Nie usuwaj save'ów. Pliki synchronizowane przez aktualizator mają kopie w `minecraft/.tokyo-world-backups`.
