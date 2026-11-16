#!/usr/bin/env python3
"""Create a safe, curated token-free client snapshot embedded in Tokyo World EXE.

The archive intentionally has NO JARs, source code, personal settings,
tokens, saves, users/accounts or GitHub credentials. Mods are still
downloaded from verified publisher sources on each user's machine.
"""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import os
import re
import sys

ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/"launcher-standalone"/"Assets"/"TokyoWorldClientSnapshot.zip"
FOLDERS=("config","defaultconfigs","kubejs","resourcepacks","shaderpacks")
EXTRAS=("modpacks/tokyo_modules.json","modpacks/base_mod_hashes.txt")
BLOCKED={".env",".git","launcher-settings.json","accounts.json","options.txt","usercache.json","servers.dat"}
SECRET=re.compile(rb"(github_pat_[A-Za-z0-9_]{20,}|gh[opusr]_[A-Za-z0-9]{30,})")
files=[]
for folder in FOLDERS:
    base=ROOT/folder
    if base.is_dir():
        files.extend(x for x in base.rglob("*") if x.is_file())
files.extend(ROOT/x for x in EXTRAS)
files=sorted(files,key=lambda p:p.relative_to(ROOT).as_posix())
if len(files)<30:
    raise RuntimeError("Client snapshot unexpectedly small (missing config/quest resources).")
total=0
for file in files:
    if file.is_symlink():
        raise RuntimeError("No symbolic links allowed: "+str(file))
    name=file.relative_to(ROOT).as_posix()
    if file.name.lower() in BLOCKED or any(x.startswith(".git") for x in file.parts):
        raise RuntimeError("Unexpected secret/local file: "+name)
    data=file.read_bytes()
    if SECRET.search(data):
        raise RuntimeError("Possible credentials in snapshot file: "+name)
    if len(data)>32*1024*1024:
        raise RuntimeError("Oversized client asset: "+name)
    total+=len(data)
    if total>256*1024*1024:
        raise RuntimeError("Client snapshot oversized")
DEST.parent.mkdir(parents=True,exist_ok=True)
with ZipFile(DEST,"w",ZIP_DEFLATED,compresslevel=6) as z:
    for file in files:
        z.write(file,file.relative_to(ROOT).as_posix())
with ZipFile(DEST) as z:
    assert z.testzip() is None
    assert all(n.startswith((*tuple(k+"/" for k in FOLDERS),"modpacks/")) for n in z.namelist())
    assert set(EXTRAS).issubset(set(z.namelist()))
    print(f"TOKYO CLIENT BOOTSTRAP: {len(files)} files, {DEST.stat().st_size} compressed bytes")
    print("Token-free curated resources ready; launcher will embed them inside the EXE.")
