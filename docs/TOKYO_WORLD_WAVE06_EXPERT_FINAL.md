# Tokyo World — Wave 06 Expert Final (Forge 1.20.1)

## Cel

To **docelowa konfiguracja nowej rozgrywki**, a nie kolejny testowy profil. Jeśli nie istnieje jeszcze świat, pierwsza sesja uruchomi komplet reguł bez migracji zapisów.

## Zainstalowany zestaw

Siedem modów treści: Sophisticated Backpacks, Sophisticated Storage, Immersive Engineering, Hex Casting, Simply Swords, Alex's Caves oraz Ad Astra. W manifestach znajduje się również 10 bibliotek zależności. Wszystkie **17 wersji przypięto do konkretnych wydań Forge 1.20.1**. Suma SHA-512 jest sprawdzana podczas pobierania.

Przewidziane jest 49 nowych questów w pięciu drzewkach progresji oraz wszystkie wcześniejsze rozdziały. Wszystkie identyfikatory FTB Quests są zachowane.

## Poziom trudności

Plik `kubejs/config/tokyo_world_expert_final.json` jest używany bezpośrednio przez `kubejs/server_scripts/tokyo_world_expert_wave06_final.js`. Domyślnie:

- Minecraft na poziomie Hard (po uruchomieniu świata).
- Brak naturalnej regeneracji zdrowia (leczą mikstury, jedzenie o efekcie leczenia, magia i inne źródła regeneracji).
- Do pominięcia nocy potrzebnych jest 100% obecnych graczy.
- Insomnia / fantomy aktywne.
- Maksymalnie 16 encji w ciasnej przestrzeni (ograniczenie farm i lagów).

Własne wartości można zmienić w JSON; plik jest synchronizowany z repozytorium, więc trwała zmiana powinna trafić do źródłowego manifestu.

## Receptury Expert Final

- Wczesna gra: pierwszy plecak z materiałów i żelaza; dalsze ulepszenia wymagają Create i jego precyzyjnych mechanizmów.
- Środek gry: złote plecaki, kontrolery Sophisticated Storage, stalowe multibloki IE, magia Hex Casting i mocniejsze uzbrojenie.
- Późna gra: diamentowe i netherytowe plecaki, HV, zaawansowane sterowniki, stół NASA, system tlenu oraz rafineria paliwa.
- Potężne maszyny z poprzednich fal (Digital Miner, AE2 Quantum Ring, Mob Duplicator, Mekasuit) nadal podlegają recepturom Expert v1.6.

Z poprzednich skryptów usunięto słabsze duplikaty przepisów. Wszystkie ulepszenia tworzą etapowe ścieżki, bez zamierzonego wymogu własnego wyniku jako składnika.

## Prawdziwe ustawienia Sophisticated Backpacks

- `config/sophisticatedbackpacks-common.toml`: `chestLootEnabled=false`, aby skrzynie z normalnego lootu nie rozdawały gotowych plecaków.
- `defaultconfigs/sophisticatedbackpacks-server.toml`: zmniejszone sloty w plecakach, od 21 na start do 108 w wersji netherytowej. Forge kopiuje to do **nowych światów** podczas inicjalizacji ustawień serwera.

| Rodzaj plecaka | Sloty | Sloty ulepszeń |
|---|---:|---:|
| Skórzany | 21 | 1 |
| Miedziany | 36 | 1 |
| Żelazny | 45 | 2 |
| Złoty | 63 | 2 |
| Diamentowy | 81 | 4 |
| Netherytowy | 108 | 5 |

## Jak rozpocząć nową grę

1. Uruchom Tokyo World Launcher i wybierz pełną paczkę w ustawieniach.
2. Zadbaj o dostęp do prywatnego repozytorium lub publicznego repozytorium dystrybucyjnego. Launcher nie zawiera tokenu GitHub.
3. Włącz GRAJ; program synchronizuje `config/`, `defaultconfigs/`, `kubejs/` i manifesty, a następnie pobiera brakujące mody.
4. Przy pierwszym uruchomieniu sprawdź logi Forge/KubeJS oraz listę modów — przed stworzeniem świata. Dla nowej rozgrywki wybierz standardową nową mapę, bez importu starszego zapisu.
5. Shadery są dodatkiem klienta, **nie są wymagane na serwerze**. Nie są automatycznie włączane.

## Wyniki kontroli

Automatyczna walidacja na GitHub Actions sprawdza: składnię KubeJS, brak kolizji receptur, JSON i TOML konfiguracji, 49 questów i ich zależności, metadane prawdziwych wydań Forge 1.20.1, sumy SHA-512 pobieranych JAR-ów i obecność przedmiotów z questów w zasobach modów.

**Nie jest to pełny test uruchomienia 250+ modów w grze**. Ostatecznym testem funkcjonalnym pozostaje odpalenie Minecrafta i otwarcie nowego świata, szczególnie w przypadku niezależnych modów zmieniających generowanie mapy lub silnik gry.

## Optymalizacja

Nie włączaj automatycznie shaderów przy pierwszym uruchomieniu. Zachowano dotychczasowy profil wydajności launchera z domyślnymi 10 GB RAM; przy rozbudowie należy monitorować wykorzystanie pamięci i czas generowania nowych chunków. Samo zwiększanie przydziału RAM nie usuwa konfliktów modów.
