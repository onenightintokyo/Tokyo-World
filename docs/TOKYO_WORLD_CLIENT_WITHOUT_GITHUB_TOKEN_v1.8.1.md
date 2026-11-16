# Tokyo World Launcher v1.8.1 — bez tokenu u znajomych i poprawka bazowego moda

## Co jest naprawione

- Każdy NOWY klient wybiera domyślnie `onenightintokyo/Tokyo-World-Client` — publiczne repozytorium z samymi plikami paczki.
- Starsze instalacje bez tokenu i z prywatnym adresem przechodzą automatycznie na publiczne źródło. Właściciel, który ma swój lokalny token, zachowuje prywatny adres.
- Nigdy nie dodawaj prywatnego PAT do pliku EXE lub kopii ustawień znajomych. PAT zapisany przez DPAPI w Windows działa tylko na koncie Windows, na którym został utworzony.
- `ftb-quests-forge-2001.4.22.jar`, którego oryginalna suma SHA-1 to `70f3bd771ab3b5921ce1ff5f14166fab694ca1aa`, jest pobierany z oficjalnego CurseForge, zamiast szukania go jedynie na Modrinth.
- Every Compat (`every-compat`, pliki `everycomp-*.jar`) jest wycofany ze wszystkich profili paczki. Stary JAR jest przenoszony do `.tokyo/optimizer-v15/disabled-every-compat/` (odwracalna kopia), niezależnie od ustawienia „Tryb lekki”.
- W razie realnie brakujących modów launcher podaje ich nazwy, a nie tylko liczbę.

## JEDNORAZOWE uruchomienie dystrybucji bez tokenu — właściciel paczki

**Ten krok jest niezbędny**. Bez niego klienci nie mogą czytać plików z prywatnego `onenightintokyo/Tokyo-World`. Po utworzeniu publicznego repo jego pliki są widoczne dla wszystkich, dlatego publikujemy wyłącznie wyselekcjonowane konfiguracje, questy, paczki zasobów i manifest modów.

1. Na swoim GitHubie utwórz nowe **PUBLICZNE** repozytorium o dokładnej nazwie `Tokyo-World-Client`. Zachowaj `Tokyo-World` jako prywatne.
2. Stwórz fine-grained Personal Access Token przeznaczony **tylko** dla `Tokyo-World-Client` z uprawnieniem `Contents: Read and write`. Nie umieszczaj go w launcherze.
3. W **PRYWATNYM** repo `Tokyo-World` wejdź: **Settings → Secrets and variables → Actions**:
   - **Variables**: `TOKYO_PUBLIC_REPO` = `onenightintokyo/Tokyo-World-Client`
   - **Secrets**: `TOKYO_PUBLIC_PUBLISH_TOKEN` = token stworzony do publikowania **wyłącznie** do publicznego repo.
4. Na stronie GitHub Actions prywatnego repo uruchom workflow `Publish curated Tokyo World client pack` przez `Run workflow`.
5. Zweryfikuj, że publiczne repo zawiera `config/`, `defaultconfigs/`, `kubejs/`, `resourcepacks/`, `shaderpacks/` (jeśli są) oraz `modpacks/tokyo_modules.json` i `modpacks/base_mod_hashes.txt`.
6. Po publikacji wszystkie kolejne zmiany tych folderów na `main` będą automatycznie wysyłane do publicznego repo; znajomi mogą używać launchera z **pustym polem tokenu**.

Nie kopiuj do publicznego repo: kodu launchera, `saves/`, `accounts/`, plików haseł, prywatnych tokenów ani zapisów ze świata. Nie publikuj, jeśli nie chcesz udostępniać konfiguracji całemu Internetowi.

## Przywracanie obecnego klienta

- Zainstaluj launcher v1.8.1, nie kasując folderu `%LOCALAPPDATA%\TokyoWorld`.
- Przy nowej instalacji pozostaw źródło `onenightintokyo/Tokyo-World-Client` i **puste** pole tokenu.
- Włącz `GRAJ`: FTB Quests zostanie pobrany po oficjalnym SHA-1, a Every Compat trafi do kopii.
- Dla starej instalacji z zaszyfrowanym PAT właściciela repo pozostanie prywatne źródło. Jeżeli chce korzystać z publicznego, może sam zmienić repozytorium na `onenightintokyo/Tokyo-World-Client` i usunąć token w ⚙.
- W starej paczce serwerowej z poprzednich archiwów również usuń `everycomp-*.jar`, zanim rozpoczniesz NOWY świat. Usunięcie moda z istniejącego, rozwiniętego świata może spowodować utratę bloków i przedmiotów.

## Status

Pobieranie modów i integralność plików można sprawdzić automatycznie; pełne uruchomienie prywatnej instancji gry wszystkich znajomych i weryfikacja konkretnego świata wymagają testu Minecraft/Forge na docelowym komputerze.
