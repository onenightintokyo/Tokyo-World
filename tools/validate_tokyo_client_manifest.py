#!/usr/bin/env python3
"""Fail the launcher build if its embedded mod list loses critical dependencies."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
manifest = json.loads((ROOT / "modpacks/tokyo_modules.json").read_text("utf-8"))
assert manifest["minecraft"] == "1.20.1"
assert manifest["loader"] == "forge"
projects = {}
for group, entries in manifest["profiles"].items():
    for entry in entries:
        slug = entry["project"].lower()
        assert slug not in {"every-compat", "everycomp", "everycompat"}, (
            "Every Compat must not return to the client")
        projects.setdefault(slug, []).append((group, entry))

pins = {
    "extended-ae": ("repair-dependencies", "versionId", "uq3lO4ER"),
    "packet-fixer": ("repair-dependencies", "versionId", "AScUisZl"),
    "kotlin-for-forge": ("repair-dependencies", "versionId", "Zsh14XeQ"),
    "thermal-foundation": ("repair-dependencies", "versionId", "44ilyZbi"),
    "guideme": ("repair-dependencies", "versionId", "i7Tp1AHw"),
    "glodium": ("repair-dependencies", "versionId", "eoUaDkZf"),
    "the-bumblezone": ("chapter-exploration", "version", "7.15.2+1.20.1-forge"),
    "sophisticated-core": ("wave06-field-logistics", "version", "1.5.33.2475"),
}
for slug, (group, key, pinned) in pins.items():
    values = projects.get(slug, [])
    assert len(values) == 1, f"{slug}: expected exactly one manifest entry, got {len(values)}"
    actual_group, entry = values[0]
    assert actual_group == group and entry.get(key) == pinned, (
        f"{slug}: unexpected group/version {actual_group}: {entry.get(key)}")
# Forge handshake channels: all 8 mods must be installed on EVERY client,
# with the exact publisher JAR bytes present in the verified dedicated-server pack.
multiplayer_pins = {
    "compact-machines": ("compactmachines-forge-6.0.3.jar", "41debe12bc7b013c39998788ed02ee971a11518f"),
    "building-gadgets": ("buildinggadgets2-1.0.8.jar", "d06a806e8ca84a9e96a69c32da4b43226a51e0c9"),
    "mining-gadgets": ("mininggadgets-1.15.6.jar", "79cfc6e0afc3ed3fc86bbda274ef3fccdc8260dd"),
    "flux-networks": ("FluxNetworks-1.20.1-7.2.1.15.jar", "65cea2d3da2a6d6fd274ef1dad37a516466be22c"),
    "laserio": ("laserio-1.6.8.jar", "f9e8dc07506eaa058f64e875e1f46dcd44ab6003"),
    "iron-chests-restocked": ("ironchests-5.0.2-forge.jar", "610aa09a1d83253db6bf32f6b12c84d446fa99b6"),
    "born-in-chaos": ("born_in_chaos_[Forge]1.20.1_1.7.5.jar", "4d616e1ccef4c55e14b04ca63228d826f4fdda5e"),
    "the-graveyard": ("The_Graveyard_3.1_(FORGE)_for_1.20.1.jar", "11ae4c94aa94b38b78cf9b901dad4612b679f8ac"),
}
for project, (filename, sha1) in multiplayer_pins.items():
    entries = projects.get(project, [])
    assert len(entries) == 1, f"{project}: expected one client mod, got {len(entries)}"
    group, entry = entries[0]
    assert group == "repair-dependencies", f"{project}: must be mandatory, not optional"
    assert entry.get("requiredForMultiplayer") is True, f"{project}: not mandatory"
    assert entry.get("source") == "curseforge", f"{project}: wrong source"
    assert entry.get("fileName") == filename, f"{project}: wrong server filename"
    assert entry.get("sha1") == sha1, f"{project}: checksum differs from server"
    assert entry.get("url", "").startswith("https://mediafilez.forgecdn.net/files/")

# Twilight's custom registries must match the dedicated-server JAR exactly.
twilight = projects.get("twilightforest", [])
assert len(twilight) == 1, "Expected one Twilight Forest entry"
_, twilight_entry = twilight[0]
assert twilight_entry.get("source") == "curseforge"
assert twilight_entry.get("fileName") == "twilightforest-1.20.1-4.3.2508-universal.jar"
assert twilight_entry.get("sha1") == "fa3b506a45d3e9c465551cd533fd50c899496e9f"
assert twilight_entry.get("url") == (
    "https://mediafilez.forgecdn.net/files/5468/648/"
    "twilightforest-1.20.1-4.3.2508-universal.jar"
), "Unexpected Twilight Forest source"
assert "extendedae-plus" in projects, "Missing Extended AE Plus"
assert "ae2" in projects, "Missing AE2"
baseline = (ROOT / "modpacks/base_mod_hashes.txt").read_text("utf-8")
assert "sophisticatedcore-1.20.1-1.5.33.2475.jar|bd67f2857cfb979b3479c8e17bb541dd6679ae3d" in baseline
print("Client manifest verified: required AE2/Extended AE/Glodium/GuideME versions, "
      "server parity pins and no Every Compat")
