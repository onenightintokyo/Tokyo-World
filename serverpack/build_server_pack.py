#!/usr/bin/env python3
"""Tokyo World server-only ZIP builder.

Only publishes a complete archive after every required JAR has been obtained
and verified from its original publisher. No Forge installer, launcher or
client-side mods are included. Python 3.11+, stdlib only.
"""
from __future__ import annotations

import hashlib
import json
import os
import re
import shutil
import sys
import time
import tomllib
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
from threading import Lock

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist"
STAGE = DIST / "TokyoWorld-Server-1.20.1-Forge-47.4.28"
MODDIR = STAGE / "mods"
REPORT = ROOT / "serverpack" / "reports"
USER_AGENT = "TokyoWorld-ServerPack/1.0 (Minecraft 1.20.1, Forge 47.4.28)"
API = "https://api.modrinth.com/v2/"
MINECRAFT = "1.20.1"
FORGE = "47.4.28"
ALLOWED_HOSTS = {
    "cdn.modrinth.com", "cdn-raw.modrinth.com",
    "mediafilez.forgecdn.net", "media.forgecdn.net", "edge.forgecdn.net"
}
CLIENT_PROFILE_NAMES = {"client-hud", "visual-extras", "visual-shaders"}
# Explicit extra exclusions for traditional client-only mods which may have
# "optional" or unspecified Modrinth environment metadata.
CLIENT_ONLY = re.compile(
    r"(?:^|[-_. ])(?:betterf3|appleskin|fancymenu|konkrete|melody|"
    r"oculus|embeddium|entityculling|mouse[_-]?tweaks|controlling|"
    r"searchables|betteradvancements|better[_-]?advancements|"
    r"jei|justenoughitems|jade|3dskinlayers|skinlayers3d|"
    r"immediatelyfast|entity[_-]?model[_-]?features|"
    r"entity[_-]?texture[_-]?features|not[_-]?enough[_-]?animations|"
    r"cloth[_-]?config|creativecoreclient)(?:[-_. ]|$)",
    flags=re.I
)
CF_SLUGS = {
    "compact-machines": "compact-machines",
    "building-gadgets": "building-gadgets",
    "mining-gadgets": "mining-gadgets",
    "mob-grinding-utils": "mob-grinding-utils",
    "flux-networks": "flux-networks",
    "laserio": "laserio",
    "ae2wtlib": "ae2wtlib",
    "twilightforest": "the-twilight-forest",
    "create-slice-and-dice": "slice-and-dice",
    "iron-chests-restocked": "iron-chests-restocked",
    "born-in-chaos": "born-in-chaos",
    "the-graveyard": "the-graveyard",
    "ftb-ultimine": "ftb-ultimine-forge",
}

ALL_MODS = {}
ALL_META = {}
ERRORS = []
CACHE = {}
LOCK = Lock()


def api_get(url: str, optional: bool = False) -> dict | list | None:
    with LOCK:
        if url in CACHE:
            return CACHE[url]
    for attempt in range(6):
        try:
            req = urllib.request.Request(
                url, headers={"User-Agent": USER_AGENT, "Accept": "application/json"})
            with urllib.request.urlopen(req, timeout=45) as resp:
                data = json.load(resp)
            with LOCK:
                CACHE[url] = data
            return data
        except urllib.error.HTTPError as exc:
            if optional and exc.code == 404:
                return None
            if exc.code not in (408, 429, 500, 502, 503, 504):
                raise
        except (urllib.error.URLError, TimeoutError):
            if attempt == 5:
                raise
        time.sleep(min(18, 2 * (attempt + 1)))
    raise RuntimeError("API failed after retries: " + url)


def is_client(name: str) -> bool:
    return bool(CLIENT_ONLY.search(name or ""))


