# Tokyo World MEGA — wynik instalacji 15.11.2026

## Wynik dostarczonego przez gracza instalatora

- Profil `Mega` na Minecraft 1.20.1 / Forge.
- **95 zgłoszonych projektów**, w tym wcześniejsze profile i nowe dodatki.
- **59 nowych plików JAR** zainstalowanych; instalator zgłosił zakończenie sukcesem oraz zweryfikowane sumy SHA-512.
- **10 projektów pominiętych** na etapie wyboru: Compact Machines, Building Gadgets, Mining Gadgets, Mob Grinding Utils, Flux Networks, LaserIO, AE2 Wireless Terminals Library, The Undergarden, Twilight Forest i Repurposed Structures.
- Większość pominięć wynikała z błędu 404 pod zadanym slugiem Modrinth; `The Undergarden` nie miał pasującego *release* według aktualnego filtra instalatora.
- **Naprawiono slug Repurposed Structures:** zamiast `repurposed-structures` używamy oficjalnej pozycji `repurposed-structures-forge`. Potwierdzone wydanie Forge 1.20.1: https://modrinth.com/mod/repurposed-structures-forge/version/IOFBvdFx
- Nie należy zastępować 404 losowymi nieoficjalnymi portami bez weryfikacji.

## Ostrzeżenie o nazwach wydania

Wbrew pierwszej ocenie nazw plików:
- `aether-1.20.1-1.5.2-neoforge.jar` jest **oficjalnie udostępniany zarówno dla Forge, jak i NeoForge** w Minecraft 1.20.1: https://www.curseforge.com/minecraft/mc-mods/aether/files/6134920
- `entangled-1.3.21a-forge-mc1.20.4.jar` na stronie autora figuruje jako kompatybilny także z Minecraft 1.20.1: https://www.curseforge.com/minecraft/mc-mods/entangled/files/9520166

**Nie usuwaj tych dwóch JAR-ów wyłącznie z powodu nazw.** Pełna kompatybilność nowej paczki wymaga uruchomienia i logu `minecraft/logs/latest.log`.

## Następne kroki

1. Wykonaj kopię zapasową/zduplikuj instancję, jeśli nie zrobiono tego przed instalacją.
2. Pobierz poprawiony manifest: `git -C "C:\Tokyo-World" pull --ff-only origin main`.
3. Spróbuj uruchomić Minecraft w Prism Launcher na **kopii** instancji i sprawdź menu oraz wejście do świata.
4. Jeśli wystąpi crash, dostarcz `latest.log` (i `crash-reports`, jeśli istnieją).
5. Dopiero po poprawnym uruchomieniu ponownie uruchom MEGA, jeśli chcesz uzupełnić poprawiony Repurposed Structures. Instalator powinien pominąć zainstalowane 59 pozycji, a pobrać tylko nowe zgodne wersje.

Nie zmieniono RAM ani zapisów świata.
