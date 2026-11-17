#!/usr/bin/env python3
"""Regression test: public CDN index must use committed blob bytes, not CRLF checkout bytes."""
from __future__ import annotations

import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INDEX = ROOT / "dist" / "client-files.json"
PROBE = "config/sophisticatedbackpacks-common.toml"


def git(*args: str) -> bytes:
    return subprocess.check_output(["git", *args], cwd=ROOT)


def main() -> None:
    index = json.loads(INDEX.read_text(encoding="utf-8"))
    commit = git("rev-parse", "HEAD").decode().strip()
    assert index["schema"] == 1 and index["commit"] == commit
    rows = index["files"]
    assert len(rows) >= 10
    assert len(rows) == len({x["path"] for x in rows}), "Duplicate file paths"

    for row in rows:
        name = row["path"]
        entry = git("ls-tree", "-z", commit, "--", name).rstrip(b"\0")
        assert entry, f"Missing git blob: {name}"
        metadata, path = entry.split(b"\t", 1)
        mode, kind, oid = metadata.split()
        assert kind == b"blob" and mode != b"120000"
        assert path.decode("utf-8") == name
        expected_size = int(git("cat-file", "-s", oid.decode()))
        canonical = git("cat-file", "blob", oid.decode())
        assert len(canonical) == expected_size
        assert row["size"] == expected_size, (
            f"{name}: index uses local checkout length {row['size']}, "
            f"Git blob has {expected_size} bytes"
        )
        assert row["sha256"] == hashlib.sha256(canonical).hexdigest(), (
            f"{name}: index hashes checkout-transformed bytes"
        )

    by_path = {row["path"]: row for row in rows}
    assert PROBE in by_path
    canonical = git("show", f"{commit}:{PROBE}")
    crlf_copy = canonical.replace(b"\r\n", b"\n").replace(b"\n", b"\r\n")
    assert canonical != crlf_copy, "CRLF regression probe is ineffective"
    assert by_path[PROBE]["size"] == len(canonical)
    assert by_path[PROBE]["size"] != len(crlf_copy), (
        "Regression probe failed to distinguish Git LF bytes from Windows CRLF"
    )
    print(
        f"OK: verified {len(rows)} canonical Git blobs; "
        f"{PROBE}: Git={len(canonical)} bytes, Windows CRLF={len(crlf_copy)} bytes"
    )


if __name__ == "__main__":
    main()
