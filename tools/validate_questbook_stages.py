#!/usr/bin/env python3
"""Validate Tokyo World 1.8 four grouped quest stages and safe random rewards."""
import re
from pathlib import Path
from collections import defaultdict

ROOT = Path("config/ftbquests/quests")
STAGES = [
 ("START / SURVIVAL",4,"6","survival",[
  "tokyo_start","tokyo_farming","caves_adventure","tokyo_create",
  "storage_network","wave06_field_logistics","food_architecture"]),
 ("INDUSTRY / AUTOMATION",2,"b","industry",[
  "create_extended","logistics_optimization","tokyo_mekanism","powah_energy",
  "metallurgy","immersive_industry","wave06_immersive_industry","mekanism_industry",
  "enderio_industry","integrated_logic","ae2_networks","factory_logic",
  "logistics_pro","me_data_center","megawatt_network","chemical_superplant"]),
 ("FARMING / MAGIC / EXPANSION",5,"d","magic",[
  "automation_agriculture","industrial_farming","bees_advanced",
  "mystical_advanced","botania_magic","ars_magic","wave06_hex_and_blades",
  "mana_engineering"]),
 ("ENDGAME / SPACE / LEGACY",3,"c","endgame",[
  "steel_alloy_empire","mega_factory","cataclysm_bosses",
  "boss_hunting_contracts","wave06_alex_caves","space_industry",
  "wave06_space_frontier","orbital_outpost","draconic_endgame",
  "tokyo_ultimate_project"])
]
TABLES = {s: int(v, 16) for s,v in [
 ("survival","61A0000000000011"),
 ("industry","61A0000000000022"),
 ("magic","61A0000000000033"),
 ("endgame","61A0000000000044")
]}
expected = {}
order = 0
for stage,g,color,art,chapters in STAGES:
    for chapter in chapters:
        order += 1
        assert chapter not in expected, chapter
        expected[chapter] = (stage,g,color,art,order)
assert order == 41
actual={p.stem:p for p in (ROOT/"chapters").glob("*.snbt")}
assert set(actual)==set(expected), (set(actual)-set(expected),set(expected)-set(actual))

chapter_map={}
quest_ids=set()
reward_count=0
for name,(stage,g,color,art,number) in expected.items():
    content=actual[name].read_text("utf8")
    header=content.split("quests: [",1)[0]
    def val(prop):
        m=re.search(r'(?m)^\s*'+prop+r':\s*"([^"]+)"',header)
        assert m,(name,prop)
        return m[1]
    assert val("group")==f"A11000000000000{g}",name
    assert val("title").startswith(f"&{color}&l{number:02d} · "),name
    chapid=val("id")
    assert chapid not in chapter_map,chapid
    chapter_map[chapid]=(name,color,number)
    m=re.search(r'(?m)^\s*order_index:\s*(\d+)',header)
    assert m and int(m[1])==number,name
    assert "tokyo_stage_"+art+".png" in header,name
    assert re.search(r'(?m)^\s*x:\s*8\.0d',header),name
    assert re.search(r'(?m)^\s*y:\s*-6\.5d',header),name
    qids=re.findall(r'(?m)^\s*id:\s*"(A13[0-9A-F]{13})"',content)
    assert qids,name
    for qid in qids:
        assert qid not in quest_ids,qid
        quest_ids.add(qid)
        assert ('id: "A17'+qid[3:]+'"') in content,(name,qid)
    num_rew=len(re.findall(r'type:\s*"random"\s+table_id:\s*'+str(TABLES[art])+r'L',content))
    assert num_rew==len(qids),(name,num_rew,len(qids))
    reward_count+=num_rew

group_str=(ROOT/"chapter_groups.snbt").read_text("utf8")
wanted=[f"A11000000000000{g}" for _,g,_,_,_ in STAGES]
assert re.findall(r'\bid:\s*"(A11000000000000[0-9])"',group_str)==wanted
for stage,g,color,art,chapters in STAGES:
    assert f'title: "&{color}&l━━ {stage} ━━"' in group_str
    banner=Path("kubejs/assets/tokyoworld/textures/gui/quests/tokyo_stage_"+art+".png")
    assert banner.is_file() and banner.stat().st_size>8000,banner
    with banner.open("rb") as fp:
        assert fp.read(8)==b"\x89PNG\r\n\x1a\n",banner

for locale in ["pl_pl","en_us"]:
    content=(ROOT/"lang"/(locale+".snbt")).read_text("utf8")
    assert re.findall(r'chapter_group\.(A11000000000000[0-9])\.title:',content)==wanted,locale
    titles=dict(re.findall(r'chapter\.(A12[0-9A-F]{13})\.title:\s*"([^"]+)"',content))
    assert set(titles)==set(chapter_map),(locale,set(chapter_map)-set(titles))
    for chapter_id,(name,color,number) in chapter_map.items():
        assert titles[chapter_id].startswith(f"&{color}&l{number:02d} · "), (locale,name)

banned={"netherite_ingot","nether_star","elytra","dragon_egg",
        "beacon","command_block","totem_of_undying"}
for art,expected_table_id in TABLES.items():
    p=ROOT/"reward_tables"/("tokyo_"+art+".snbt")
    source=p.read_text("utf8")
    assert f'id: "{expected_table_id:016X}"' in source,art
    assert f'filename: "tokyo_{art}"' in source,art
    assert "loot_size: 1" in source,art
    rewards=re.findall(r'item:\s*\{\s*id:\s*"minecraft:([^"]+)"',source)
    assert len(rewards)>=10,(art,rewards)
    assert not banned.intersection(rewards),art
    counts=[int(x) for x in re.findall(r'\}\s+count:\s*(\d+)',source)]
    assert counts and all(1<=x<=32 for x in counts),(art,counts)

claims=Path("config/ftbchunks-world.snbt").read_text("utf8")
assert re.search(r'(?m)^\s*max_claimed_chunks:\s*128$',claims)
assert re.search(r'(?m)^\s*max_force_loaded_chunks:\s*4$',claims)
assert 'force_load_mode: "never"' in claims
assert reward_count==len(quest_ids),("random reward count",reward_count,len(quest_ids))
assert len(quest_ids)>=200,len(quest_ids)
print(f"OK: 41 chapters, {len(quest_ids)} quests grouped in four coherent stages")
print(f"OK: {reward_count} bounded random rewards, four tables, no OP equipment")
print("OK: stage art PNG, PL/EN translations, quest ID and FTB Chunks safety config")
