#!/usr/bin/env python3
"""Static integrity check for Tokyo World content and Wave 06 questbook.
Does not replace an in-game Forge startup test.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
chapters = ROOT / "config/ftbquests/quests/chapters"
manifest = json.loads((ROOT / "modpacks/tokyo_modules.json").read_text(encoding="utf-8"))
assert manifest["minecraft"] == "1.20.1"
assert manifest["loader"] == "forge"
all_projects = {}
for profile, modules in manifest["profiles"].items():
    assert isinstance(modules, list), profile
    for item in modules:
        project = item["project"]
        assert project, f"Missing project: {profile}"
        all_projects.setdefault(project, []).append(profile)
for project in ("immersiveengineering", "sophisticated-backpacks", "sophisticated-storage",
                "alexs-caves", "hex-casting", "simply-swords", "ad-astra",
                "sophisticated-core", "citadel", "paucal", "botarium",
                "resourceful-lib", "resourceful-config", "architectury-api", "fzzy-config"):
    assert project in all_projects, f"Missing wave-06 project: {project}"

quest_ids = set()
new_dependencies = {}
wave_counts = {}
chapter_ids = set()
for path in chapters.glob("*.snbt"):
    text = path.read_text(encoding="utf-8")
    # A structural check: braces/brackets and quoted strings, including escaped quotes.
    stack = []
    in_string = False
    escaped = False
    for index, ch in enumerate(text):
        if in_string:
            if escaped:
                escaped = False
            elif ch == "\\":
                escaped = True
            elif ch == '"':
                in_string = False
            continue
        if ch == '"':
            in_string = True
        elif ch in "[{":
            stack.append(ch)
        elif ch in "]}":
            assert stack and stack.pop() == {"]": "[", "}": "{"}[ch], \
                f"Unbalanced FTB SNBT in {path.name} at {index}"
    assert not stack and not in_string, f"Unclosed SNBT in {path.name}"
    header_id = re.search(r'(?m)^\s*id:\s*"(A12[0-9A-F]{13})"', text)
    assert header_id, f"Missing chapter ID: {path.name}"
    assert header_id.group(1) not in chapter_ids, f"Duplicate chapter ID: {header_id.group(1)}"
    chapter_ids.add(header_id.group(1))
    ids = re.findall(r'(?m)^\s*id:\s*"(A13[0-9A-F]{13})"', text)
    for quest in ids:
        assert quest not in quest_ids, f"Duplicate quest ID: {quest}"
        quest_ids.add(quest)
    if path.name.startswith("wave06_"):
        wave_counts[path.name] = len(ids)
        assert "default_hide_dependency_lines: false" in text, path.name
        assert "hide_dependency_lines: false" in text, path.name
        assert len(ids) >= 9, f"Wave 06 chapter too short: {path.name}"
        # Each Wave 06 dependency is a real quest ID and must exist globally.
        deps = re.findall(r'dependencies:\s*\[([^\]]*)\]', text)
        assert len(deps) == len(ids), f"Quest without dependencies in {path.name}"
        quest_blocks = re.split(r'(?m)^\s*id:\s*"(A13[0-9A-F]{13})"', text)
        assert len(quest_blocks) >= 2 * len(ids) + 1
        for idx in range(1, len(quest_blocks), 2):
            quest = quest_blocks[idx]
            chunk = quest_blocks[idx + 1]
            dep_match = re.search(r'dependencies:\s*\[([^\]]*)\]', chunk)
            assert dep_match, f"No prerequisite: {quest}"
            new_dependencies[quest] = re.findall(r'"(A13[0-9A-F]{13})"', dep_match.group(1))
            assert new_dependencies[quest], f"Empty dependency list: {quest}"
            assert "type: \"item\"" in chunk or "type: \"checkmark\"" in chunk, \
                f"No meaningful task: {quest}"

assert len(wave_counts) == 5, f"Expected five Wave 06 chapters, found {wave_counts}"
assert sum(wave_counts.values()) == 49, wave_counts
for quest, dependencies in new_dependencies.items():
    for dep in dependencies:
        assert dep in quest_ids, f"{quest} references missing dependency {dep}"

visited, pending = set(), set()
def visit(q):
    if q in visited:
        return
    assert q not in pending, f"Quest dependency cycle: {q}"
    pending.add(q)
    for dep in new_dependencies.get(q, []):
        visit(dep)
    pending.remove(q)
    visited.add(q)
for q in new_dependencies:
    visit(q)

print(f"OK: {len(chapter_ids)} chapters, {len(quest_ids)} quests; " +
      f"{sum(wave_counts.values())} Wave 06 quests in five connected trees")
