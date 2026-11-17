#!/usr/bin/env python3
"""Build a hash-pinned, immutable-commit client file index for public raw updates.

This avoids calling api.github.com from the launcher for every individual file.
Only content synced by GitHubSynchronizer is indexed. It is NOT an inventory of
server-side JAR files, saves, user settings or GitHub credentials.
"""
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "dist" / "client-files.json"
PREFIXES = ("config/", "defaultconfigs/", "kubejs/", "resourcepacks/", "shaderpacks/")
EXTRAS = {"modpacks/tokyo_modules.json", "modpacks/base_mod_hashes.txt"}


def main() -> None:
    commit = os.getenv("GITHUB_SHA") or subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True
    ).strip()
    if len(commit) != 40 or any(c not in "0123456789abcdefABCDEF" for c in commit):
        raise ValueError("Not a full immutable commit SHA")
    checkout_commit = subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True
    ).strip()
    if checkout_commit.lower() != commit.lower():
        raise ValueError(
            "Build commit mismatch: GITHUB_SHA differs from checked-out HEAD"
        )
    tracked = subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT)
    entries = []
    for name in sorted(x.decode("utf-8") for x in tracked.split(b"\0") if x):
        if name not in EXTRAS and not name.startswith(PREFIXES):
            continue
        file = ROOT / name
        if not file.is_file() or file.is_symlink():
            raise ValueError(f"Missing or symlinked managed file: {name}")
        # Hash immutable Git blob bytes, NOT the checked-out filesystem:
        # Git for Windows can convert LF to CRLF during checkout, whereas
        # raw.githubusercontent.com serves the original committed LF bytes.
        canonical = subprocess.check_output(
            ["git", "show", f"{commit}:{name}"], cwd=ROOT
        )
        if len(canonical) > 500_000_000:
            raise ValueError(f"Managed Git blob too large: {name}")
        entries.append({
            "path": name,
            "sha256": hashlib.sha256(canonical).hexdigest(),
            "size": len(canonical),
        })
    paths = {item["path"] for item in entries}
    if not EXTRAS.issubset(paths) or len(entries) < 10:
        raise ValueError(f"Incomplete public client index: {len(entries)} files")
    payload = {"schema": 1, "commit": commit, "files": entries}
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(payload, ensure_ascii=False, separators=(",", ":")) + "\n", encoding="utf-8")
    print(f"Verified client update index: {len(entries)} files, pinned to {commit}")


if __name__ == "__main__":
    main()
