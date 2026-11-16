#!/usr/bin/env python3
"""Verify official CurseForge-hosted Forge FTB JARs without redistributing them.

For each file, try CurseForge's file delivery CDNs and validate SHA1.
Print which exact URL works, for the launcher to use.
"""
import hashlib, urllib.request, urllib.error

FILES = [
 ("ftb-chunks-forge-2001.3.8.jar",8216874,"ee2d0cdac6d2ca31f43da30f76dff8c6bae9d687"),
 ("ftb-teams-forge-2001.3.2.jar",7499810,"c49023f5d471aa1ba7fc624d2565fe9e82b7117e"),
 ("ftb-library-forge-2001.2.13.jar",8226927,"a5427d5ff07eb044144ed0b54308370d88bce17e"),
 ("ftb-quests-forge-2001.4.22.jar",8078538,"70f3bd771ab3b5921ce1ff5f14166fab694ca1aa"),
]
failures=[]
for filename,fileid,sha in FILES:
    path=f"{fileid//1000}/{fileid%1000}/{filename}"
    verified=False
    for cdn in ("mediafilez.forgecdn.net","edge.forgecdn.net","media.forgecdn.net"):
        url=f"https://{cdn}/files/{path}"
        try:
            req=urllib.request.Request(url, headers={"User-Agent":"TokyoWorldLauncher/1.8"})
            with urllib.request.urlopen(req,timeout=25) as stream:
                data=stream.read(15_000_001)
                actual_url=stream.geturl()
            if len(data)>15_000_000:
                raise ValueError("File too large")
            got=hashlib.sha1(data).hexdigest()
            if got!=sha:
                raise ValueError(f"SHA1 mismatch {got}")
            print("VERIFIED",filename,len(data),"bytes",url,"final",actual_url,flush=True)
            verified=True
            break
        except Exception as exc:
            print("RETRY",url,str(exc)[:180],flush=True)
    if not verified:
        failures.append(filename)
if failures:
    raise SystemExit("Unable to verify official CDN downloads: "+", ".join(failures))
