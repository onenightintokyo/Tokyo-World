#!/usr/bin/env python3
"""Fail closed unless the built Tokyo World dedicated-server ZIP is complete.

Checks every JAR's archive CRC and SHA-256 against the generated checksum list.
Ensures client-only leftovers, old version collisions and launch files are absent.
"""
from __future__ import annotations

import hashlib
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ARCHIVE = ROOT / "dist" / "TokyoWorld-Server-1.20.1-Forge-47.4.28.zip"
REQUIRED = {
    "the_bumblezone-7.15.2+1.20.1-forge.jar",
    "sophisticatedcore-1.20.1-1.5.33.2475.jar",
    "ExtendedAE-1.20-1.4.18-forge.jar",
    "twilightforest-1.20.1-4.3.2508-universal.jar",
}
TWILIGHT_SHA256 = "0bdc89263616d1b35c32ef82c5e9c14cbd20368e2fe8b468c72a28320be7a778"
RETIRED = re.compile(r"every[-_.]?(?:compat|comp)", re.I)
ALLOWED_ROOTS = {"mods", "config", "defaultconfigs", "kubejs"}
ALLOWED_FILES = {"SHA256SUMS.txt", "README-SERWER.txt"}


def verify() -> None:
    if not ARCHIVE.is_file() or ARCHIVE.stat().st_size < 5_000_000:
        raise RuntimeError("Missing or unexpectedly small full server ZIP")
    with zipfile.ZipFile(ARCHIVE) as zf:
        names = [info.filename for info in zf.infolist() if not info.is_dir()]
        if len(names) != len(set(n.lower() for n in names)):
            raise RuntimeError("Duplicate or case-colliding ZIP entries")
        if any(n.startswith("/") or ".." in Path(n).parts for n in names):
            raise RuntimeError("Unsafe ZIP entry path")
        unexpected = [
            n for n in names
            if (n.split("/", 1)[0] not in ALLOWED_ROOTS and n not in ALLOWED_FILES)
        ]
        if unexpected:
            raise RuntimeError("Unexpected ZIP files (launch scripts or client resources): " +
                               ", ".join(unexpected[:10]))
        mods = sorted(n for n in names if n.startswith("mods/") and n.lower().endswith(".jar"))
        if len(mods) < 100:
            raise RuntimeError(f"Expected full modpack, only {len(mods)} JAR files found")
        for required in REQUIRED:
            if mods.count("mods/" + required) != 1:
                raise RuntimeError("Missing exact shared client/server version: " + required)
        retired = [n for n in mods if RETIRED.search(Path(n).name)]
        if retired:
            raise RuntimeError("Retired Every Compat found: " + ", ".join(retired))
        if "SHA256SUMS.txt" not in names:
            raise RuntimeError("Missing per-JAR SHA256SUMS manifest")
        checksums = {}
        for line in zf.read("SHA256SUMS.txt").decode("utf-8").splitlines():
            match = re.fullmatch(r"([a-f0-9]{64})  (mods/[^\r\n]+\.jar)", line.strip())
            if not match:
                raise RuntimeError("Malformed SHA256SUMS entry: " + line[:140])
            checksums[match.group(2)] = match.group(1)
        if set(checksums) != set(mods):
            missing = set(mods) - set(checksums)
            extra = set(checksums) - set(mods)
            raise RuntimeError(f"SHA256 manifest does not match mods: missing={missing}, extra={extra}")
        bad_zip_file = zf.testzip()
        if bad_zip_file is not None:
            raise RuntimeError("Corrupt ZIP member: " + bad_zip_file)
        for name in mods:
            digest = hashlib.sha256(zf.read(name)).hexdigest()
            if digest != checksums[name]:
                raise RuntimeError("JAR SHA256 mismatch: " + name)
            if name == "mods/twilightforest-1.20.1-4.3.2508-universal.jar" and digest != TWILIGHT_SHA256:
                raise RuntimeError("Twilight Forest JAR differs from the verified client/server release")
        print(f"VERIFIED: {len(mods)} mod JARs, exact shared versions, no Every Compat, " +
              "no launcher/start files, all ZIP CRCs and SHA256 match")


if __name__ == "__main__":
    try:
        verify()
    except BaseException as exc:
        print("SERVER ZIP VERIFICATION FAILED:", exc, file=sys.stderr, flush=True)
        raise
