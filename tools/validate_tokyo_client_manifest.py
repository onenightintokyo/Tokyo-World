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

# Exact client/server binaries. Identical displayed version numbers do
# not prove identical Forge networking or registry contents.
binary_pins = {
    "ae2wtlib": ("repair-dependencies", "curseforge", "ae2wtlib-15.3.3-forge.jar", "1a546af04844677542b6914191783c7de15364fc", "39e5cede37d5c6a07592d6c1e83398791e037f3d434bc44f7460cd085a4de5f4"),
    "mob-grinding-utils": ("repair-dependencies", "curseforge", "mob_grinding_utils-1.20.1-1.1.0.jar", "58991a3a94e1f982d0e558bed7b60226a097ae49", "6e0fa6a245fd56cce99d60ac908b5028bc6392a59833c45e0bc3e27ec7f0a2f9"),
    "create-slice-and-dice": ("repair-dependencies", "curseforge", "sliceanddice-forge-3.6.0.jar", "a4e47dbf4382ebe092b6c2cb71919e412b456d84", "bafdcbeda044603b59eb16059829a6698c1a58d1ce865b8658e598f2718d315a"),
    "advancedae": ("repair-dependencies", "modrinth-sha1", "AdvancedAE-1.3.6-1.20.1.jar", "d8d9612c37a6cece6cd885f33e52c1489467a13f", "e216f95ac73eb4c1e8f3db22cc3d30f37f2612a7cb1762b9ebce148d0d5657b2"),
    "ae2": ("tech-core", "modrinth-sha1", "appliedenergistics2-forge-15.4.12.jar", "4e81984691e35c729880a11f72deb52dcc74d41f", "b9c5928893a20d3feb0b4393e2c23d756a16d9abee24d9c14c98e6096d53844d"),
}
for project, (group_expected, source_expected, name_expected, sha1, sha256) in binary_pins.items():
    entries = projects.get(project, [])
    assert len(entries) == 1, f"{project}: expected exactly one manifest entry"
    group, entry = entries[0]
    assert group == group_expected, f"{project}: not in mandatory profile"
    assert entry.get("source") == source_expected, f"{project}: wrong source"
    assert entry.get("fileName") == name_expected, f"{project}: unexpected JAR"
    assert entry.get("sha1") == sha1 and entry.get("sha256") == sha256, (
        f"{project}: diverged from server SHA1/SHA256")
    assert entry.get("requiredForMultiplayer") is True, f"{project}: multiplayer required flag absent"
assert "create-1.20.1-6.0.8.jar|b13d912b9247a38d66d11598c121442585a1c1e9" in (
    ROOT / "modpacks/base_mod_hashes.txt").read_text("utf-8"), "Server Create 6.0.8 hash mismatch"

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
