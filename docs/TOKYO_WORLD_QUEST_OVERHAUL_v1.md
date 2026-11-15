# Tokyo World — Nowa progresja fabularna

Minecraft **1.20.1**, Forge **47.4.28** — aktualizacja questbooka oraz nowa grupa modów.

## Księga questów: 518 zadań, 36 rozdziałów i 8 kategorii

Przebudowano opisy, kolejność, nazwy rozdziałów i połączenia między zadaniami. Nie resetowano ich identyfikatorów, nagród ani istniejących zadań przedmiotowych.

### Nowy podział

- **Pierwszy ślad** — przetrwanie, pierwszy warsztat i podstawy uprawy
- **Mechanizmy** — Create, rozbudowa fabryki, programowanie urządzeń
- **Ogień i stal** — Mekanism, metalurgia, Ender IO, chemia
- **Energia i sieci** — Powah, magazyny, AE2, automatyczna logistyka
- **Żywa fabryka** — farming, uprawy magiczne, pszczoły i zaplecze bazy
- **Sekrety magii** — Botania, Ars Nouveau, technomagia
- **Poza horyzont** — Ad Astra, Alex's Caves, bossowie i dalekie wyprawy
- **Dziedzictwo Tokyo** — Draconic Evolution i projekt finałowy

### Jak działa drzewko

Zamiast numerowanej listy typu „etap 1/16, etap 2/16” każde drzewko prowadzi przez:

**Wprowadzenie → dwie drogi rozwoju → połączenie → kolejny wybór → wspólna próba → finał.**

Na przykład w rozdziale *Hala Tokyo* najpierw przygotowujesz fundament, następnie rozwijasz niezależne odnogi mechaniki i transportu. Obie gałęzie muszą być ukończone, zanim przejdziesz do następnego odcinka. Do ostatecznego celu nie można przeskoczyć pomijając podstawy.

Zmienił się styl narracji. Zamiast automatycznie generowanych komunikatów o kolejnym etapie zadania opisują rzeczywisty problem, który pojawia się podczas budowania, i sugerują, co warto sprawdzić w świecie.

**36 nowych ręcznych prób praktycznych** w 12 zaawansowanych rozdziałach: w punktach, gdzie gałęzie się łączą, oraz w finałach gracz ma po wykonaniu zadań przedmiotowych przetestować swój układ albo przygotowania do wyprawy, a następnie zaznaczyć `checkmark` w FTB Quests. Nie usunięto wcześniejszych wymagań materiałowych.

### Co zachowano

- Wszystkie 518 identyfikatorów zadań, na których opiera się zapis postępu
- ID wcześniej istniejących zadań przedmiotowych
- Nagrody oraz dotychczasowe ilości wymaganych przedmiotów
- Połączenia między rozdziałami prowadzące do ważnych projektów
- Grafiki i ikony questów

**Uwaga:** Zmiana układu i dołożenie ręcznych prób może wymagać ponownego zatwierdzenia punktów kontrolnych. Nie ma gwarancji zachowania każdego szczegółu postępu po zmianach bez testu na kopii świata.

## Kolejna grupa modów: Chapter Expansion (15 propozycji)

Technologia i magia:
- Tinkers' Construct
- Mantle (biblioteka)
- Rats
- EvilCraft
- Nature's Aura
- Productive Trees

Świat, farming i znajomi:
- The Bumblezone
- Dynamic Trees
- Serene Seasons
- Simple Voice Chat
- Every Compat
- Immersive Paintings
- End's Delight
- Ender's Delight
- Villager Quests

Projekty dobrano z myślą o **Forge 1.20.1**, ale nie ma jeszcze potwierdzenia ich wspólnego działania na pełnej instancji Tokyo World. W szczególności długi zestaw modów i konfiguracje generowania świata mogą powodować konflikty lub wyższe zużycie RAM.

### Instalacja

**Jeśli używasz dotychczasowego folderu C:\Tokyo-World:**
1. Zamknij grę, zrób backup świata.
2. W PowerShell wykonaj `git -C "C:\Tokyo-World" pull --ff-only origin main`.
3. Uruchom `INSTALUJ_ROZDZIALY.cmd`, zobacz listę modów i potwierdź `T`.
4. Sprawdź uruchomienie Forge i wczytanie 8 kategorii w FTB Quests.

**Jeśli korzystasz z niezależnego EXE:**
- Przy kolejnym uruchomieniu aktualizator pobierze nowy questbook.
- Profil `InstallAllModules` w ustawieniach dodaje również nowe propozycje z manifestu Modrinth.
- Samo zapisanie manifestu na GitHubie **nie oznacza**, że nowe JAR-y są już zainstalowane; zawsze sprawdź wynik pobierania.

### Testowanie

Prawidłowe przetworzenie plików SNBT zostało sprawdzone skryptem (518 unikalnych identyfikatorów; każda zależność prowadzi do istniejącego questa; brak cykli). **Nie jest to jeszcze test klienta Minecraft.** W razie błędu podeślij `logs/latest.log` i zrzut drzewa.

Źródła projektu:
- `tools/questbook-storyline.mjs` — odtwarzalny generator i walidator
- `config/ftbquests/quests/chapters/` — 36 gotowych rozdziałów
- `config/ftbquests/quests/chapter_groups.snbt` — 8 nowych kategorii
- `modpacks/tokyo_modules.json` — deklaracja nowej serii modów