def resolve_version(project: str, pinned: str = "") -> dict:
    details = api_get(API + "project/" + urllib.parse.quote(project))
    if details["project_type"] != "mod":
        raise ValueError("Not a Forge mod project: " + project)
    if details.get("server_side") == "unsupported":
        raise SkipClient(project)
    if pinned:
        version = api_get(API + "version/" + urllib.parse.quote(pinned))
        if version["project_id"] != details["id"]:
            raise ValueError("Pinned version belongs to a different project: " + project)
    else:
        q = urllib.parse.urlencode({
            "game_versions": json.dumps([MINECRAFT]),
            "loaders": json.dumps(["forge"])
        })
        choices = api_get(API + "project/" + urllib.parse.quote(details["id"]) + "/version?" + q)
        choices = [v for v in choices if "forge" in v.get("loaders", [])
                   and MINECRAFT in v.get("game_versions", [])]
        if not choices:
            raise ValueError("No Forge 1.20.1 version: " + project)
        releases = [v for v in choices if v.get("version_type") == "release"]
        version = max(releases or choices, key=lambda x: x.get("date_published", ""))
    if "forge" not in version.get("loaders", []) or MINECRAFT not in version.get("game_versions", []):
        raise ValueError("Wrong loader/game version for " + project)
    return version


class SkipClient(Exception):
    pass


def mod_file(version: dict) -> dict:
    files = [f for f in version.get("files", []) if f["filename"].endswith(".jar")]
    if not files:
        raise ValueError("No JAR in version " + version["id"])
    file = next((f for f in files if f.get("primary")), files[0])
    if not re.fullmatch(r"[a-fA-F0-9]{128}", file.get("hashes", {}).get("sha512", "")):
        raise ValueError("Missing SHA512 for " + file["filename"])
    return file


def add_mod(version: dict, reason: str, exact_base_sha1: str | None = None) -> None:
    project = version["project_id"]
    with LOCK:
        if project in ALL_MODS:
            return
        ALL_MODS[project] = None  # guard against cycles
    try:
        for dep in version.get("dependencies", []):
            if dep.get("dependency_type") != "required":
                continue
            dep_id = dep.get("project_id")
            if not dep_id:
                continue
            with LOCK:
                if dep_id in ALL_MODS:
                    continue
            try:
                add_mod(resolve_version(dep_id, ""), "dependency of " + reason)
            except SkipClient:
                # Required dependency marked as client-only: mod developer
                # may make it optional server-side, but this is not proven.
                print("Server-unsupported dependency excluded:", dep_id, flush=True)
        file = mod_file(version)
        if exact_base_sha1 and file["hashes"].get("sha1", "").lower() != exact_base_sha1.lower():
            raise ValueError("Original JAR SHA1 does not match resolved version")
        with LOCK:
            ALL_MODS[project] = {
                "project_id": project,
                "version_id": version["id"],
                "filename": file["filename"],
                "sha512": file["hashes"]["sha512"].lower(),
                "url": file["url"],
                "reason": reason,
                "sha1": file["hashes"].get("sha1", "").lower()
            }
    except Exception:
        with LOCK:
            ALL_MODS.pop(project, None)
        raise



def curseforge_version(project: str) -> dict:
    """Find the actual Forge 1.20.1 JAR by the CurseForge community API.

    CFWidget is metadata only; JAR bytes are fetched directly from the official
    ForgeCDN host. The resulting SHA256 is recorded in the manifest and pack.
    Never accept a Fabric/NeoForge/other-MC file when CFWidget falls back.
    """
    slug = CF_SLUGS[project]
    url = ("https://api.cfwidget.com/minecraft/mc-mods/" + slug +
           "?version=1.20.1&loader=forge")
    details = api_get(url)
    if not isinstance(details, dict):
        raise ValueError("CurseForge discovery response is not an object")
    file = details.get("download") or {}
    print("CurseForge response", project, "project keys",
          list(details.keys()), "release", str(file)[:900], flush=True)
    if not file:
        raise ValueError("No CurseForge release")
    versions = file.get("versions") or file.get("gameVersions") or []
    if not isinstance(versions, list):
        versions = []
    version_tokens = [str(v).lower() for v in versions]
    if not any("1.20.1" in token for token in version_tokens):
        raise ValueError("CurseForge file not marked Minecraft 1.20.1: " + str(versions))
    if not any("forge" == token or "forge" in token for token in version_tokens):
        raise ValueError("CurseForge file not marked Forge: " + str(versions))
    filename = file.get("name") or file.get("fileName") or file.get("filename")
    fileid = file.get("id")
    if not isinstance(fileid, int) or not filename or not str(filename).endswith(".jar"):
        raise ValueError("CurseForge file ID/filename missing: " + str(file))
    if Path(filename).name != filename:
        raise ValueError("Unsafe CurseForge filename")
    path = f"{fileid//1000}/{fileid%1000}/{filename}"
    source = f"https://mediafilez.forgecdn.net/files/{path}"
    return {
        "filename": filename,
        "sha1": "",
        "url": source,
        "reason": "official CurseForge: " + project +
            ", Forge 1.20.1, file id " + str(fileid),
    }


