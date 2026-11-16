#!/usr/bin/env python3
"""Audit exact 1.20.1 Forge compatibility of v1.8 HUD and claim add-ons.

No downloads or guesswork: Modrinth API declares valid loader and game version.
Base FTB files that already exist are detected separately from the manifest.
"""
import json, urllib.request, urllib.error, urllib.parse, time, sys
from pathlib import Path

m=json.loads(Path("modpacks/tokyo_modules.json").read_text("utf8"))
all_mods={}
for name in ["community-core","client-hud"]:
    for o in m["profiles"][name]:
        all_mods[o["project"]]=o
found=[]
missing=[]
for slug,o in all_mods.items():
    try:
        req=urllib.request.Request(
            "https://api.modrinth.com/v2/project/"+urllib.parse.quote(slug),
            headers={"User-Agent":"TokyoWorld-ModpackAudit/1.8"})
        with urllib.request.urlopen(req,timeout=25) as h:
            project=json.load(h)
        query=urllib.parse.urlencode({
            "game_versions":json.dumps(["1.20.1"]),
            "loaders":json.dumps(["forge"])
        })
        req=urllib.request.Request(
            "https://api.modrinth.com/v2/project/"+urllib.parse.quote(project["id"])+"/version?"+query,
            headers={"User-Agent":"TokyoWorld-ModpackAudit/1.8"})
        with urllib.request.urlopen(req,timeout=25) as h:
            versions=json.load(h)
        version_id=o.get("versionId")
        candidates=[v for v in versions if
            "1.20.1" in v.get("game_versions",[]) and "forge" in v.get("loaders",[])]
        if version_id and not any(v["id"]==version_id for v in candidates):
            raise RuntimeError("Pinned versionId incompatible: "+version_id)
        if not candidates:
            raise RuntimeError("No Forge 1.20.1 release on Modrinth")
        found.append((slug,version_id or candidates[0]["id"]))
        print("OK",slug,"Forge 1.20.1",version_id or candidates[0]["id"],flush=True)
    except Exception as e:
        missing.append((slug,str(e)))
        print("MISSING",slug,str(e),flush=True)
print("RESULTS: "+json.dumps({"found":found,"missing":missing}))
if missing:
    sys.exit(1)
