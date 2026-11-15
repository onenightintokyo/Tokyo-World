# Tokyo World MEGA — naprawa 3 brakujących zależności

**Dotyczy Minecraft 1.20.1 / Forge 47.4.28 po instalacji Tokyo World MEGA.**

## Błąd wykryty z logu 15 listopada 2026

Podczas uruchamiania Forge zgłaszał brakujące wymagane zależności:

| Mod żądający biblioteki | Mod ID biblioteki | Wymagany zakres | Instalowana wersja |
|---|---|---|---|
| Iron's Spells 'n Spellbooks | `irons_lib` | `[1.20.1-2,1.20.1-3)` | Iron's Lib `1.20.1-2.3.0` |
| Iron's Spells 'n Spellbooks | `playeranimator` | `[1.0.2-rc1+1.20,)` | playerAnimator `1.0.2-rc1+1.20-forge` |
| ExtendedAE Plus | `expatternprovider` | `[1.20-1.4.6-forge,)` | Extended AE `1.20-1.4.18-forge` |

Niektóre wymagane zależności nie były poprawnie zadeklarowane w metadanych Modrinth udostępnionych instalatorowi MEGA. Poprawiono manifest i dodano wymienione biblioteki jako wymagane do naprawy (oraz do następnych instalacji MEGA).

Zweryfikowane wydania / identyfikatory Modrinth:
- Iron's Lib: https://modrinth.com/mod/irons-lib/version/IptroA3L
- playerAnimator: https://modrinth.com/mod/playeranimator/version/xe2EVE6q
- Extended AE: https://modrinth.com/mod/extended-ae/version/uq3lO4ER

## Jedno kliknięcie zamiast ponownego pobierania MEGA

1. Zamknij Minecraft.
2. Wykonaj `git -C "C:\Tokyo-World" pull --ff-only origin main` w PowerShell.
3. Uruchom `C:\Tokyo-World\NAPRAW_START_TOKYO.cmd`.
4. Skrypt `-Profile Repair -Apply` pobierze **maksymalnie 3 brakujące JAR-y**, zachowa istniejące, sprawdzi SHA-512 i wymagane zależności na Modrinth.
5. Uruchom Minecraft w Prism Launcher. Jeżeli Forge pokaże kolejne błędy, prześlij świeży `minecraft/logs/latest.log`. Nie trzeba od razu reinstalować paczki.

Uwaga: skrypt został sprawdzony statycznie, lecz nie uruchomiono go na komputerze użytkownika.
