#!/usr/bin/env python3
"""Deterministic, idempotent Tokyo World FTB Quests v2 layout/reward artwork.

Preserves ALL quest IDs, task IDs, dependencies and original random reward
table IDs, so existing multiplayer world progress is not reset.
"""
from __future__ import annotations
import hashlib
import json
import random
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
CHAPTERS = ROOT / "config/ftbquests/quests/chapters"
REWARDS = ROOT / "config/ftbquests/quests/reward_tables"
IMAGES = ROOT / "kubejs/assets/tokyoworld/textures/gui/quests"
ART_MARKER = ROOT / "tools/quest-art-revision.json"

ADD = {
    "tokyo_survival": [
        ("D", "minecraft:apple", 4, 6),
        ("E", "minecraft:steak", 4, 5),
        ("F", "minecraft:raw_iron", 4, 4),
        ("10", "minecraft:oak_sapling", 2, 5),
    ],
    "tokyo_industry": [
        ("D", "minecraft:piston", 2, 4),
        ("E", "minecraft:hopper", 1, 3),
        ("F", "minecraft:comparator", 2, 3),
        ("10", "minecraft:redstone_block", 2, 3),
    ],
    "tokyo_magic": [
        ("D", "minecraft:glistering_melon_slice", 4, 4),
        ("E", "minecraft:phantom_membrane", 2, 3),
        ("F", "minecraft:honeycomb", 6, 5),
        ("10", "minecraft:fermented_spider_eye", 4, 4),
    ],
    "tokyo_endgame": [
        ("D", "minecraft:ender_eye", 4, 4),
        ("E", "minecraft:golden_apple", 2, 3),
        ("F", "minecraft:crying_obsidian", 6, 4),
        ("10", "minecraft:prismarine_crystals", 12, 5),
    ],
}

QUEST_BLOCK = re.compile(r'\n\t\t\{\n\t\t\tid: "[0-9A-F]+"[\s\S]*?\n\t\t\}')
POSITION = re.compile(r'(\n\t\t\tx: )(-?[0-9.]+)d(\n\t\t\ty: )(-?[0-9.]+)d')


def dense_chapter(text: str, name: str) -> tuple[str, int]:
    if "default_quest_size: 1.55d" in text:
        return text, len(QUEST_BLOCK.findall(text))
    blocks = QUEST_BLOCK.findall(text)
    coords = [POSITION.search(block) for block in blocks]
    if not blocks or any(m is None for m in coords):
        raise ValueError("Unexpected quest layout: " + name)
    xs = [float(m[2]) for m in coords]
    ys = [float(m[4]) for m in coords]
    center = (min(xs) + max(xs)) / 2
    y_min = min(ys)

    def shrink(match: re.Match) -> str:
        block = match[0]
        def shift(p: re.Match) -> str:
            x = center + (float(p[2]) - center) * .84
            y = y_min + (float(p[4]) - y_min) * .78
            return f"{p[1]}{x:.2f}d{p[3]}{y:.2f}d"
        block, changes = POSITION.subn(shift, block, count=1)
        if changes != 1:
            raise ValueError("Missing quest coordinates: " + name)
        block = re.sub(r'(\n\t\t\tsize: )[0-9.]+d', r'\g<1>1.55d', block, count=1)
        block = re.sub(r'(\n\t\t\ticon_scale: )[0-9.]+d', r'\g<1>1.08d', block, count=1)
        return block
    text = QUEST_BLOCK.sub(shrink, text)
    text = re.sub(r'default_quest_size: [0-9.]+d', 'default_quest_size: 1.55d', text, count=1)
    if "width: 24.0d" not in text or "height: 7.8d" not in text:
        raise ValueError("Chapter banner layout changed: " + name)
    text = text.replace("width: 24.0d", "width: 25.5d", 1)
    text = text.replace("height: 7.8d", "height: 8.2d", 1)
    text = re.sub(
        r'(\n\t\t\tx: )-?[0-9.]+d(\n\t\t\ty: )-?[0-9.]+d(\n\t\t\torder: -1)',
        lambda m: f"{m[1]}{center:.2f}d{m[2]}{y_min-6:.2f}d{m[3]}",
        text, count=1,
    )
    return text, len(blocks)


