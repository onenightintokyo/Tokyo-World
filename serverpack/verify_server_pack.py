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
    "jei-1.20.1-forge-15.74.0.234.jar",
    "ae2wtlib-15.3.3-forge.jar",
    "mob_grinding_utils-1.20.1-1.1.0.jar",
    "sliceanddice-forge-3.6.0.jar",
    "AdvancedAE-1.3.6-1.20.1.jar",
    "appliedenergistics2-forge-15.4.12.jar",
    "create-1.20.1-6.0.8.jar",
}
TWILIGHT_SHA256 = "0bdc89263616d1b35c32ef82c5e9c14cbd20368e2fe8b468c72a28320be7a778"
# Exact hashes from the published server ZIP, shared with the client manifest.
NETWORK_PARITY_SHA256 = {
    "mods/ae2wtlib-15.3.3-forge.jar": "39e5cede37d5c6a07592d6c1e83398791e037f3d434bc44f7460cd085a4de5f4",
    "mods/mob_grinding_utils-1.20.1-1.1.0.jar": "6e0fa6a245fd56cce99d60ac908b5028bc6392a59833c45e0bc3e27ec7f0a2f9",
    "mods/sliceanddice-forge-3.6.0.jar": "bafdcbeda044603b59eb16059829a6698c1a58d1ce865b8658e598f2718d315a",
    "mods/AdvancedAE-1.3.6-1.20.1.jar": "e216f95ac73eb4c1e8f3db22cc3d30f37f2612a7cb1762b9ebce148d0d5657b2",
    "mods/appliedenergistics2-forge-15.4.12.jar": "b9c5928893a20d3feb0b4393e2c23d756a16d9abee24d9c14c98e6096d53844d",
    "mods/create-1.20.1-6.0.8.jar": "6fbb910c367dbce8e4fc7e5bf64b6edd4de980906ed00af8e47e4af843c0d9b0",
}
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
        # The same pinned Packet Fixer release is installed from Modrinth on
        # clients, and must also be present in this server-only pack.
        packetfixers = [name for name in mods if re.fullmatch(
            r"mods/packetfixer(?:forge)?[-_.].*\.jar", name, flags=re.I)]
        if len(packetfixers) != 1:
            raise RuntimeError("Expected exactly one Packet Fixer Forge 1.20.1 JAR in server ZIP")
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
            if name == "mods/jei-1.20.1-forge-15.74.0.234.jar":
                actual_jei_sha1 = hashlib.sha1(zf.read(name)).hexdigest()
                if actual_jei_sha1 != "1ae408badfe8b57417f465a06fc4d03215dba993":
                    raise RuntimeError("Server JEI SHA1 mismatched to client pinned 15.74.0.234")
            digest = hashlib.sha256(zf.read(name)).hexdigest()
            if digest != checksums[name]:
                raise RuntimeError("JAR SHA256 mismatch: " + name)
            if name == "mods/twilightforest-1.20.1-4.3.2508-universal.jar" and digest != TWILIGHT_SHA256:
                raise RuntimeError("Twilight Forest JAR differs from the verified client/server release")
            if name in NETWORK_PARITY_SHA256 and digest != NETWORK_PARITY_SHA256[name]:
                raise RuntimeError("Shared client/server JAR differs from pinned release: " + name)
        print(f"VERIFIED: {len(mods)} mod JARs, exact shared versions, no Every Compat, " +
              "no launcher/start files, all ZIP CRCs and SHA256 match")


if __name__ == "__main__":
    try:
        verify()
    except BaseException as exc:
        print("SERVER ZIP VERIFICATION FAILED:", exc, file=sys.stderr, flush=True)
        raise
