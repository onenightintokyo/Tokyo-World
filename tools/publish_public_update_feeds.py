#!/usr/bin/env python3
"""Publish public static update feeds after a verified Windows launcher release.

This script runs only inside GitHub Actions using the ephemeral GITHUB_TOKEN.
Users' launchers contact raw.githubusercontent.com instead of api.github.com.
Feeds are committed to .github/launcher-updates, excluded from launcher rebuild
triggers so no self-rebuilding loop is possible.
"""
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
FEED_DIR = ".github/launcher-updates"


def request(method: str, path: str, payload: dict | None = None) -> dict | None:
    repo = os.environ["GITHUB_REPOSITORY"]
    url = f"https://api.github.com/repos/{repo}/{path}"
    token = os.environ["GH_TOKEN"]
    body = None if payload is None else json.dumps(payload).encode("utf-8")
    headers = {
        "Authorization": "Bearer " + token,
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "TokyoWorld-CI-Publisher/1.0",
    }
    if body is not None:
        headers["Content-Type"] = "application/json"
    try:
        with urllib.request.urlopen(urllib.request.Request(
                url, method=method, headers=headers, data=body), timeout=45) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        if error.code == 404 and method == "GET":
            return None
        reason = error.read().decode("utf-8", errors="replace")[:1000]
        raise RuntimeError(f"Publishing {path} failed: HTTP {error.code}: {reason}") from error


def read_content(path: str) -> tuple[str | None, bytes | None]:
    record = request("GET", f"contents/{path}?ref=main")
    if not record:
        return None, None
    sha = record.get("sha")
    encoded = record.get("content", "").replace("\n", "")
    return sha, base64.b64decode(encoded)


def publish_content(path: str, content: bytes, message: str) -> None:
    sha, existing = read_content(path)
    if existing == content:
        print(f"Already current: {path}")
        return
    payload = {
        "message": message,
        "content": base64.b64encode(content).decode("ascii"),
        "branch": "main",
    }
    if sha is not None:
        payload["sha"] = sha
    request("PUT", f"contents/{path}", payload)
    print(f"Published public CDN feed: {path}, {len(content)} bytes")


def version_triplet(value: str) -> tuple[int, int, int]:
    if not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", value):
        raise ValueError(f"Invalid launcher version: {value}")
    return tuple(int(piece) for piece in value.split("."))


def main() -> None:
    if os.environ.get("GITHUB_REF") != "refs/heads/main":
        raise RuntimeError("Only publish public feeds from main")
    if not os.environ.get("GH_TOKEN"):
        raise RuntimeError("Missing short-lived GITHUB_TOKEN in Actions")
    repo = os.environ["GITHUB_REPOSITORY"]
    if repo != "onenightintokyo/Tokyo-World":
        raise RuntimeError("Unexpected update repository; refusing to publish")
    version = ET.parse(ROOT / "launcher-standalone/TokyoWorldLauncher.csproj") \
        .findtext("./PropertyGroup/Version")
    if not version:
        raise ValueError("Launcher project version missing")
    version_triplet(version)
    current = request("GET", f"contents/{FEED_DIR}/latest.json?ref=main")
    if current:
        old = json.loads(base64.b64decode(current["content"].replace("\n", "")))
        if version_triplet(old["version"]) > version_triplet(version):
            print(f"Skipping stale CI run {version}: already published {old['version']}")
            return

    exe = ROOT / "dist/TokyoWorldLauncher.exe"
    if not exe.is_file() or exe.stat().st_size < 10_000_000:
        raise ValueError("Published Windows EXE missing or unexpectedly small")
    digest = hashlib.sha256()
    with exe.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)

    index_path = ROOT / "dist/client-files.json"
    index = json.loads(index_path.read_text("utf-8"))
    if index.get("schema") != 1 or index.get("commit") != os.environ["GITHUB_SHA"]:
        raise ValueError("Client update index is not pinned to the build commit")
    if len(index.get("files", [])) < 10:
        raise ValueError("Client update index is incomplete")

    # Always publish file index FIRST, then the EXE manifest. Readers download
    # content against the immutable commit recorded in the index.
    publish_content(
        f"{FEED_DIR}/client-files.json",
        index_path.read_bytes(),
        f"Update verified Tokyo World client file index for {version}",
    )
    feed = {
        "schema": 1,
        "version": version,
        "sha256": digest.hexdigest(),
        "size": exe.stat().st_size,
        "download_url": (
            f"https://github.com/{repo}/releases/download/"
            f"tokyo-world-launcher-v{version}/TokyoWorldLauncher.exe"
        ),
        "commit": os.environ["GITHUB_SHA"],
    }
    publish_content(
        f"{FEED_DIR}/latest.json",
        (json.dumps(feed, indent=2) + "\n").encode("utf-8"),
        f"Publish Tokyo World Launcher {version} public auto-update feed",
    )
    print(f"Auto-update feed ready: v{version}, sha256={digest.hexdigest()}")


if __name__ == "__main__":
    main()
