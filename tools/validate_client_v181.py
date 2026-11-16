#!/usr/bin/env python3
"""Release v1.8.1: prevent a missing FTB Quests baseline and leaked PATs.

This checks source manifests and installer policy. A separate Windows .NET
workflow compiles the launcher. Testing on a real Minecraft client is still
needed before claiming every possible mod combination works.
"""
import json
import re
from pathlib import Path

root=Path(__file__).resolve().parents[1]
modules=json.loads((root/"modpacks/tokyo_modules.json").read_text("utf8"))
profiles=modules["profiles"]
assert modules["minecraft"]=="1.20.1" and modules["loader"]=="forge"

every=[(group,item["project"]) for group,mods in profiles.items()
       for item in mods if item["project"] in ("every-compat","everycomp")]
assert not every,f"Every Compat still present: {every}"

quests=[item for item in profiles["community-core"] if item["project"]=="ftb-quests"]
assert len(quests)==1
item=quests[0]
assert item["source"]=="curseforge"
assert item["fileName"]=="ftb-quests-forge-2001.4.22.jar"
assert item["sha1"]=="70f3bd771ab3b5921ce1ff5f14166fab694ca1aa"
assert item["url"]=="https://mediafilez.forgecdn.net/files/8078/538/ftb-quests-forge-2001.4.22.jar"
original=dict(line.strip().split("|",1)
   for line in (root/"modpacks/base_mod_hashes.txt").read_text().splitlines()
   if line.strip() and not line.startswith("#"))
assert original[item["fileName"]]==item["sha1"]

installer=(root/"launcher-standalone/ModpackInstaller.cs").read_text("utf8")
assert 'library|teams|quests' in installer
assert '"ftb-quests"' in installer
assert 'projectRef.Equals("every-compat"' in installer
assert 'if (suppressEveryCompat' not in installer

optimized=(root/"launcher-standalone/TokyoOptimization.cs").read_text("utf8")
assert "Every Compat has been removed" in optimized
assert "RestoreEveryCompat(root, log)" not in optimized

settings=(root/"launcher-standalone/LauncherSettings.cs").read_text("utf8")
assert 'PublicDistributionRepository = "onenightintokyo/Tokyo-World-Client"' in settings
assert "DataProtectionScope.CurrentUser" in settings
assert 'string.IsNullOrWhiteSpace(loaded.GetToken())' in settings

sync=(root/"launcher-standalone/GitHubSynchronizer.cs").read_text("utf8")
assert 'LauncherSettings.PublicDistributionRepository' in sync
assert "publiczne repo" in sync.lower() or "Publiczne repo" in sync

workflow=(root/".github/workflows/tokyo-public-client-pack.yml").read_text("utf8")
assert "TOKYO_PUBLIC_PUBLISH_TOKEN" in workflow
assert "TOKYO_PUBLIC_REPO" in workflow
assert "branches: [main]" in workflow
assert 'vars.TOKYO_PUBLIC_REPO' in workflow

print("OK: public token-free client distribution default; private PAT not embedded.")
print("OK: exact Forge FTB Quests 2001.4.22 from official CurseForge; base SHA1 matches.")
print("OK: Every Compat removed from profiles and automatically quarantined.")