def resolve_all() -> None:
    manifest = json.loads((ROOT / "modpacks/tokyo_modules.json").read_text("utf8"))
    if manifest["minecraft"] != MINECRAFT or manifest["loader"] != "forge":
        raise ValueError("Wrong modpack game version/loader")
    baselines = []
    for ln in (ROOT / "modpacks/base_mod_hashes.txt").read_text("utf8").splitlines():
        if not ln.strip() or ln.startswith("#"):
            continue
        name, sha1 = ln.split("|", 1)
        if not re.fullmatch(r"[a-fA-F0-9]{40}", sha1):
            raise ValueError("Bad SHA1 for " + name)
        if not is_client(name):
            baselines.append((name, sha1))
    print(f"Original baseline: {len(baselines)} server candidate JARs", flush=True)
    for name, sha1 in baselines:
        try:
            version = api_get(API + "version_file/" + sha1 + "?algorithm=sha1", optional=True)
            if version is None:
                # The only supported non-Modrinth baseline source is an
                # official CurseForge file with a known, pinned SHA1 below.
                if name == "ftb-quests-forge-2001.4.22.jar":
                    ALL_META[name] = {
                        "filename": name, "sha1": sha1,
                        "url": "https://mediafilez.forgecdn.net/files/8078/538/" + name,
                        "reason": "original baseline (official CurseForge)"
                    }
                    continue
                if name.startswith(("ftb-library-", "ftb-teams-")):
                    # A verified copy is resolved from community-core below.
                    continue
                ERRORS.append("Baseline not available by exact SHA1: " + name +
                              " | " + sha1)
                continue
            if version.get("project_id"):
                if version.get("server_side") == "unsupported":
                    continue
                if MINECRAFT not in version.get("game_versions", []):
                    ERRORS.append("Baseline wrong game version: " + name)
                    continue
                add_mod(version, "original baseline " + name, sha1)
        except Exception as exc:
            ERRORS.append("Cannot resolve original baseline " + name + ": " + str(exc))
    print("Original baseline resolved, errors:", len(ERRORS), flush=True)

    # These FTB JARs are official pinned CurseForge versions used in client.
    for group, entries in manifest["profiles"].items():
        if group in CLIENT_PROFILE_NAMES:
            continue
        for entry in entries:
            project = entry["project"]
            if is_client(project):
                continue
            if entry.get("source") == "curseforge":
                sha1 = entry.get("sha1", "").lower()
                if not re.fullmatch(r"[a-fA-F0-9]{40}", sha1):
                    ERRORS.append("Missing official SHA1 for " + project)
                    continue
                ALL_META[project] = {
                    "filename": entry["fileName"],
                    "sha1": sha1,
                    "url": entry["url"],
                    "reason": "official CurseForge " + project
                }
                continue
            try:
                add_mod(resolve_version(project, entry.get("versionId", "")),
                        "manifest " + group + "/" + project)
            except SkipClient:
                print("Client-only project skipped:", project, flush=True)
            except Exception as exc:
                if project in CF_SLUGS:
                    try:
                        ALL_META[project] = curseforge_version(project)
                        print("Resolved official CurseForge:", project,
                              ALL_META[project]["filename"], flush=True)
                        continue
                    except Exception as cf_exc:
                        ERRORS.append(f"Failed {group}/{project}: Modrinth {exc}, "
                                      f"CurseForge {cf_exc}")
                        continue
                ERRORS.append(f"Failed {group}/{project}: {exc}")
    with LOCK:
        for modid in [k for k,v in ALL_MODS.items() if v is None]:
            ERRORS.append("Unresolved dependency cycle: " + modid)
            del ALL_MODS[modid]


