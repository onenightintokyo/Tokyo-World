#!/usr/bin/env python3
"""Static checks for the one-source-of-truth Wave 06 Expert Final settings.

Runtime registries are checked in-game via KubeJS Item.exists. Modrinth build
compatibility is separately checked by tools/verify_wave06_modrinth.py.
"""
import json
import re
import tomllib
from collections import defaultdict
from pathlib import Path

root = Path(__file__).resolve().parents[1]
config_path = root / "kubejs/config/tokyo_world_expert_final.json"
config = json.loads(config_path.read_text("utf8"))
assert config["minecraft"] == "1.20.1" and config["loader"] == "forge"
world = config["world"]
assert world["hardDifficulty"] is True, "World must start at HARD difficulty"
assert world["naturalRegeneration"] is False, "Expert should require active healing"
assert 75 <= world["playersSleepingPercentage"] <= 100
assert 1 <= world["entityCrammingLimit"] <= 24
assert len(config["gameplay"]) == 6
assert all(value is True for value in config["gameplay"].values())

wave = root / "kubejs/server_scripts/tokyo_world_expert_wave06_final.js"
source = wave.read_text("utf8")
assert "JsonIO.read(settingsPath)" in source, "Expert settings must actually be consumed"
assert "ServerEvents.recipes" in source and "ServerEvents.loaded" in source
assert "server.runCommandSilent('difficulty hard')" in source
assert "Item.exists(output)" in source

# No duplicate KubeJS outputs across older scripts (KubeJS recipe removals
# cannot erase recipes registered earlier by KubeJS).
outputs = defaultdict(list)
for source_file in (root / "kubejs/server_scripts").glob("*.js"):
    content = source_file.read_text("utf8")
    for output in re.findall(r"(?:shaped|gate)\(\s*['\"]([a-z0-9_]+:[a-z0-9_]+)['\"]", content):
        outputs[output].append(source_file.name)
dupes = {output: paths for output, paths in outputs.items() if len(paths) > 1}
assert not dupes, f"Duplicate KubeJS recipe outputs: {dupes}"

must_gate = {
    "sophisticatedbackpacks:backpack",
    "sophisticatedbackpacks:iron_backpack",
    "sophisticatedbackpacks:gold_backpack",
    "sophisticatedbackpacks:diamond_backpack",
    "sophisticatedbackpacks:netherite_backpack",
    "sophisticatedstorage:controller",
    "immersiveengineering:heavy_engineering",
    "immersiveengineering:capacitor_hv",
    "hexcasting:spellbook",
    "simplyswords:diamond_katana",
    "ad_astra:nasa_workbench",
    "ad_astra:oxygen_distributor",
    "ad_astra:fuel_refinery",
    "mekanism:digital_miner",
    "ae2:quantum_ring",
    "industrialforegoing:mob_duplicator",
}
assert must_gate.issubset(outputs), f"Missing Expert recipes: {must_gate - outputs.keys()}"

# Validate the pattern of every directly authored Wave 06 3x3 shaped recipe.
# Requiring only three-symbol rows prevents invalid output on datapack reload.
patterns = re.findall(
    r"gate\(\s*['\"][a-z0-9_]+:[a-z0-9_]+['\"]\s*,\s*\[([^\]]+)\]",
    source
)
assert len(patterns) >= 13, f"Expected 13+ tier-gated recipes, got {len(patterns)}"
for lines in patterns:
    rows = re.findall(r"['\"]([^'\"]+)['\"]", lines)
    assert len(rows) == 3 and all(len(row) == 3 for row in rows), \
        f"Incorrect 3x3 recipe: {rows!r}"

# Real Forge-specific mod configuration — not a placeholder JSON.
with (root / "config/sophisticatedbackpacks-common.toml").open("rb") as fp:
    common_cfg = tomllib.load(fp)
assert common_cfg["common"]["chestLootEnabled"] is False

with (root / "defaultconfigs/sophisticatedbackpacks-server.toml").open("rb") as fp:
    backpack_cfg = tomllib.load(fp)
names = ("leatherBackpack", "copperBackpack", "ironBackpack",
         "goldBackpack", "diamondBackpack", "netheriteBackpack")
slots = [backpack_cfg["server"][name]["inventorySlotCount"] for name in names]
upgrades = [backpack_cfg["server"][name]["upgradeSlotCount"] for name in names]
assert slots == sorted(slots) and all(12 <= n <= 144 for n in slots)
assert upgrades == sorted(upgrades) and all(0 <= n <= 10 for n in upgrades)

# All configured files are synchronized by the existing launcher:
# GitHubSynchronizer Managed = "kubejs/".
sync = (root / "launcher-standalone/GitHubSynchronizer.cs").read_text("utf8")
assert '"kubejs/"' in sync and '"config/"' in sync and '"defaultconfigs/"' in sync
print(f"OK: Forge backpack TOML capacities {slots}; rare chest backpack loot disabled.")
print(f"OK: Expert Final HARD world settings, {len(patterns)} Wave 06 recipe gates, "
      f"{len(outputs)} distinct KubeJS recipe outputs with no duplicates.")
