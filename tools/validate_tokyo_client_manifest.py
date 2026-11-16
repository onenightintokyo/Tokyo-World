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
assert "extendedae-plus" in projects, "Missing Extended AE Plus"
assert "ae2" in projects, "Missing AE2"
baseline = (ROOT / "modpacks/base_mod_hashes.txt").read_text("utf-8")
assert "sophisticatedcore-1.20.1-1.5.33.2475.jar|bd67f2857cfb979b3479c8e17bb541dd6679ae3d" in baseline
print("Client manifest verified: required AE2/Extended AE/Glodium/GuideME versions, "
      "server parity pins and no Every Compat")
