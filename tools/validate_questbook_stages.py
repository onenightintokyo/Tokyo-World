#!/usr/bin/env python3
"""Tokyo World: enforce a real five-stage FTB Quests sidebar.

Prevents stale chapter_group translations from overriding stage labels,
stray chapters escaping categories and duplicate order indexes.
Runs on CI for every questbook change; never changes quest progress IDs.
"""
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "config/ftbquests/quests"
CHAPTERS = ROOT / "chapters"
PHASES = [
    ("STARTOWE", 4, "6", ["tokyo_start", "tokyo_farming"]),
    ("EARLY GAME", 1, "a", [
        "tokyo_create", "caves_adventure", "storage_network", "wave06_field_logistics"
    ]),
    ("MID GAME", 2, "e", [
        "create_extended", "logistics_optimization", "automation_agriculture",
        "tokyo_mekanism", "powah_energy", "metallurgy", "immersive_industry",
        "wave06_immersive_industry", "mekanism_industry", "enderio_industry",
        "integrated_logic", "ae2_networks", "factory_logic", "logistics_pro",
        "industrial_farming", "bees_advanced", "botania_magic",
        "ars_magic", "wave06_hex_and_blades"
    ]),
    ("END GAME", 3, "c", [
        "chemical_superplant", "steel_alloy_empire", "megawatt_network",
        "me_data_center", "mystical_advanced", "cataclysm_bosses",
        "boss_hunting_contracts", "wave06_alex_caves", "space_industry",
        "wave06_space_frontier", "orbital_outpost", "mana_engineering",
        "mega_factory", "draconic_endgame", "tokyo_ultimate_project"
    ]),
    ("CREATIVE", 5, "b", ["food_architecture"])
]

expected = {}
chapter_ids = {}
order = 0
for stage_name, group_number, colour, names in PHASES:
    assert names, f"Empty stage: {stage_name}"
    for name in names:
        order += 1
        if name in expected:
            raise AssertionError(f"Duplicate chapter in plan: {name}")
        expected[name] = (group_number, colour, order)

files = {p.stem: p for p in CHAPTERS.glob("*.snbt")}
assert set(files) == set(expected), (
    f"Uncategorised chapters: {sorted(set(files) - set(expected))}\n"
    f"Missing chapters: {sorted(set(expected) - set(files))}"
)
quest_ids = set()
counts = defaultdict(int)
for name, (g, colour, number) in expected.items():
    data = files[name].read_text("utf-8")
    header = data.split("quests: [", 1)[0]
    def header_value(prop):
        match = re.search(r'(?m)^\s*'+prop+r':\s*"([^"]+)"', header)
        assert match, f"Missing {prop} in {name}"
        return match.group(1)

    group_id = header_value("group")
    assert group_id == f"A11000000000000{g}", (name, group_id, g)
    title = header_value("title")
    assert title.startswith(f"&{colour}&l{number:02d} · "), (name, title)
    chapter_id = header_value("id")
    assert chapter_id not in chapter_ids, f"Duplicate chapter ID: {chapter_id}"
    chapter_ids[chapter_id] = (name, g, colour, number)
    order_match = re.search(r'(?m)^\s*order_index:\s*(\d+)\s*$', header)
    assert order_match and int(order_match.group(1)) == number, (name, number)
    ids = re.findall(r'(?m)^\s*id:\s*"(A13[0-9A-F]{13})"', data)
    assert ids, f"Chapter {name} has no quests"
    for quest_id in ids:
        assert quest_id not in quest_ids, f"Duplicate quest: {quest_id}"
        quest_ids.add(quest_id)
    counts[g] += 1

groups_text = (ROOT / "chapter_groups.snbt").read_text("utf-8")
group_order = re.findall(r'\bid:\s*"(A11000000000000[0-9])"', groups_text)
wanted = [f"A11000000000000{g}" for _, g, _, _ in PHASES]
assert group_order == wanted, f"Incorrect sidebar group order: {group_order}; expected {wanted}"
for stage_name, g, colour, _ in PHASES:
    assert f'title: "&{colour}&l━━ {stage_name} ━━"' in groups_text

for locale in ("pl_pl", "en_us"):
    text = (ROOT / "lang" / f"{locale}.snbt").read_text("utf-8")
    group_keys = re.findall(r'\bchapter_group\.(A11[0-9A-F]{13})\.title:', text)
    assert group_keys == wanted, f"Outdated group translations in {locale}: {group_keys}"
    titles = dict(re.findall(r'\bchapter\.(A12[0-9A-F]{13})\.title:\s*"([^"]+)"', text))
    assert set(titles) == set(chapter_ids), (
        f"Missing chapter titles in {locale}: {set(chapter_ids) - set(titles)}; "
        f"orphaned: {set(titles) - set(chapter_ids)}"
    )
    for chapter_id, (name, g, colour, number) in chapter_ids.items():
        assert titles[chapter_id].startswith(f"&{colour}&l{number:02d} · "), (
            locale, name, titles[chapter_id]
        )

print("OK: 5 separated chapter groups; 41 numbered, fully categorised chapters.")
print(f"OK: {len(quest_ids)} unique quests; all original quest IDs preserved in source files.")
print("OK: both PL and EN localisation files agree with group order, colours and chapter numbers.")
for name, g, _, chapters in PHASES:
    print(f"  {name}: {len(chapters)} chapters")
