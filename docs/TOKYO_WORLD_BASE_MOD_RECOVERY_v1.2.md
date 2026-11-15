# Tokyo World Launcher v1.2 — naprawa brakujących modów bazowych

## Dlaczego poprzedni EXE wyświetlał błąd

Launcher v1.1 wymagał 66 konkretnych, oryginalnych plików JAR. Ich lista znajduje się w `modpacks/base_mod_hashes.txt` i ma dokładne sumy SHA-1. Nie każda dokładna wersja archiwalnego moda jest publicznie dostępna na Modrinth. Instalator nie powinien uruchamiać niekompletnej gry, ale poprzedni uproszczony interfejs **nie zawierał przycisku importu**, co utrudniało ukończenie instalacji.

## Co poprawiono

Launcher v1.2 odzyskuje oryginalne pliki automatycznie:

1. Synchronizuje listę paczki z GitHub.
2. Sprawdza własny katalog `mods`.
3. Szuka brakujących JAR-ów w istniejących folderach Minecrafta: np. `%APPDATA%\PrismLauncher\instances\Tokyo_World_Forge_1.20.1\minecraft\mods`. To **jednorazowy import** istniejących plików, nie zależność uruchamiania od Prism.
4. Jeśli w folderze Pobrane, Pulpit albo Dokumenty znajduje się plik `Tokyo_World_Forge_1.20.1.zip`, próbuje uzupełnić bazę z tego archiwum automatycznie.
5. Dopiero wówczas odpytuje Modrinth o pozostałe brakujące dokładne wydania.
6. Jeśli nadal czegoś brakuje, otwiera standardowe okno Windows i pozwala **wskazać ZIP** zamiast niejasnego komunikatu o błędzie.
7. Każdy skopiowany mod jest weryfikowany SHA-1 zgodnie z oryginalnym indeksem; nie nadpisuje istniejących plików `.jar`.

Wszystkie przyciski głównego okna pozostają bez zmian: nick, RAM, Premium/Offline oraz GRAJ. Dodatkowe kroki importu pojawiają się wyłącznie w razie brakujących modów.

## Pierwszy start po aktualizacji

Pobierz nowszy `TokyoWorldLauncher.exe` z [GitHub Actions](https://github.com/onenightintokyo/Tokyo-World/actions). Zastąp stary EXE.

Nie usuwaj folderu gry utworzonego poprzednią wersją — nowe wydanie sprawdzi już pobrane pliki i uzupełni tylko brakujące.

Jeżeli na Twoim komputerze istnieje stara instancja Prism, launcher sam spróbuje znaleźć z niej oryginalne 66 JAR-ów. Jeśli instancji nie ma, możesz wskazać własny ZIP starej paczki w pojawiającym się oknie.

## Co pozostaje ograniczeniem

Przy całkowicie czystym komputerze bez oryginalnych modów lub archiwum ZIP instalator wciąż może mieć problem z 66 historycznymi wydaniami — brak publicznego źródła nie daje możliwości bezpiecznego pobrania tych dokładnych JAR-ów. Aby dystrybuować paczkę znajomym jako jeden instalator, wymagane jest przygotowanie publicznie dostępnego, zgodnego z licencjami źródła modów (np. plików z oficjalnych CDN) albo jednorazowa dystrybucja archiwum. Samo osadzenie prywatnego tokenu GitHub w EXE nie rozwiązuje problemu braku modów.

**Status:** zmiany w kodzie i test kompilacji .NET na Windows; pełna instalacja i uruchomienie świata wymagają dalszego testu.