def download_one(entry: dict) -> str:
    url = entry["url"]
    parsed = urllib.parse.urlparse(url)
    if parsed.scheme != "https" or parsed.hostname not in ALLOWED_HOSTS:
        raise ValueError("Unexpected download host: " + parsed.hostname)
    filename = entry["filename"]
    if filename != Path(filename).name or not filename.endswith(".jar"):
        raise ValueError("Unsafe filename: " + filename)
    dest = MODDIR / filename
    algorithm = "sha512" if entry.get("sha512") else "sha1"
    digest = entry[algorithm]
    if dest.exists():
        if not digest or getattr(hashlib, algorithm)(dest.read_bytes()).hexdigest() == digest:
            return filename
        raise ValueError("Conflicting contents for JAR " + filename)
    tmp = dest.with_suffix(".jar.download")
    for attempt in range(3):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=120) as response, tmp.open("wb") as fp:
                final = urllib.parse.urlparse(response.url)
                if final.scheme != "https" or final.hostname not in ALLOWED_HOSTS:
                    raise ValueError("Unexpected CDN redirect host: " + final.hostname)
                shutil.copyfileobj(response, fp)
            if tmp.stat().st_size > 100_000_000:
                raise ValueError("Oversized JAR: " + filename)
            if digest and getattr(hashlib, algorithm)(tmp.read_bytes()).hexdigest().lower() != digest:
                raise ValueError("SHA mismatch: " + filename)
            if not zipfile.is_zipfile(tmp):
                raise ValueError("Official CDN returned a non-JAR: " + filename)
            entry["sha256"] = hashlib.sha256(tmp.read_bytes()).hexdigest()
            tmp.rename(dest)
            return filename
        except Exception:
            if tmp.exists():
                tmp.unlink()
            if attempt == 2:
                raise
            time.sleep(3 * (attempt+1))
    return filename


def sanity_check_jars(entries: list[dict]) -> None:
    # Forge entries are in META-INF/mods.toml. Prevent duplicate mod IDs caused
    # by baseline + latest versions of the same mod being packaged together.
    declared = {}
    for entry in entries:
        dest = MODDIR / entry["filename"]
        with zipfile.ZipFile(dest) as jar:
            candidates = [n for n in jar.namelist() if n.lower() == "meta-inf/mods.toml"]
            if not candidates:
                # Auxiliary Forge library metadata could be elsewhere; warn.
                print("WARN: no mods.toml in", dest.name, flush=True)
                continue
            data = tomllib.loads(jar.read(candidates[0]).decode("utf8-sig"))
            for item in data.get("mods", []):
                modid = item.get("modId")
                if modid in ("minecraft", "forge", None):
                    continue
                prev = declared.get(modid)
                if prev and prev != dest.name:
                    ERRORS.append(f"Duplicate Forge modId {modid}: {prev} / {dest.name}")
                else:
                    declared[modid] = dest.name


def copy_server_configs() -> int:
    allowed = [
        "config/ftbchunks-world.snbt",
        "config/sophisticatedbackpacks-common.toml"
    ]
    for path in allowed:
        src = ROOT / path
        if src.is_file():
            dst = STAGE / path
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
    for top in ("defaultconfigs", "kubejs"):
        folder = ROOT / top
        if folder.exists():
            shutil.copytree(folder, STAGE / top, dirs_exist_ok=True)
    src = ROOT / "config/ftbquests"
    if src.exists():
        shutil.copytree(src, STAGE / "config" / "ftbquests", dirs_exist_ok=True)
    # Never include FancyMenu client configs, shaders, HUDs or launcher files.
    return len(list(STAGE.rglob("*")))


