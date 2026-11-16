# Tokyo World 1.8.1 — znajomi bez tokenu

## Token GitHub nie jest przenoszony do EXE
Token użytkownika jest chroniony przez Windows DPAPI (CurrentUser), co uniemożliwia przenoszenie go między komputerami. Umieszczenie tokenu właściciela w EXE byłoby niebezpieczne: każdy użytkownik mógłby odczytać go i uzyskać dostęp do prywatnego repozytorium.

Nowy launcher zawiera wbudowaną kopię plików gry (config, defaultconfigs, kubejs, resourcepacks, shaderpacks i manifesty). Znajomi **nie potrzebują tokenu do pierwszej instalacji**: mod JAR jest pobierany z Modrinth lub oficjalnego CDN CurseForge.

**Ograniczenie:** automatyczne aktualizacje późniejszych zmian w prywatnym repo nadal wymagają dostępu. Aby znajomi mogli aktualizować bez tokenu, ustaw osobne publiczne repo dystrybucyjne. W prywatnym repo jest przygotowany workflow Publish curated Tokyo World client pack.

## Brakujący mod bazowy

FTB Quests 2001.4.22, Forge 1.20.1, jest pobierany z oficjalnego pliku CurseForge i sprawdzany według SHA-1: 70f3bd771ab3b5921ce1ff5f14166fab694ca1aa.

Jeśli jeszcze brakuje moda, launcher wyświetla jego dokładną nazwę zamiast tylko liczby.

## Usunięto Every Compat

- Nie ma Every Compat w manifeście ani w nowych instalacjach.
- Wszelkie istniejące pliki everycomp*.jar są automatycznie przenoszone do .tokyo/backups/retired-every-compat/.
- Wyłączenie trybu lekkiego nie przywraca go.
- Nowe paczki serwerowe również go nie instalują. Wcześniej pobrany serwerowy ZIP może nadal zawierać Every Compat; usuń everycomp*.jar ręcznie na serwerze.
- Jeśli istniejące światy zawierają bloki dodawane przez ten mod, przed usunięciem zrób kopię świata.

## Publiczny kanał aktualizacji — jednorazowe ustawienie przez właściciela

1. Utwórz osobne publiczne repozytorium GitHub, np. onenightintokyo/Tokyo-World-Client.
2. W źródłowym Tokyo-World dodaj w Settings → Secrets and variables → Actions zmienną TOKYO_PUBLIC_REPO = onenightintokyo/Tokyo-World-Client.
3. Dodaj sekret TOKYO_PUBLIC_PUBLISH_TOKEN: fine-grained token z prawem zapisu tylko do publicznego repo.
4. Uruchom workflow Publish curated Tokyo World client pack.
5. U znajomych ustaw nazwę tego publicznego repo w ⚙ i zostaw token pusty.

Nie umieszczaj żadnego tokenu w EXE ani nie wysyłaj go znajomym.

## Testowanie

GitHub Actions tworzy bezpieczny wbudowany ZIP (bez JARów, tokenów ani danych osobowych), kompiluje EXE i testuje uruchamianie UI. Pełne uruchomienie Forge i instalacja na komputerze znajomego muszą zostać osobno przetestowane.
