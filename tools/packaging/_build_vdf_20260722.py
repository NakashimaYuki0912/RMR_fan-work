# -*- coding: utf-8 -*-
# Build a clean VDF for item 3743867841 (2026-07-22 key page layer fix upload).
# Fixes two problems that broke steamcmd parsing:
#  1) preserved description contained scraped page JS noise ($J(...) / "3 Comments")
#  2) changenote written via PowerShell got mojibake for CJK text
import io

PKG = r"D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main\tools\packaging"
desc = io.open(PKG + r"\_preserved_description_3743867841.txt", "r", encoding="utf-8").read()

cut = desc.find("$J(")
if cut > 0:
    desc = desc[:cut]
desc = desc.rstrip().rstrip("\t").rstrip()
assert desc.endswith("[/list]"), "unexpected description tail: %r" % desc[-60:]
assert len(desc) > 1000, "description too short"

note = (
    "[h2]2026-07-22 Update[/h2]\n[list]\n"
    "[*][b]Key Page hover preview fix[/b]: in the battle-prep inventory, hovering a Key Page "
    "now correctly shows the enlarged preview panel on top "
    "(it was being painted over by the raised RMR inventory layer).\n"
    "[*]The preview layer boost is cleanly restored when the preview hides or when leaving "
    "the Key Page tab, so other UI ordering is unaffected.\n"
    "[/list]\n\n"
    "[h2]2026-07-22 更新[/h2]\n[list]\n"
    "[*][b]钥匙页悬停预览修复[/b]：战斗准备界面中，鼠标悬停钥匙页时放大预览面板现在会正确显示在最上层（此前被抬高的RMR库存图层遮挡）。\n"
    "[*]预览关闭或离开钥匙页页签时会还原图层设置，不影响其他界面层级。\n"
    "[/list]"
)

def esc(s):
    return (s.replace("\\", "\\\\").replace('"', '\\"')
             .replace("\r\n", "\\n").replace("\n", "\\n").replace("\r", "\\n")
             .replace("\t", "\\t"))

content_folder = r"E:\Steam\steamapps\workshop\content\1256670_BACKUPS\3743867841_upload"
preview = content_folder + r"\preview.jpg.png"
title = "RMR REBORN fan work [Workshop]"

vdf = (
    '"workshopitem"\n{\n'
    '\t"appid"\t\t"1256670"\n'
    '\t"publishedfileid"\t\t"3743867841"\n'
    '\t"contentfolder"\t\t"%s"\n' % esc(content_folder)
    + '\t"previewfile"\t\t"%s"\n' % esc(preview)
    + '\t"visibility"\t\t"0"\n'
    + '\t"title"\t\t"%s"\n' % esc(title)
    + '\t"description"\t\t"%s"\n' % esc(desc)
    + '\t"changenote"\t\t"%s"\n' % esc(note)
    + "}\n"
)

out = PKG + r"\workshop_item_3743867841.vdf"
io.open(out, "w", encoding="utf-8", newline="").write(vdf)
print("VDF written:", out)
print("desc chars:", len(desc), "| note chars:", len(note))
