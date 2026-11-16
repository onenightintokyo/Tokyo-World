#!/usr/bin/env python3
"""Inspect *actual pinned Forge JAR resources* for item IDs used by Wave 06 quests.

This is much stronger than merely parsing quests. Not every mod translates
items via en_us.json, so unresolved keys are shown as warnings and must be
resolved by in-game registry tests before calling them definitively invalid.
"""
import hashlib
import io
import json
import re
import sys
import urllib.request
import zipfile
from collections import defaultdict
from pathlib import Path

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / "modpacks/tokyo_modules.json").read_text("utf8"))
map_ids = {
    "sophisticatedbackpacks": "sophisticated-backpacks",
    "sophisticatedstorage": "sophisticated-storage",
    "immersiveengineering": "immersiveengineering",
    "hexcasting": "hex-casting",
    "simplyswords": "simply-swords",
    "alexscaves": "alexs-caves",
    "ad_astra": "ad-astra",
}
projects = {e["project"]: e for g, vals in manifest["profiles"].items()
            if g.startswith("wave06-") for e in vals}
used = defaultdict(set)
for file in (root / "config/ftbquests/quests/chapters").glob("wave06_*.snbt"):
    source = file.read_text("utf8")
    for name in re.findall(r'\b(?:item|icon): "([a-z0-9_]+:[a-z0-9_/]+)"', source):
        ns, item = name.split(":", 1)
        if ns in map_ids:
            used[ns].add(item)

def read_json(uri):
    r = urllib.request.Request(uri, headers={
        "User-Agent": "TokyoWorld/ExpertFinalItemCheck (Forge 1.20.1)"})
    with urllib.request.urlopen(r, timeout=40) as response:
        return json.load(response)

unresolved = []
for namespace, project in map_ids.items():
    version_id = projects[project]["versionId"]
    version = read_json("https://api.modrinth.com/v2/version/" + version_id)
    primary = next((f for f in version["files"] if f.get("primary") and
                    f["filename"].endswith(".jar")), None)
    if primary is None:
        primary = next(f for f in version["files"] if f["filename"].endswith(".jar"))
    file_request = urllib.request.Request(primary["url"], headers={
        "User-Agent": "TokyoWorld/ExpertFinalItemCheck (Forge 1.20.1)"})
    with urllib.request.urlopen(file_request, timeout=100) as response:
        data = response.read()
    assert hashlib.sha512(data).hexdigest().lower() == primary["hashes"]["sha512"].lower(), \
        f"SHA512 mismatch in {project}"
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        filenames = z.namelist()
        translation_keys = set()
        for name in filenames:
            if re.fullmatch(r"assets/" + re.escape(namespace) + r"/lang/en_us\.json", name):
                translation_keys.update(json.loads(z.read(name).decode("utf8")).keys())
        resources = set(filenames)
        for item in sorted(used[namespace]):
            keys = [
                f"item.{namespace}.{item}", f"block.{namespace}.{item}",
                f"item.{namespace}.{item.replace('/', '.')}",
                f"block.{namespace}.{item.replace('/', '.')}",
            ]
            models = [
                f"assets/{namespace}/models/item/{item}.json",
                f"assets/{namespace}/blockstates/{item}.json",
            ]
            present = any(key in translation_keys for key in keys) or \
                      any(model in resources for model in models)
            if not present:
                unresolved.append((namespace, item, version["version_number"]))
            print(f"{'FOUND' if present else 'CHECK'} {namespace}:{item}", flush=True)

if unresolved:
    print("\nUnresolved item IDs (not proof of absence; registries may be dynamic):")
    for ns, item, version in unresolved:
        print(f"  {ns}:{item}  |  Forge version {version}")
    print(f"{len(unresolved)} items require an in-game /kubejs hand registry check.")
else:
    print("All new chapter item IDs found in their pinned Forge JAR assets.")