def write_report(entries: list[dict]) -> None:
    REPORT.mkdir(parents=True, exist_ok=True)
    summary = {
        "minecraft": MINECRAFT, "forge": FORGE,
        "jar_count": len(entries), "downloaded_jars": len(list(MODDIR.glob("*.jar"))),
        "errors": ERRORS,
        "installed_mods": [{k:v for k,v in e.items() if k != "url"} for e in entries]
    }
    (REPORT / "server-pack-report.json").write_text(json.dumps(summary, indent=2))
    if ERRORS:
        (REPORT / "MISSING_MODS.txt").write_text("\n".join(ERRORS) + "\n")


def package() -> None:
    entries = [*ALL_META.values(), *(v for v in ALL_MODS.values() if v)]
    # Drop any duplicate named JAR already backed by authoritative baseline.
    uniq = {}
    for x in entries:
        previous = uniq.get(x["filename"])
        if previous and previous.get("sha1") != x.get("sha1"):
            ERRORS.append("Filename version conflict: " + x["filename"])
        uniq[x["filename"]] = x
    entries = list(uniq.values())
    MODDIR.mkdir(parents=True, exist_ok=True)
    print(f"Resolved {len(entries)} server candidate JARs; unresolved: {len(ERRORS)}", flush=True)
    write_report(entries)
    if ERRORS:
        print("STOP: cannot publish incomplete server pack. See serverpack/reports/MISSING_MODS.txt",
              flush=True)
        return
    with ThreadPoolExecutor(max_workers=8) as pool:
        tasks = {pool.submit(download_one, entry): entry["filename"] for entry in entries}
        for future in as_completed(tasks):
            try:
                name = future.result()
                print("DOWNLOADED", name, flush=True)
            except Exception as exc:
                ERRORS.append(tasks[future] + " download error: " + str(exc))
    if not ERRORS:
        sanity_check_jars(entries)
    copy_server_configs()
    write_report(entries)
    if ERRORS:
        print("STOP: JAR integrity or mod-ID checks failed.", flush=True)
        return
    checksum_text = "".join(
        hashlib.sha256((MODDIR/e["filename"]).read_bytes()).hexdigest() +
        "  mods/" + e["filename"] + "\\n" for e in entries
    )
    (STAGE / "SHA256SUMS.txt").write_text(checksum_text)
    readme = (
        "TOKYO WORLD — SERVER PACK / Forge 1.20.1-47.4.28 / Java 17\n\n"
        "This ZIP contains mods/*.jar, config/, defaultconfigs/ and kubejs/.\n"
        "It intentionally does NOT contain Forge bootstrap, server.jar, start scripts,\n"
        "launcher, resourcepacks, shaders, FancyMenu, or client-only HUD mods.\n\n"
        "Install Minecraft Forge SERVER 1.20.1-47.4.28 through your host panel.\n"
        "Stop the server, unpack these directories into the server root,\n"
        "and use Java 17. Back up any server files that already exist.\n"
        "Use the client Tokyo World pack of the same revision.\n"
        "Keep FTB Teams/Chunks claim and force-load limits configured.\n"
        "No world files are included; create a new world on the host.\n"
    )
    (STAGE / "README-SERWER.txt").write_text(readme, encoding="utf8")
    final = DIST / "TokyoWorld-Server-1.20.1-Forge-47.4.28.zip"
    with zipfile.ZipFile(final, "w", zipfile.ZIP_DEFLATED, compresslevel=4) as archive:
        for file in sorted(STAGE.rglob("*")):
            if file.is_file():
                archive.write(file, file.relative_to(STAGE))
    print("BUILT ZIP", final, "size", final.stat().st_size, "JARs", len(entries), flush=True)


if __name__ == "__main__":
    try:
        resolve_all()
        package()
    except BaseException as exc:
        ERRORS.append("Fatal build error: " + repr(exc))
        REPORT.mkdir(parents=True, exist_ok=True)
        (REPORT / "MISSING_MODS.txt").write_text("\n".join(ERRORS)+"\n")
        raise
    if ERRORS:
        sys.exit(1)
