#!/usr/bin/env python3
"""Tokyo World 1.8: four broad FTB Quests stages and bounded random rewards.

Run once on the questbook source tree. Chapter and quest IDs stay unchanged.
The generated reward tables only contain small utility/resource drops.
"""
import re
from pathlib import Path

ROOT = Path("config/ftbquests/quests")
GROUPS = [
    dict(id=4, name="START / SURVIVAL", color="6", art="survival", chapters=[
        "tokyo_start", "tokyo_farming", "caves_adventure", "tokyo_create",
        "storage_network", "wave06_field_logistics", "food_architecture"]),
    dict(id=2, name="INDUSTRY / AUTOMATION", color="b", art="industry", chapters=[
        "create_extended", "logistics_optimization", "tokyo_mekanism",
        "powah_energy", "metallurgy", "immersive_industry", "wave06_immersive_industry",
        "mekanism_industry", "enderio_industry", "integrated_logic", "ae2_networks",
        "factory_logic", "logistics_pro", "me_data_center", "megawatt_network",
        "chemical_superplant"]),
    dict(id=5, name="FARMING / MAGIC / EXPANSION", color="d", art="magic", chapters=[
        "automation_agriculture", "industrial_farming", "bees_advanced",
        "mystical_advanced", "botania_magic", "ars_magic", "wave06_hex_and_blades",
        "mana_engineering"]),
    dict(id=3, name="ENDGAME / SPACE / LEGACY", color="c", art="endgame", chapters=[
        "steel_alloy_empire", "mega_factory", "cataclysm_bosses",
        "boss_hunting_contracts", "wave06_alex_caves", "space_industry",
        "wave06_space_frontier", "orbital_outpost", "draconic_endgame",
        "tokyo_ultimate_project"])
]
TABLES = [
    ("61A0000000000011", "survival", "SURVIVAL SUPPLIES", [
        ("torch",16,10), ("bread",6,10), ("baked_potato",6,9),
        ("coal",8,8), ("oak_log",12,8), ("iron_ingot",3,3),
        ("leather",4,5), ("string",8,7), ("arrow",12,7),
        ("copper_ingot",8,5), ("bone_meal",8,6), ("glass",12,5)]),
    ("61A0000000000022", "industry", "INDUSTRIAL SUPPLIES", [
        ("redstone",12,10), ("lapis_lazuli",10,8), ("iron_ingot",7,9),
        ("copper_ingot",12,8), ("gold_ingot",4,4), ("quartz",12,7),
        ("obsidian",4,3), ("glass",16,7), ("coal",16,8),
        ("slime_ball",6,4), ("blaze_powder",3,2), ("experience_bottle",5,4)]),
    ("61A0000000000033", "magic", "FARMING / MAGIC MATERIALS", [
        ("bone_meal",16,12), ("glowstone_dust",8,7), ("lapis_lazuli",12,9),
        ("amethyst_shard",10,8), ("moss_block",8,6), ("honey_bottle",4,6),
        ("ender_pearl",2,2), ("golden_carrot",4,5), ("emerald",2,2),
        ("experience_bottle",6,5), ("flowering_azalea",2,5), ("melon_seeds",8,5)]),
    ("61A0000000000044", "endgame", "ELITE SUPPLIES - NO OP GEAR", [
        ("diamond",2,2), ("emerald",6,5), ("blaze_rod",4,4),
        ("ender_pearl",6,7), ("obsidian",12,8), ("experience_bottle",12,7),
        ("quartz",24,8), ("redstone",24,9), ("lapis_lazuli",16,8),
        ("gold_ingot",9,7), ("copper_block",3,6), ("amethyst_shard",12,7)])
]
TABLE_ID = {name: int(hexid, 16) for hexid, name, _, _ in TABLES}
QUEST_BLOCK = re.compile(r'^\t\t\{\r?\n\t\t\tid: "(A13[0-9A-F]{13})"[\s\S]*?^\t\t\}', re.M)
QUEST_ID = re.compile(r'(?m)^\t\t\tid: "(A13[0-9A-F]{13})"')
CHAPTER_ID = re.compile(r'(?m)^\tid: "(A12[0-9A-F]{13})"')
TITLE = re.compile(r'(?m)^\ttitle: "([^"]+)"')
chapter_ids = {}
quest_ids = set()
num = 0