def extend_reward_table(text: str, name: str) -> str:
    additions = ADD[name]
    prefix = {"tokyo_survival": "11", "tokyo_industry": "21",
              "tokyo_magic": "31", "tokyo_endgame": "41"}[name]
    if f'id: "{prefix}0000000000000D"' in text:
        return text
    if "\n\t]\n}" not in text:
        raise ValueError("Unrecognized reward table format: " + name)
    lines = []
    for item_id, item, count, weight in additions:
        reward_id = prefix + "000000000000" + item_id.rjust(2, "0")
        lines.append(
            f'\t\t{{ id: "{reward_id}" item: {{ id: "{item}" count: 1 }} '
            f'count: {count} weight: {weight}.0f }}'
        )
    return text.replace("\n\t]\n}", "\n" + "\n".join(lines) + "\n\t]\n}", 1)


def enhance_art() -> dict:
    if ART_MARKER.exists():
        marker = json.loads(ART_MARKER.read_text("utf-8"))
        if marker.get("revision") == 2:
            return marker
    palette = {
        "survival": (255, 178, 75),
        "industry": (40, 213, 242),
        "magic": (208, 109, 255),
        "endgame": (255, 69, 131),
    }
    result = {"revision": 2, "images": {}}
    for i, (stage, accent) in enumerate(palette.items()):
        file = IMAGES / f"tokyo_stage_{stage}.png"
        source = Image.open(file).convert("RGB")
        if source.size != (1200, 390):
            raise ValueError("Unexpected Tokyo banner dimensions: " + stage)
        artwork = ImageEnhance.Color(source).enhance(1.17)
        artwork = ImageEnhance.Contrast(artwork).enhance(1.07)
        haze = Image.new("RGBA", artwork.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(haze)
        rng = random.Random(19001 + i)
        # Neon particles over existing cyber-Tokyo skyline and sakura artwork.
        for _ in range(130):
            x = rng.randint(640, 1192)
            y = rng.randint(18, 366)
            radius = rng.choice((1, 1, 2, 3))
            c = rng.choice([(251, 70, 151, 95), (79, 222, 235, 85), (255, 203, 91, 80)])
            d.ellipse((x-radius, y-radius, x+radius, y+radius), fill=c)
        d.rounded_rectangle((18, 18, 1182, 370), radius=15,
                            outline=(*accent, 95), width=2)
        d.line((34, 356, 589, 356), fill=(*accent, 210), width=4)
        glow = haze.filter(ImageFilter.GaussianBlur(radius=7))
        artwork = Image.alpha_composite(artwork.convert("RGBA"), glow)
        artwork = Image.alpha_composite(artwork, haze).convert("RGB")
        artwork.save(file, "PNG", optimize=True)
        result["images"][file.name] = hashlib.sha256(file.read_bytes()).hexdigest()
    ART_MARKER.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return result


def main() -> None:
    found = sorted(CHAPTERS.glob("*.snbt"))
    if len(found) != 41:
        raise ValueError(f"Expected 41 FTB Quest chapters; found {len(found)}")
    count = 0
    changed = 0
    for file in found:
        old = file.read_text("utf-8")
        updated, quest_count = dense_chapter(old, file.name)
        count += quest_count
        if old != updated:
            file.write_text(updated, "utf-8", newline="\n")
            changed += 1
    if count != 567:
        raise ValueError(f"Quest count changed unexpectedly: {count}, expected 567")
    rewards_changed = 0
    for name in ADD:
        file = REWARDS / f"{name}.snbt"
        old = file.read_text("utf-8")
        updated = extend_reward_table(old, name)
        if updated != old:
            file.write_text(updated, "utf-8", newline="\n")
            rewards_changed += 1
    art = enhance_art()
    print(f"Tokyo World quest UI OK: {len(found)} chapters, {count} quests, "
          f"{changed} layouts adjusted, {rewards_changed} reward tables updated, "
          f"{len(art['images'])} artwork assets.")
    

if __name__ == "__main__":
    main()
