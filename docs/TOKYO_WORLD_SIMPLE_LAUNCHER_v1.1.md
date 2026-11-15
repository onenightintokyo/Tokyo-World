# Tokyo World Launcher v1.1 — prosty ekran gry

**Windows x64 · Minecraft 1.20.1 / Forge 47.4.28 · niezależny od Prism**

## Co się zmieniło

Interfejs został uproszczony do:
- **NICK** — dla konta Microsoft jest to zweryfikowany nick Minecraft, dla trybu Offline wpisujesz wybrany nick;
- **RAM** — suwak pamięci od 2 do 24 GB, domyślnie 4 GB;
- **PREMIUM / MICROSOFT** lub **OFFLINE / NICK** — mały wybór trybu;
- **GRAJ** — jedyny główny przycisk, który obsługuje logowanie, instalację i aktualizację, a potem uruchamia Minecraft.

Grafika tła jest rysowana bezpośrednio w aplikacji: panorama Tokio, czerwone słońce, Fuji, brama torii i różowe płatki sakury, w stylu istniejących materiałów Tokyo World. Po uruchomieniu aplikacja nie wymaga pobierania osobnych grafik.

Usunięto z głównego ekranu: pole tokenu, folder instalacji, import ZIP, osobne przyciski logowania / pobierania / aktualizacji / folderu i duży dziennik. Ustawienia znajdują się pod małą ikoną **⚙** (folder, repozytorium aktualizacji, opcjonalny token, zakres modów). Przy pierwszym uruchomieniu pojawiają się raz.

## Naprawa logowania Microsoft

Po wybraniu PREMIUM i kliknięciu **GRAJ**:
1. Launcher uruchamia autoryzację Microsoft obsługiwaną przez CmlLib.Core.Auth.Microsoft;
2. Po powodzeniu pobiera nazwę profilu Minecraft Java Edition;
3. Aktualizuje widoczny nick w głównym polu i zapisuje go w ustawieniach;
4. Następnie synchronizuje repozytorium, przygotowuje mody i uruchamia grę.

Nie wpisujesz dowolnego nicku Premium — pochodzi z konta Microsoft. Przy przełączeniu na Offline pole nicku staje się edytowalne. Offline nie omija licencji ani autoryzacji serwerów premium.

## Czy można zaszyć token GitHub w EXE?

**Technicznie tak, lecz nie jest to bezpieczne.** Każdy posiadacz EXE może analizować plik i odzyskać wbudowany token. Ponieważ projekt źródłowy `onenightintokyo/Tokyo-World` jest prywatny, nie należy udostępniać znajomym swojego tokenu do tego repozytorium.

Dostępne ścieżki:

**A. Prywatne repozytorium:** każdy uprawniony użytkownik ma własny fine-grained token z `Contents: Read-only`, zapisany lokalnie w Windows DPAPI. To działa bez upubliczniania plików, ale znajomi muszą mieć nadany dostęp.

**B. Polecane: oddzielne PUBLICZNE repozytorium dystrybucyjne** — np. `onenightintokyo/Tokyo-World-Client`. Umieść w nim wyłącznie pliki przeznaczone dla klientów: `config/`, `defaultconfigs/`, `kubejs/`, `resourcepacks/`, `shaderpacks/`, `modpacks/tokyo_modules.json`, `modpacks/base_mod_hashes.txt`. Repozytorium z kodem, prywatnymi narzędziami i tokenami pozostanie zamknięte.

W repozytorium źródłowym dodałem opcjonalny workflow **Publish curated Tokyo World client pack**:
1. Utwórz puste **publiczne repozytorium** GitHub `Tokyo-World-Client` (osobno od prywatnego `Tokyo-World`);
2. W prywatnym repo `Tokyo-World` otwórz **Settings → Secrets and variables → Actions**;
3. W **Variables** dodaj `TOKYO_PUBLIC_REPO = onenightintokyo/Tokyo-World-Client`;
4. W **Secrets** dodaj `TOKYO_PUBLIC_PUBLISH_TOKEN` — fine-grained token o prawie **Contents: Read and write** ograniczony WYŁĄCZNIE do nowego publicznego repozytorium;
5. Wejdź do **Actions → Publish curated Tokyo World client pack → Run workflow**.
6. W launcherze pod **⚙** ustaw repozytorium `onenightintokyo/Tokyo-World-Client` i zostaw pole tokenu **puste**. Po udanym uruchomieniu możesz budować launcher dla znajomych z tym adresem jako domyślnym.

**Uwaga:** publikacja tych plików ujawnia je całemu Internetowi. Przed uruchomieniem workflow obejrzyj zawartość folderów konfiguracji i usuń z nich ewentualne hasła, klucze lub prywatne dane. Nie publikuj `saves/`, plików kont, haseł ani prywatnych zasobów.

## Instalacja nowego EXE

Gotowy plik **TokyoWorldLauncher-v1.1.exe** powstał w udanej kompilacji Windows GitHub Actions:

https://github.com/onenightintokyo/Tokyo-World/actions/runs/43755226737

1. Pobierz artefakt **TokyoWorld-Standalone-Windows-x64** i wypakuj EXE.
2. Zastąp poprzedni `TokyoWorldLauncher.exe` nowym (najpierw zamknij stary proces).
3. Wybierz Premium lub Offline, nick (Offline), RAM i naciśnij **GRAJ**.
4. Przy pierwszym uruchomieniu potwierdź folder instalacji i źródło aktualizacji w ustawieniach.

## Testy i ograniczenia

Projekt pomyślnie przechodzi kompilację .NET 10 dla Windows x64 i jest publikowany jako pojedynczy samowystarczalny EXE. **Pierwszy start gry, instalacja wszystkich modów i logowanie Microsoft nie były jeszcze przetestowane w sesji użytkownika.**

Instalator nadal pobiera bazowe JAR-y z Modrinth według ich SHA-1. Jeśli któreś dokładne wydanie nie jest już tam udostępniane, program przerwie uruchomienie zamiast używać niezweryfikowanego zamiennika. Jest to oddzielny problem dystrybucji modów, a nie interfejsu launchera.

Kliknięcie napisu statusu na dole otwiera dziennik operacji, który można skopiować przy zgłaszaniu problemów.
