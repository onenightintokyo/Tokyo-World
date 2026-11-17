#!/usr/bin/env python3
"""Publisher-verified JEI and FTB Quests update for Tokyo World 1.20.1 Forge.

Produces TWO small, self-contained overlay ZIPs with no server startup files.
Source/client mod: jei-1.20.1-forge-15.74.0.234.jar, pinned in base_mod_hashes.
"""
from __future__ import annotations
import hashlib
import json
import re
import time
import urllib.error
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "dist"
JAR = "jei-1.20.1-forge-15.74.0.234.jar"
SHA1 = "1ae408badfe8b57417f465a06fc4d03215dba993"
BASE = ROOT / "modpacks/base_mod_hashes.txt"
CHAPTERS = ROOT / "config/ftbquests/quests/chapters"
TABLES = ROOT / "config/ftbquests/quests/reward_tables"
ASSETROOT = ROOT / "kubejs/assets/tokyoworld/textures/gui/quests"
TABLE_IDS = {int(x, 16) for x in [
    "61A0000000000011", "61A0000000000022",
    "61A0000000000033", "61A0000000000044",
]}


def get(url: str, limit: int = 35_000_000) -> bytes:
    req = urllib.request.Request(url, headers={
        "User-Agent": "TokyoWorld-Verified-Forge-Server/1.0",
        "Accept": "application/json, application/octet-stream",
    })
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=60) as res:
                data = res.read(limit + 1)
                if len(data) > limit:
                    raise ValueError("Publisher response too large: " + url)
                return data
        except urllib.error.HTTPError as exc:
            if exc.code not in (429, 502, 503, 504) or attempt == 3:
                raise
            time.sleep(2 * (attempt + 1))
    raise RuntimeError("Cannot resolve publisher file")


def download_pinned_jei() -> bytes:
    if (JAR + "|" + SHA1) not in BASE.read_text("utf-8").splitlines():
        raise ValueError("JEI does not match the client's pinned base manifest")
    meta = json.loads(get("https://api.modrinth.com/v2/version_file/" + SHA1 +
                          "?algorithm=sha1"))
    if "1.20.1" not in meta.get("game_versions", []) or "forge" not in meta.get("loaders", []):
        raise ValueError("JEI publisher metadata does not target Forge 1.20.1")
    matched = [v for v in meta.get("files", [])
               if v.get("filename") == JAR and
               v.get("hashes", {}).get("sha1", "").lower() == SHA1]
    if len(matched) != 1:
        raise ValueError("Exact pinned JEI binary not available from publisher")
    file = matched[0]
    url = file["url"]
    if not url.startswith(("https://cdn.modrinth.com/", "https://cdn-raw.modrinth.com/")):
        raise ValueError("Untrusted download domain")
    data = get(url)
    if hashlib.sha1(data).hexdigest() != SHA1:
        raise ValueError("JEI SHA1 mismatch against client version")
    expected512 = file.get("hashes", {}).get("sha512", "").lower()
    if len(expected512) != 128 or hashlib.sha512(data).hexdigest() != expected512:
        raise ValueError("JEI publisher SHA512 mismatch")
    import io
    with zipfile.ZipFile(io.BytesIO(data)) as mod:
        if mod.testzip():
            raise ValueError("JEI JAR corrupt")
        if "META-INF/mods.toml" not in mod.namelist():
            raise ValueError("Downloaded JEI is not a Forge mod")
    return data


def gather_quest_files() -> list[Path]:
    chapters = sorted(CHAPTERS.glob("*.snbt"))
    reward_tables = sorted(TABLES.glob("*.snbt"))
    images = sorted(ASSETROOT.glob("tokyo_stage_*.png"))
    if len(chapters) != 41 or len(reward_tables) != 4 or len(images) != 4:
        raise ValueError("Tokyo FTB quest sources incomplete (need 41/4/4)")
    quest_ids = []
    table_refs = []
    for p in chapters:
        content = p.read_text("utf-8")
        if "default_quest_size: 1.55d" not in content:
            raise ValueError("Chapter still uses old spread-out layout: " + p.name)
        ids = re.findall(r'^\t\t\tid: "([0-9A-F]+)"', content, re.M)
        quest_ids.extend(ids)
        table_refs.extend(int(v) for v in re.findall(r'table_id:\s*(\d+)L', content))
        image_refs = re.findall(r'image:\s*"tokyoworld:textures/gui/quests/([^"]+)"', content)
        if len(image_refs) != 1 or not (ASSETROOT / image_refs[0]).is_file():
            raise ValueError("FTB Quest banner missing: " + p.name)
    if len(quest_ids) != 567 or len(set(quest_ids)) != len(quest_ids):
        raise ValueError("Quest IDs changed or duplicate quests found")
    if len(table_refs) != 567 or set(table_refs) - TABLE_IDS:
        raise ValueError("Invalid FTB random reward table references")
    all_ids = set(quest_ids)
    for p in chapters:
        for group in re.findall(r'dependencies:\s*\[([^\]]+)\]', p.read_text("utf-8")):
            for ident in re.findall(r'"([0-9A-F]+)"', group):
                if ident not in all_ids:
                    raise ValueError(f"Quest prerequisite missing: {p.name} -> {ident}")
    for p in reward_tables:
        text = p.read_text("utf-8")
        if len(re.findall(r'item: \{ id:', text)) < 16 or 'type: "random"' in text:
            raise ValueError("Reward pool not expanded or malformed: " + p.name)
    for p in images:
        if not p.read_bytes().startswith(b"\x89PNG\r\n\x1a\n"):
            raise ValueError("Quest artwork not PNG: " + p.name)
    cucumber = ROOT / "config/cucumber-tags.json"
    json.loads(cucumber.read_text("utf-8"))
    return [*chapters, *reward_tables, *images, cucumber]


def main() -> None:
    files = gather_quest_files()
    jei = download_pinned_jei()
    OUT.mkdir(parents=True, exist_ok=True)
    for target, include_graphics in [
        (OUT / "TokyoWorld-Server-JEI-Quest-Hotfix.zip", False),
        (OUT / "TokyoWorld-Client-JEI-Quest-Fallback.zip", True),
    ]:
        with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
            z.writestr("mods/" + JAR, jei)
            for f in files:
                relative = f.relative_to(ROOT).as_posix()
                if not include_graphics and relative.startswith("kubejs/assets/"):
                    continue
                z.write(f, relative)
            z.writestr("TOKYO_README.txt",
                "Tokyo World / Minecraft 1.20.1 / Forge 47.4.28\n"
                "Stop Minecraft/server before applying; back up world/config.\n"
                "Extract into server root or client game root, keeping paths.\n"
                "Never use this client fallback when automatic launcher update works.\n"
                "Do not delete world data or reset quest progress.\n"
                "Exact pinned JEI SHA1: " + SHA1 + "\n")
        with zipfile.ZipFile(target) as z:
            assert z.testzip() is None
            assert z.read("mods/" + JAR) == jei
            assert len([n for n in z.namelist() if n.startswith("mods/")]) == 1
        print(target, target.stat().st_size, hashlib.sha256(target.read_bytes()).hexdigest(),
              flush=True)
    print("OK: 41 chapters, 567 quests, 4 random reward pools, JEI pinned, no start files")


if __name__ == "__main__":
    main()
