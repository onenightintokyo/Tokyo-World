# Tokyo World v0.7 — Industrial Expansion
**Minecraft 1.20.1 · Forge 47.4.28 · Expert Fun · Friends**

## Co jest już zapisane w GitHub

- **518 questów w 36 rozdziałach** — +192 nowe zadania i +12 rozdziałów, wszystkie w istniejących sześciu grupach.
- Nowe rozdziały: megafabryka, sterowanie fabryką, chemia, imperium stopów, sieć FE, centrum danych ME, optymalizacja logistyki, farmy autonomiczne, technomagia, baza orbitalna, kontrakty na bossów oraz wielki finał.
- Każdy nowy quest ma minimum dwa zadania przedmiotowe, rosnące wymagania, 4 równoległe ścieżki oraz nagrodę. Finał ma trzecie zadanie, a jego odblokowanie zależy od ukończenia gałęzi.
- Wielki finał **Tokyo World: Expert** jest zależny od ukończenia wszystkich 11 pozostałych nowych projektów. Odpowiedni dla gry ze znajomymi.
- **5 nowych przepisów KubeJS** na zaawansowane maszyny i systemy między modami.
- Dotychczasowe ID questów **1–326** i ich postęp nie zostały zmienione.
- **10 kolejnych modów** udostępnionych w 3 opcjonalnych instalatorach. Modów **nie pobiera automatycznie** zwykły skrypt pre-launch.

## Profile nowych modów

### 1. FABRYKI — 4 mody

Kliknij `INSTALUJ_FABRYKI.cmd` w folderze `C:\Tokyo-World`.

- Thermal Dynamics — przesył energii i płynów.
- Thermal Innovation — narzędzia, gadżety i mocniejsze wyposażenie technologiczne.
- Thermal Integration — kompatybilność receptur w serii Thermal.
- Thermal Cultivation — dodatkowe technologie farmingowe.

### 2. AUTOMATYKA — 3 mody

Kliknij `INSTALUJ_AUTOMATYKE.cmd`.

- Advanced Peripherals — komputery CC:Tweaked sterujące automatyzacją.
- CC:C Bridge — programowe sterowanie Create przez CC:Tweaked (wydanie 1.7.1 dla Create 6).
- Create: Enchantment Industry — automatyzacja zaklinania (wydanie 1.4.1 dla Create 6.0.8).

### 3. SUROWCE — 3 mody

Kliknij `INSTALUJ_SUROWCE.cmd`.

- Applied Flux — magazynowanie i przesył energii FE w sieci AE2.
- Hostile Neural Networks — symulowanie mobów i produkcja ich dropów z energii.
- Thermal's Delight — dodatkowe przepisy wykorzystujące Farmer's Delight i Thermal Cultivation (najpierw zainstaluj Fabryki).

Te 10 modów pochodzi z projektów dostępnych dla Forge 1.20.1; **nie zostały przetestowane wspólnie na Twoim komputerze**. Instalator wybiera tylko wydania Forge 1.20.1, część krytycznych wydań jest przypięta konkretnym ID i każdy pobierany plik JAR jest sprawdzany SHA-512. Dzięki temu nie podmieniamy istniejących modów w ciemno, ale nie gwarantuje to zgodności wszystkich dodatków ze sobą.

## Instalacja (bez zmian RAM)

1. Zamknij Minecrafta. Zrób kopię zapasową świata w Prism Launcher.
2. Otwórz PowerShell, wykonaj:
   `git -C "C:\Tokyo-World" pull --ff-only origin main`
3. Uruchom Tokyo World przez Prism Launcher i sprawdź FTB Quests — **36 rozdziałów / 518 questów**. Jeśli są błędy w księdze, nie instaluj jeszcze kolejnych modów.
4. Zamknij grę. Uruchom **INSTALUJ_FABRYKI.cmd**. Zobaczysz najpierw listę, potem potwierdzisz `T`.
5. Uruchom grę i przetestuj maszyny. Jeśli działa, zamknij i przejdź do **INSTALUJ_AUTOMATYKE.cmd**, potem ponownie przetestuj grę.
6. Dopiero po teście obu etapów uruchom **INSTALUJ_SUROWCE.cmd**.
7. Po każdej instalacji przejrzyj `minecraft/logs/latest.log` oraz listę modów Prism. W razie crasha prześlij log.

**Nie uruchamiaj wszystkich trzech profili naraz.** Przy zmianach składników paczki backup świata jest obowiązkowy. Serwer znajomych musi mieć odpowiednie mody po stronie serwera i zgodne konfiguracje.

## Expert — nowe crafting gates

W `kubejs/server_scripts/tokyo_world_expert_07.js`:
- Mekanism Quantum Entangloporter wymaga AE2 i zaawansowanych obwodów.
- AE2 Quantum Ring wymaga podzespołów Mekanism.
- Industrial Foregoing Machine Frame Advanced i Mob Duplicator wymagają technologii spoza samego Industrial Foregoing.
- Powah Nitro Reactor wymaga zaawansowanej energetyki Mekanism i Draconic Evolution.

Przepisy zostały zapisane w skrypcie, ale **nie są jeszcze zweryfikowane przez działający serwer Minecraft**. Jeśli JEI nie pokazuje nowej receptury, sprawdź `kubejs/logs/server.log` i `logs/latest.log`.

## Dalsze prace

- Po potwierdzeniu instalacji nowych modów: dodatkowe questy o ich własnych przedmiotach i mechanikach (bez ryzyka brakujących itemów w obecnej księdze).
- Balans produkcji energii, maszyn, mobów i doświadczenia po testach.
- Ostateczne menu FancyMenu i grafiki rozdziałów.
- Rozbudowa świata eksploracyjnego i bossów w kolejnych etapach.
