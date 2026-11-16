#!/usr/bin/env python3
"""Build a small server patch from the exact pinned Forge 1.20.1 Packet Fixer release."""
from __future__ import annotations
import hashlib
import io
import json
import urllib.request
import zipfile
from pathlib import Path

VERSION_ID = "AScUisZl"  # Modrinth Packet Fixer Forge 1.20.1, version 1.4.2
PROJECT_ID = "c7m1mi73"
OUT = Path("dist/TokyoWorld-Server-PacketFixer-Hotfix.zip")

def main() -> None:
    headers = {"User-Agent": "TokyoWorld-Server-Hotfix/1.0"}
    with urllib.request.urlopen(urllib.request.Request(
            "https://api.modrinth.com/v2/version/" + VERSION_ID,
            headers=headers), timeout=45) as r:
        metadata = json.load(r)
    if metadata.get("id") != VERSION_ID or metadata.get("project_id") != PROJECT_ID:
        raise ValueError("Unexpected Packet Fixer project/version ID")
    if "1.20.1" not in metadata.get("game_versions", []) or "forge" not in metadata.get("loaders", []):
        raise ValueError("Wrong game version or loader (requires Forge 1.20.1)")
    jars = [f for f in metadata.get("files", []) if f.get("filename", "").lower().endswith(".jar")]
    if len(jars) != 1:
        raise ValueError("Unexpected Packet Fixer JAR count")
    item = jars[0]
    name = item["filename"]
    if (name != Path(name).name or
            not name.lower().startswith("packetfixer") or
            not name.lower().endswith(".jar")):
        raise ValueError("Unsafe or unexpected JAR filename")
    link = item["url"]
    if not link.startswith(("https://cdn.modrinth.com/", "https://cdn-raw.modrinth.com/")):
        raise ValueError("Unexpected publisher host")
    with urllib.request.urlopen(urllib.request.Request(link, headers=headers), timeout=90) as r:
        data = r.read(20_000_000)
    actual = hashlib.sha512(data).hexdigest()
    expected = item.get("hashes", {}).get("sha512", "")
    if len(expected) != 128 or actual.lower() != expected.lower():
        raise ValueError("Packet Fixer publisher SHA512 mismatch")
    with zipfile.ZipFile(io.BytesIO(data)) as jar:
        if jar.testzip() is not None:
            raise ValueError("Corrupt Packet Fixer JAR")
        if "META-INF/mods.toml" not in jar.namelist():
            raise ValueError("Downloaded JAR does not have Forge mods.toml")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(OUT, "w", compression=zipfile.ZIP_DEFLATED) as z:
        z.writestr("mods/" + name, data)
    with zipfile.ZipFile(OUT) as z:
        assert z.testzip() is None and z.namelist() == ["mods/" + name]
    print(f"OK: {OUT} -> mods/{name} SHA512={actual}")

if __name__ == "__main__":
    main()
