#!/usr/bin/env python3
"""Verify FINAL Wave 06 on Modrinth's actual Forge 1.20.1 metadata.

Only network and metadata checks; no claims about in-game startup or runtime FPS.
Prints version IDs so release builds may be pinned explicitly.
"""
from __future__ import annotations
import json
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
manifest = json.loads((ROOT / "modpacks/tokyo_modules.json").read_text("utf8"))
WAVES = [k for k in manifest["profiles"] if k.startswith("wave06-")]
headers = {
    "User-Agent": "TokyoWorld/ExpertFinal-Wave06 (Forge 1.20.1; modpack validator)",
    "Accept": "application/json",
}
API = "https://api.modrinth.com/v2/"

def get(path):
    req = urllib.request.Request(API + path, headers=headers)
    for tries in range(4):
        try:
            with urllib.request.urlopen(req, timeout=24) as response:
                return json.load(response)
        except (urllib.error.URLError, TimeoutError) as exc:
            if tries == 3:
                raise RuntimeError(f"Cannot read Modrinth {path}: {exc}") from exc
            time.sleep(2 + tries * 2)

errors = []
releases = {}
for group in WAVES:
    for item in manifest["profiles"][group]:
        project = item["project"]
        if project in releases:
            continue
        try:
            details = get("project/" + urllib.parse.quote(project))
            if details["project_type"] != "mod":
                raise ValueError(f"Modrinth project type is {details['project_type']}, not mod")
            filt = urllib.parse.urlencode({
                "game_versions": json.dumps(["1.20.1"]),
                "loaders": json.dumps(["forge"]),
            })
            choices = get(f"project/{urllib.parse.quote(project)}/version?{filt}")
            choices = [v for v in choices
                       if "1.20.1" in v.get("game_versions", [])
                       and "forge" in v.get("loaders", [])
                       and any(f["filename"].endswith(".jar") and
                               re.fullmatch("[A-Fa-f0-9]{128}",
                                   f.get("hashes", {}).get("sha512", ""))
                               for f in v.get("files", []))]
            if not choices:
                raise ValueError("No Forge 1.20.1 release with SHA-512")
            pinned = item.get("versionId", "")
            if pinned:
                matches = [v for v in choices if v["id"] == pinned]
                if not matches:
                    raise ValueError("Pinned versionId not in compatible Forge 1.20.1 releases: " + pinned)
                version = matches[0]
            else:
                preferred = [v for v in choices if v.get("version_type") == "release"]
                version = max(preferred or choices,
                              key=lambda x: x.get("date_published", ""))
            releases[project] = version["id"]
            deps = [x.get("project_id") for x in version.get("dependencies", [])
                    if x.get("dependency_type") == "required"]
            print("OK:", group, project, "=>", version["id"],
                  version["version_number"], "required dependencies:", len(deps), flush=True)
        except Exception as exc:
            errors.append(f"{group}/{project}: {exc}")
            print("ERROR:", errors[-1], flush=True)

print("SELECTED_VERSION_IDS=" + json.dumps(releases, sort_keys=True))
if errors:
    print("\nFAILED metadata checks:", *errors, sep="\n - ", file=sys.stderr)
    sys.exit(1)
print(f"OK: {len(releases)} independent projects verified for Forge 1.20.1.")
