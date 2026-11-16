#!/usr/bin/env python3
"""Create 1200×390 quest banners in the Tokyo World main-menu visual language.

Original artwork, layered neon voxel scenery, cherry blossoms, crimson moon
and stage-specific landmarks. PNG assets are committed by GitHub Actions and
used directly by Minecraft. It does NOT require players to rebuild their EXE.
"""
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from pathlib import Path
from random import Random
import math

W,H=1200,390
OUTPUT=Path("kubejs/assets/tokyoworld/textures/gui/quests")
OUTPUT.mkdir(parents=True,exist_ok=True)
fontpaths=[
 "/usr/share/fonts/truetype/dejavu/DejaVuSansCondensed-Bold.ttf",
 "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
 "C:/Windows/Fonts/arialbd.ttf"
]
def font(size):
    for f in fontpaths:
        if Path(f).is_file():
            return ImageFont.truetype(f,size)
    return ImageFont.load_default()

def blend(a,b,t):
    return tuple(int(x+(y-x)*t) for x,y in zip(a,b))

def render(kind,index,heading,sub,accent):
    random=Random(2026+index*313)
    im=Image.new("RGB",(W,H))
    px=im.load()
    top=(8,9,29); bottom=(63,31,85)
    for y in range(H):
        t=y/H
        base=blend(top,bottom,t)
        for x in range(W):
            glow=math.exp(-((x-945)**2/(2*270**2)+(y-100)**2/(2*140**2)))
            px[x,y]=(min(255,base[0]+int(78*glow)),
                     min(255,base[1]+int(13*glow)),
                     min(255,base[2]+int(31*glow)))
    d=ImageDraw.Draw(im,"RGBA")
    # The recognizable red sun, Fuji silhouette, distant floating islands.
    d.ellipse((838,-75,1190,278),fill=(244,46,105,235))
    for y in range(175,275,16):
        d.rectangle((837,y,1191,y+5),fill=(36,20,52,95))
    d.polygon([(1000,124),(890,286),(1109,286)],fill=(43,46,75,245))
    d.polygon([(1000,124),(966,185),(988,179),(1007,202),(1025,186),(1046,199)],
              fill=(226,231,250,230))
    for x,y in [(780,115),(1086,71),(945,234)]:
        d.polygon([(x-65,y),(x+45,y),(x+24,y+22),(x-43,y+20)],
                  fill=(30,28,58,250))
        d.line((x,y+23,x-4,y+98),fill=(57,207,252,110),width=3)
        for p in range(4):
            xx=x+random.randrange(-40,36)
            d.ellipse((xx,y-13,xx+23,y+7),fill=(85,167,119,245))
    # Low skyline with alternating neon windows.
    for x in range(620,W,31):
        topy=random.randrange(164,299)
        d.rectangle((x,topy,x+random.randrange(22,44),H),fill=(17,20,40,255))
        for yy in range(topy+13,H-8,15):
            for xx in range(x+7,x+27,12):
                if random.random()>.27:
                    d.rectangle((xx,yy,xx+3,yy+5),
                        fill=(37,239,235,150) if random.random()>.5 else (255,99,167,155))
    # Distinct scene silhouettes; all match the menu's tech-fantasy world.
    if kind=="survival":
        d.polygon([(708,341),(788,203),(872,341)],fill=(58,41,77,255))
        d.line((788,207,788,344),fill=(255,185,78,255),width=5)
        d.ellipse((754,281,816,351),fill=(241,135,57,220))
        for xx in range(700,826,22):
            d.line((xx,345,xx+8,304),fill=(88,224,128,250),width=7)
        d.rectangle((819,271,887,330),fill=(40,28,42,255))
        d.rectangle((839,248,863,274),fill=(250,171,70,240))
    if kind=="industry":
        for x,y,w,h in [(665,150,86,210),(777,115,100,260),(906,201,68,150)]:
            d.rectangle((x,y,x+w,y+h),fill=(34,33,59,255),outline=(63,228,239,220),width=3)
            for yy in range(y+18,min(H-14,y+h),37):
                d.rectangle((x+13,yy,x+w-12,yy+10),fill=(255,113,59,235))
        for x in (760,836,917):
            d.line((x,200,x+86,200),fill=(252,99,59,255),width=15)
        for x in range(680,1110,30):
            d.rectangle((x,346,x+17,362),fill=(70,237,246,210))
    if kind=="magic":
        for x,y in [(749,187),(910,203),(1044,250)]:
            for r in (41,25):
                d.ellipse((x-r,y-r,x+r,y+r),outline=(192,100,255,220),width=5)
            d.line((x-17,y,x+17,y),fill=(255,234,167,200),width=4)
            d.line((x,y-17,x,y+17),fill=(255,234,167,200),width=4)
        d.polygon([(760,340),(850,226),(950,340)],fill=(26,70,60,245))
        for y in (306,333):
            d.line((750,y,1080,y),fill=(98,245,190,220),width=7)
    if kind=="endgame":
        d.rectangle((955,169,987,362),fill=(217,218,234,255),
                    outline=(251,88,155,220),width=5)
        d.polygon([(952,177),(970,91),(990,177)],fill=(255,87,133,255))
        d.polygon([(954,339),(935,376),(969,364),(1003,376),(985,339)],
                  fill=(255,144,58,230))
        d.ellipse((776,90,853,164),outline=(159,100,255,230),width=8)
        d.arc((723,98,897,172),180,350,fill=(65,230,253,240),width=7)
        # Dragon shadow without detail loss when image is scaled.
        d.polygon([(707,164),(764,137),(802,170),(850,111),(855,171),
                   (920,124),(875,196),(813,205),(785,174)],fill=(29,22,57,245))
    # Cherry blossoms and ambient sparks — the recurring Tokyo identity.
    for i in range(94):
        x=random.randint(480,1199);y=random.randint(8,340)
        radius=random.randint(2,7)
        color=(255,125,190,random.randint(85,205))
        d.ellipse((x,y,x+radius,y+radius),fill=color)
    # Deliberately readable title plate, sharp crimson/cyan trim.
    d.rectangle((0,0,624,H),fill=(8,11,27,208))
    d.polygon([(0,0),(660,0),(580,H),(0,H)],fill=(12,14,31,190))
    d.rectangle((28,34,35,353),fill=accent)
    d.line((35,353,598,353),fill=accent,width=5)
    d.text((65,48),"TOKYO WORLD   /   STAGE "+str(index).zfill(2),
           font=font(23),fill=accent)
    d.text((62,125),heading,font=font(62),fill=(248,245,253,255))
    d.text((66,232),sub,font=font(19),fill=(223,225,246,255))
    d.text((66,278),"FORGE 1.20.1  •  EXPERT QUESTBOOK",
           font=font(16),fill=(171,180,215,240))
    # Neon edge line and right side glint.
    d.line((0,386,1199,386),fill=accent,width=4)
    target=OUTPUT/("tokyo_stage_"+kind+".png")
    im.save(target,optimize=True)
    print("Generated",target,round(target.stat().st_size/1024),"KiB")

render("survival",1,"SURVIVAL","PRAWIDZIWA PRZYGODA ZACZYNA SIE TUTAJ",(255,179,81,255))
render("industry",2,"INDUSTRY","STAL, ENERGIA I SAMODZIELNE FABRYKI",(72,221,239,255))
render("magic",3,"FARMING / MAGIC","ZIEMIA, MANA I SEKRETY NOWEGO SWIATA",(228,103,225,255))
render("endgame",4,"ENDGAME","KOSMOS, BOSSOWIE I DZIEDZICTWO TOKYO",(252,79,126,255))