def replace_reward_array(quest, replacement):
    idx = quest.find("rewards:")
    if idx < 0:
        return re.sub(r'(?m)^\t\t\}$', '\t\t\t'+replacement+'\n\t\t}', quest, count=1)
    start = quest.find("[", idx)
    if start < 0:
        raise ValueError("Missing opening rewards bracket")
    depth, quote, escaped, end = 0, False, False, -1
    for pos in range(start, len(quest)):
        ch = quest[pos]
        if quote:
            if escaped:
                escaped = False
            elif ch == "\\":
                escaped = True
            elif ch == '"':
                quote = False
        elif ch == '"':
            quote = True
        elif ch == "[":
            depth += 1
        elif ch == "]":
            depth -= 1
            if depth == 0:
                end = pos+1
                break
    if end < 0:
        raise ValueError("Unclosed quest reward array")
    return quest[:idx] + replacement + quest[end:]

planned = [name for group in GROUPS for name in group["chapters"]]
actual = [p.stem for p in (ROOT / "chapters").glob("*.snbt")]
if len(planned) != 41 or set(planned) != set(actual):
    raise ValueError("All 41 chapters must be mapped exactly once")

for group in GROUPS:
    for name in group["chapters"]:
        num += 1
        file = ROOT / "chapters" / (name+".snbt")
        text = file.read_text("utf8")
        ch_id = CHAPTER_ID.search(text)
        title = TITLE.search(text)
        if not ch_id or not title:
            raise ValueError("Invalid chapter "+name)
        chapter_ids[ch_id[1]] = dict(name=name, order=num, group=group)
        clean = re.sub(r'^\d{2}\s*[·.]\s*', '',
                       re.sub(r'^&[0-9a-f]&l', '', title[1]))
        text = TITLE.sub('\ttitle: "&'+group["color"]+'&l'+
                         f'{num:02d} · '+clean+'"',text,count=1)
        text = re.sub(r'(?m)^\tgroup: "[^"]+"',
                      '\tgroup: "A11000000000000'+str(group["id"])+'"',text,count=1)
        text = re.sub(r'(?m)^\torder_index:\s*\d+',
                      '\torder_index: '+str(num),text,count=1)
        text = re.sub(r'tokyo_(?:early|mid|late)\.png|tokyo_stage_(?:survival|industry|magic|endgame)\.png',
                      'tokyo_stage_'+group["art"]+'.png',text)
        pos = text.find("\tquests: [")
        if pos < 0:
            raise ValueError("Missing quests: "+name)
        header, body = text[:pos],text[pos:]
        # FTB image coordinates specify the centre, so x=8 aligns it with nodes.
        # FTB chapters use both compact one-line and multi-line image tags.
        # Only replace coordinates inside the banner image block, not quest nodes.
        def center_banner(match):
            image = match[0]
            image = re.sub(r'(?<![a-z])x:\s*-?[\d.]+d', 'x: 8.0d', image, count=1)
            image = re.sub(r'(?<![a-z])y:\s*-?[\d.]+d', 'y: -6.5d', image, count=1)
            image = re.sub(r'width:\s*[\d.]+d', 'width: 24.0d', image, count=1)
            image = re.sub(r'height:\s*[\d.]+d', 'height: 7.8d', image, count=1)
            return image
        header = re.sub(r'images:\s*\[[^\]]*\]', center_banner, header, count=1)
        text=header+body
        count = len(QUEST_ID.findall(text))
        if not count:
            raise ValueError("Empty quest tree "+name)
        local = [0]
        def transform(match):
            local[0]+=1
            qid=match[1]
            if qid in quest_ids:
                raise ValueError("Duplicate quest ID "+qid)
            quest_ids.add(qid)
            block=match[0]
            block = re.sub(r'(?m)^(\t\t\tx:\s*)([-\d.]+)d',
                           lambda m: m[1]+f'{8+(float(m[2])-8)*0.84:.2f}d',
                           block,count=1)
            block = re.sub(r'(?m)^(\t\t\ty:\s*)([-\d.]+)d',
                           lambda m: m[1]+f'{float(m[2])*0.78:.2f}d',
                           block,count=1)
            reward='rewards: [{ id: "A17'+qid[3:]+'" type: "random" table_id: '+str(TABLE_ID[group["art"]])+'L }'
            if local[0] == count:
                reward+=', { id: "A18'+qid[3:]+'" type: "item" item: "minecraft:experience_bottle" count: '+("10" if group["art"]=="endgame" else "6")+' }'
            reward+="]"
            return replace_reward_array(block,reward)
        text=QUEST_BLOCK.sub(transform,text)
        if local[0]!=count:
            raise ValueError("Quest count mismatch in "+name)
        file.write_text(text,"utf8")

