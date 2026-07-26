# -*- coding: utf-8 -*-
# One-off analysis for local optimization pass (2026-07-22).
# Dumps an inventory of hardcoded CJK string literals in C# sources
# (targets for future localization replacement by collaborators).
import os, re, glob, io

ROOT = r"D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main"
DIRS = ["", "abcdcode_LOGLIKE_MOD", "abcdcode_Refactored",
        "abcdcode_LOGLIKE_MOD_Extension", "CommonModApi"]

cjk = re.compile(u'"(?:[^"\\\\\\n]|\\\\.)*[\u4e00-\u9fff\uac00-\ud7af](?:[^"\\\\\\n]|\\\\.)*"')

out = io.open(os.path.join(ROOT, "docs", "localization-hardcoded-strings.tsv"),
              "w", encoding="utf-8", newline="")
out.write("file\tline\tliteral\n")
count = 0
for d in DIRS:
    for f in sorted(glob.glob(os.path.join(ROOT, d, "*.cs"))):
        rel = os.path.relpath(f, ROOT)
        for i, line in enumerate(io.open(f, encoding="utf-8-sig", errors="replace"), 1):
            if line.lstrip().startswith("//"):
                continue  # comments are not localization targets
            for m in cjk.findall(line):
                out.write(u"%s\t%d\t%s\n" % (rel, i, m.strip()))
                count += 1
out.close()
print("wrote docs/localization-hardcoded-strings.tsv, rows:", count)
