#!/usr/bin/env python3
"""Pre-release check: AE2 + AdvancedAE pinned JARs are resolvable by SHA-1."""
from __future__ import annotations
import json
from pathlib import Path
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "modpacks" / "tokyo_modules.json"
HASHED = {"ae2", "advancedae"}

def fetch(sha1: str) -> dict:
    url = "https://api.modrinth.com/v2/version_file/" + sha1 + "?algorithm=sha1"
    for n in range(4):
        try:
            req = urllib.request.Request(url, headers={
                "User-Agent": "TokyoWorld-CI-PinnedModCheck/1.0",
                "Accept": "application/json"
            })
            with urllib.request.urlopen(req, timeout=30) as resp:
                return json.load(resp)
        except urllib.error.HTTPError as e:
            if e.code not in (429, 502, 503, 504):
                raise
            if n == 3:
                raise
            time.sleep(2 * (n + 1))
    raise RuntimeError("Unreachable Modrinth metadata endpoint")

def main() -> None:
    manifest = json.loads(MANIFEST.read_text("utf-8"))
    for project in HASHED:
        matches = [(group, e) for group, entries in manifest["profiles"].items()
                   for e in entries if e["project"] == project]
        if len(matches) != 1:
            raise RuntimeError("Wrong number of " + project + " entries")
        group, entry = matches[0]
        if entry.get("source") != "modrinth-sha1":
            raise RuntimeError("Missing exact publisher hash source for " + project)
        server_sha1 = entry["sha1"].lower()
        server_file = entry["fileName"]
        version = fetch(server_sha1)
        if "1.20.1" not in version.get("game_versions", []) or "forge" not in version.get("loaders", []):
            raise RuntimeError(project + ": incorrect Minecraft/loader")
        matches = [f for f in version.get("files", []) if f.get("filename") == server_file
                   and f.get("hashes", {}).get("sha1", "").lower() == server_sha1]
        if len(matches) != 1 or len(matches[0].get("hashes", {}).get("sha512", "")) != 128:
            raise RuntimeError(project + ": pinned SHA1 is unavailable from the mod publisher")
        print("Resolved exact Forge 1.20.1 JAR: " + server_file + " " + server_sha1, flush=True)

if __name__ == "__main__":
    main()
