# LoR-RMR 会话交接（2026-08-03）

> 新会话请先读 [AGENTS.md](../AGENTS.md) 与 [agent-handbook/00-INDEX.md](./agent-handbook/00-INDEX.md)。  
> 本文是**快照**，以 `git status` 与 `RMR_Core.BuildTimestamp` 为准。

---

## 1. 工作区

```text
源码 / git:
  D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main\

本机实测部署（Player.log 实际加载的就是这个）:
  E:\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\RMR_REBORN_LOCAL\

Workshop 部署 / 上传源:
  E:\Steam\steamapps\workshop\content\1256670\3743867841\

Player.log:
  %USERPROFILE%\AppData\LocalLow\Project Moon\LibraryOfRuina\Player.log
```

- 外层 `D:\VS_program\ruina-roguelike-reborn-main\` **没有** `.git`。  
- 原作者对照：`D:\VS_program\ruina-roguelike-reborn-main\original-codes\`（只读）。
- 当前 GitHub `origin`：`https://github.com/NakashimaYuki0912/RMR_private.ver`（以 `git remote -v` 为准）。

**本机日常测试用 `deploy_local.ps1`，不是 `deploy_workshop.ps1`。** 两者 ModId 相同，
Mod 列表里只能启用一个，否则互相顶掉（玩家反馈「杂质章节不存在」多半就是这个原因）。

---

## 2. 当前状态

| 项 | 值 |
|---|---|
| Build 戳 | `2026-08-03Tgithub-release-cleanup+08:00` |
| 本地测试 DLL SHA256 | `443C99F93320B8C49F19120C035B6D1E6468563FF2C9FCFA835792616D5970A8` |
| Workshop | **已发布**（item 3743867841，2026-07-27，91.40 MB） |
| Workshop DLL SHA256 | `0D5BB90368339D83A4E056313605AA7882FAF9A102F44C233AA7FC57073B99FC`（本轮未上传） |
| GitHub | 当前发布目标为 `origin/main`；具体提交以 `git log -1` 为准 |
| 工作树 | 以 `git status --short` 为准 |

### 2026-08-03 发布审查摘要

- 解放战仍使用直接配置的临时图鉴编队；整队/单司书预设实验均已移除。
- 修复临时司书与准备界面的对象映射，避免核心页、副本壳和被动归属串位。
- 路线快照现在同时保存并恢复归属被动、被动供体占用和特殊固定牌组来源，避免解放战污染路线配置。
- 前四层配置上限为都市之星，后六层为杂质；候选只来自永久图鉴。
- 临时被动诊断探针与解析脚本已从 Release 源码中移除。
- Release、三语本地化、XML、Harmony 签名及相关专项静态检查已通过；新 Build 尚待重启游戏后用 `Player.log` 确认。

---

## 3. 本轮（2026-07-26 ~ 27）修复清单

按玩家反馈与实测排查，全部有日志或反编译实证。

### 存档

| 问题 | 根因 | 位置 |
|---|---|---|
| 继续游戏必损坏存档 | 读档抛异常 → `saveloading` 永久为 true → `CheckStage()` 恒真 → 库存 UI 被劫持为空 → 下次写盘固化 | `LoguePlayDataSaver.LoadPlayData` 加 try/finally + `LastLoadFailed` 锁存 |
| 写盘覆盖好存档 | 无守卫 | `ShouldRefuseSnapshotWrite`：读档失败或读档中 → 拒绝写入 |
| 第三方模组导致读档中断 | 6 处裸 `assem.GetTypes()` 遇 `ReflectionTypeLoadException` | 统一走 `LogLikeMod.GetLoadableTypes()` |
| 中段状态失败拖垮整局 | 无隔离 | 抽出 `LoadOptionalRunState`，保证 cardlist/booklist 一定执行 |
| **首次继续时司书牌组全丢** | 原版 `AddCardFromInventory` 查楼层携带上限，而读档时 `LibraryModel.GetFloor()` 返回 null → NRE | `LogLikePatches` 钩子：读档中直接 `AddCardFromInventoryToCurrentDeck`，跳过此时无法求值的上限检查 |

### 数据 / 玩法

| 问题 | 根因 |
|---|---|
| 异想体战斗节点从不出现 | `e998173` 误删占位 Stage `991103-991107` / `991001-991003`，节点图仍引用 → 开局被剔除 |
| 苍蓝残响奖励发不出 | `BlueReverberationCorePageId = 250013` 是 **TextId 不是 Book ID**，无书页匹配 → 整个解锁 return false。改为按 TextId 解析，优先 `260005`（玩家版） |
| Roland 解析靠枚举顺序 | 两本书 TextId 都是 102，改为显式优先 `id==102` |
| 楼层数显示 10 | 显示的是 `StageClassInfo.floorNum`（配置最大值）。本模组切换楼层仅改 BGM，改为显示 1。**不动字段**——`GetAvailableFloorList` 不读它，改字段只会影响消耗计数 |

### 显示 / 性能