group_headers='{\n\tchapter_groups: [\n'
for group in GROUPS:
    group_headers+=('\t\t{ id: "A11000000000000'+str(group["id"])+
                    '" title: "&'+group["color"]+'&l━━ '+group["name"]+' ━━" }\n')
group_headers+='\t]\n}\n'
(ROOT/"chapter_groups.snbt").write_text(group_headers,"utf8")

for locale in ("pl_pl","en_us"):
    location=ROOT/"lang"/(locale+".snbt")
    text=location.read_text("utf8")
    text=re.sub(r'(?m)^\s*chapter_group\.A11[0-9A-F]{13}\.title:[^\n]+\n','',text)
    rows='\n'.join(
        '\tchapter_group.A11000000000000'+str(g["id"])+'.title: "&'+
        g["color"]+'&l━━ '+g["name"]+' ━━"' for g in GROUPS)
    text=re.sub(r'^\{\s*\n','{\n'+rows+'\n',text,count=1)
    hits=[0]
    pattern=re.compile(r'(?m)^(\s*chapter\.(A12[0-9A-F]{13})\.title:\s*")([^"]+)(")')
    def update_title(match):
        ch=chapter_ids.get(match[2])
        if not ch:
            raise ValueError("Unknown translated chapter "+match[2])
        hits[0]+=1
        title=re.sub(r'^\d{2}\s*[·.]\s*','',re.sub(r'^&[0-9a-f]&l','',match[3]))
        return match[1]+'&'+ch["group"]["color"]+'&l'+f'{ch["order"]:02d} · '+title+match[4]
    text=pattern.sub(update_title,text)
    if hits[0]!=41:
        raise ValueError("Expected 41 translations in "+locale+", got "+str(hits[0]))
    location.write_text(text,"utf8")

reward_dir=ROOT/"reward_tables"
reward_dir.mkdir(exist_ok=True)
for stage_ix,(table_id,name,title,items) in enumerate(TABLES):
    lines=['{', '\tfilename: "tokyo_'+name+'"', '\tid: "'+table_id+'"', '\tloot_size: 1',
           '\torder_index: '+str(stage_ix),
           '\ttitle: "TOKYO WORLD — '+title+'"','\tuse_title: true','\trewards: [']
    for i,(item,qty,weight) in enumerate(items):
        entry_id=f'{stage_ix+1:X}'+'1'+f'{i+1:014X}'
        lines.append('\t\t{ id: "'+entry_id+'" item: { id: "minecraft:'+
                     item+'" count: 1 } count: '+str(qty)+
                     ' weight: '+str(weight)+'.0f }')
    lines+=['\t]','}']
    (reward_dir/('tokyo_'+name+'.snbt')).write_text('\n'.join(lines)+'\n','utf8')
audit=['# Tokyo World 1.8: Four-stage Questbook','',
       'Chapters: 41. Quests with bounded random rolls: '+str(len(quest_ids))+'.','']
audit+=['- '+g["name"]+': '+str(len(g["chapters"]))+
        ' chapters; stage banner tokyo_stage_'+g["art"]+'.png' for g in GROUPS]
audit+=['','Chapter/quest IDs, tasks and dependencies remain stable.',
        'One small weighted resource reward per quest; chapter final also gives XP bottles.',
        'No netherite, elytra, nether stars, finished machines or creative items.',
        'Stage banners centred at x=8, y=-6.5; tighter quest tree layout.']
Path("docs/TOKYO_WORLD_QUESTBOOK_V18.md").write_text('\n'.join(audit)+'\n',"utf8")
print("SUCCESS: 41 chapters, "+str(len(quest_ids))+" quests, 4 safe random tables")
