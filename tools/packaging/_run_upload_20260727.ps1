# One-off wrapper: upload the 2026-07-27 build.
# The Steam page description is preserved (scraped from the live page and written back untouched);
# only the content and this changenote are updated.
#
# Usage (from anywhere):
#   powershell -ExecutionPolicy Bypass -File .\tools\packaging\_run_upload_20260727.ps1
#
# The upload tree was already prepared, so -SkipPrepare is passed. Drop that switch if you want the
# package rebuilt from the Workshop deploy folder first.
$note = @"
[h2]2026-07-27 Update[/h2]
[list]
[*][b]Continue Run no longer corrupts saves[/b]: restoring a run could throw partway through, which
left the inventory empty and then wrote that empty state back over the save file. Loading is now
isolated per step, and a snapshot is never written after a failed load.
[*][b]Librarian decks restore correctly on the first Continue[/b]: the deck restore ran a per-floor
carry-limit check before the library floors existed, so every librarian lost their deck unless you
exited and continued a second time.
[*][b]Abnormality Battle nodes now appear[/b]: the placeholder stages the route referenced were
missing from the data files, so every one of these nodes was silently dropped at run start.
[*][b]Blue Reverberation rewards now unlock[/b]: the core page was looked up by a localization id
instead of a book id, so it never resolved and neither the key page nor its combat pages were granted.
[*][b]Chinese text is sharper[/b]: extra glyph padding was being forced on every label, which made
dense CJK glyphs bleed at the edges.
[*][b]Faster loading and smoother reward screens[/b]: art assets are indexed once instead of being
re-scanned on every lookup, book thumbnails are cached, and the post-battle font pass no longer
sweeps the whole scene.
[*][b]Key Page hover preview and passive attribution popup layering[/b]: layer changes are now saved
and restored in pairs instead of being applied one-way, and ESC / the back button can always close
the attribution popup.
[*]Floor selection now shows 1 available floor: switching floors only changes the map theme and
music in this mod, it never consumes a deployment.
[/list]

[h2]2026-07-27 更新[/h2]
[list]
[*][b]继续游戏不再损坏存档[/b]：读档中途出错会导致库存为空，随后又把这个空状态写回存档文件。现在读档逐段隔离，且读档失败后绝不写盘。
[*][b]首次继续时司书牌组正确恢复[/b]：牌组恢复会在图书馆楼层尚未建立时执行楼层携带上限检查，导致必须退出再继续一次才能拿回牌组。
[*][b]异想体战斗节点现在会出现[/b]：路线引用的占位关卡在数据文件中缺失，导致这些节点在开局时被静默剔除。
[*][b]苍蓝残响奖励现在能解锁[/b]：核心书页此前按本地化 ID 而非书页 ID 查找，始终解析失败，角色书页与对应战斗书页都发不出来。
[*][b]中文显示更锐利[/b]：此前对所有文本强制附加了额外字形边距，导致笔画密集的中文边缘互相渗透。
[*][b]载入更快、奖励界面更流畅[/b]：美术资源改为建立索引而非每次查找都重扫目录，书页缩略图加入缓存，战后字体修复不再扫描整个场景。
[*][b]钥匙页悬停预览与被动分配窗口图层[/b]：图层调整改为成对保存与还原，不再是单向施加；ESC 与返回键现在总能关闭分配窗口。
[*]楼层选择现在显示可接待楼层为 1：本模组中切换楼层仅改变地图主题与背景音乐，不消耗接待次数。
[/list]
"@.Trim()

# Invoke in-process so the multiline changenote stays a single argument.
& "$PSScriptRoot\upload_workshop_preserve_desc.ps1" -ChangeNote $note -SkipPrepare
exit $LASTEXITCODE