| 问题 | 根因 |
|---|---|
| 中文发黏 | `ApplyTmpFontPreservingSharpMaterial` 无条件设 `extraPadding = true`，中文笔画密集时 SDF 边缘互渗。已恢复为 false |
| 启动 15 秒卡顿 | 美术预加载每帧 1 张（767 张≈13s），且**用全路径当缓存键**而消费方用文件名查 → 全部落空。改为文件名作键 + 每帧 8ms 预算 + 建立 `name→path` 索引。**15s → 6s** |
| 选完奖励卡顿 | `RepairActiveTmpFonts` 全场景 `FindObjectsOfType`，且 `PickEmotion`/`StartMystery` 在 UI 建好**之前**就扫。新增 `RepairTmpFontsUnder(root)` 局部修复 |
| 书页缩略图 | `BookModel.GetThumbSprite` 每张都全表 `Find` + `Resources.Load`，已加 skin→Sprite 缓存 |
| 悬停预览被埋 | boost 固定 250，而 `inventoryOrder` 由克隆画布累加得来可能更高。改为相对 `LogLikeMod.PrepareInventoryTopSortingOrder` |
| 分配窗口软锁 | 图层单向抬高从不还原 + ESC/返回键都被屏蔽。改为成对保存-还原，并给 ESC/返回键留逃生口 |

### 编码

- `RMR_AbnormalityUnlocks.cs` 12 处 GBK 往返 mojibake、`RMR_Core.cs` 23 处烧进文件的 U+FFFD，全部改为 ASCII。
- 全仓字节级 UTF-8 校验通过，仅 `TODO.md` 残留 1 处（用户笔记，未动）。

---

## 4. 未解决 / 待办

| 项 | 状态 |
|---|---|
| **切换语言后部分文本保持旧语言** | ❌ 未修。已加探针，需要**英文下**切一次语言的日志：搜 `lang check:` 与 `Refreshed vanilla abnormality text`，看 `sample ... expect=en OK/MISMATCH` |
| Localization Manager 冲突崩溃 | ❌ 未修。根因已定位：`TextDataModel.GetText` 阻断式 Prefix + MonoMod/Harmony 混用同一方法 |
| `Localize/en` 缺 71 键 | 内容工作项（`OrdealReward` 25、`OrdealText` 24 等） |
| 静态检查套件 | 49 个脚本中 41 个失败，均为 `6a201ac` 回退遗留（大量引用已改名的 `LogAtlasPanel.cs`）。**基线对比确认本轮改动新增失败 0 条** |

---

## 5. 打包上传（2026-07-27 修正后）

```powershell
cd "D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main"
powershell -ExecutionPolicy Bypass -File .\tools\packaging\deploy_workshop.ps1 -Configuration Release
powershell -ExecutionPolicy Bypass -File .\tools\packaging\_run_upload_YYYYMMDD.ps1
```

`_run_upload_*.ps1` 是每次发布的一次性包装（内含 changenote），调用
`upload_workshop_preserve_desc.ps1` → `prepare_workshop_upload.ps1` → steamcmd。

**steamcmd 登录用缓存凭据**（账号 `gffnj3`），本机当前无需输入密码；凭据失效时会转为交互提示，
那种情况 agent 无法代登，需人工执行。

### 本轮修掉的两个打包陷阱

1. **描述抓取带入页面 JavaScript** → steamcmd `KeyValues Error: got } in key` 直接拒绝。
   已在 `Convert-SteamDescHtml` 里按 `$J(` / `InitializeCommentThread` / `contributors` 截断，
   并回退到最后一个合法 BBCode 闭合标签。
2. **上传前无校验** → 登录一圈后才失败。新增 `Assert-VdfParses`。
   ⚠ 关键：Steam 的 KeyValues **不认 `\"` 转义**，裸引号即终止字符串。校验器必须照此实现，
   否则会放行 steamcmd 拒绝的文件（第一版就犯了这个错，用 steamcmd 判定为坏的文件反测才发现）。
3. `prepare_workshop_upload.ps1` 排除规则补上 `*.old`（否则 `RogueLike Mod Reborn.dll.old`
   会作为第二个 1.4 MB 程序集发给订阅者）。

---

## 6. 验证命令

```powershell
cd "D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main"
git status -sb
$log = "$env:USERPROFILE\AppData\LocalLow\Project Moon\LibraryOfRuina\Player.log"
Select-String -Path $log -Pattern "Build:|RMR Save|failed to restore|lang check:|PreLoad ArtWork|AbnoRoute"
```

---

## 7. 排查方法备忘

本轮最有效的手段，后续沿用：

- **Mono.Cecil 反编译原版**（`dependencies/Mono.Cecil.dll` 可用）比查维基可靠得多。
  苍蓝残响的 TextId/BookID 之分、楼层数三个 API 的区别、`AddCardFromInventory` 的
  null 来源，都是这样定位的。
- **游戏本体 `Managed/BaseMod/StaticInfo` 与 `Localize`** 是权威数据源。
- **日志探针**（照抄 `ReloadVanillaBossBirdTextForLanguage` 的健康检查模式）比反复猜快。
- 声称修好之前**先拿已知坏样本反测**——校验器那次就是没反测差点交付无效品。

---

*更新本文时请改日期，并同步 AGENTS.md §10。*
