# 已知问题与已修复清单（2026-07 会话）

> 目的：后续 agent **不要重开已修的洞**，也不要把症状当新需求乱改。

---

## A. 本地化 / 字体

| 症状 | 根因 | 状态 | 关键代码 |
|---|---|---|---|
| 中文 口口口 | 错误字体 / Fallback / 编码 | 持续加固 | `LogLikeMod` 字体管线 |
| Hub 字糊 | Bold 伪粗体 + 材质 | **已修** | `RMR_StartHubPanel` |
| 解放战选层字糊 | 同上 | **已修** | `LogRealizationPanel` |
| 玩法介绍 / 图鉴糊 | 裸 `font=` | **已修** | `RMR_HelpHandbookPanel`、`LogAtlasPanel` |
| 商店/神秘按钮 | CreateText_TMP | **已走锐利路径** | `ModdingUtils` |
| 异想体名 触须类 口口口 | script 未映射 vanilla Name | **已修一批** | `ModScriptToVanillaScript` |
| 开场 PV / 编队名 tofu | OpeningLyrics 编码 + slot 字体 | **已修** | `LogLikePatches` / `LogLikeMod` |
| 「当前层限制：没有可显示的核心书页」 | 空库存弹窗 | **已静默** | `NotifyInventoryEmptyIfNeeded` |
| **全局中文糊（含原版 UI）** | RMR 把 **韩文** `NotoSerifCJKkr-Medium SDF` 写进 `cnFont_notoSansCJKsc` | **已修 2026-07-26** | `IsFaceRegionAcceptableForLanguage`、`PatchSetterFontField` |

### A.1 全局中文糊（2026-07-26，字形区域守卫）

**症状**：不只是 RMR 自建面板，**整个游戏**（原版接待、书页、选项）中文都糊。

**Player.log 实证**：

```text
[RMR Localize] LocalizedFontSetter.cnFont_notoSansCJKsc:
    '[Fallback_1]NotoSansCJKsc-Regular SDF' → 'NotoSerifCJKkr-Medium SDF' (lang=cn, force=False).
[RMR Localize] DefFont_TMP = 'NotoSerifCJKkr-Medium SDF' for lang=cn.
```

**根因（两个缺陷叠加）**：

1. **`[Fallback_N]` 前缀被当成坏字体。** 原版 `cnFont_notoSansCJKsc` 里存的就是
   `[Fallback_1]NotoSansCJKsc-Regular SDF` —— 那只是 TMP 给「同时挂在 fallback 表里的资源」加的
   运行时标签，资源本身是正牌 SC 字体。旧代码 `IsTmpFallbackFaceName` 一律否决 → 触发替换。
2. **语言兼容只查字形覆盖，不查字形区域。** Noto CJK 是泛 CJK：kr 面同样能回答
   `HasCharacter('图'/'汉'/'语')`，所以 `IsTmpFontCompatibleWithLanguage(kr面, "cn")` 返回 true。
   en 启动时解析出的 kr 面在切到 cn 后 **不会失效**，接着被盖进 cn 槽位。

结果：原版 `SetLocalizedFont(cn)` 把 **韩文 Serif Medium** 发给每一个中文 TMP —— 衬线 + 中等字重
在小字号 SDF 上就是「糊」。

**修法（`LogLikeMod.cs`）**：

| 新增 / 改动 | 作用 |
|---|---|
| `StripTmpFallbackPrefix` | 剥掉 `[Fallback_N]` 标签后再判断 |
| `GetFaceScriptRegion` | 面名 → `sc` / `kr` / `jp` / 未知 |
| `IsFaceRegionAcceptableForLanguage` | cn/trcn 只收 `sc`；kr 只收 `kr`；jp 只收 `jp`；en 不限 |
| `IsUsableLanguageFace` | 「忽略 Fallback 标签」的可用性判定，用于评价**原版已装好**的字体 |
| `PatchSetterFontField` 硬拦截 | 区域不符的面 **拒绝**写入语言槽位，并 `LogWarning` |
| `ScoreFace` 排序 | Sans/Regular 优先，Serif/Medium/Bold 降权 |
| `IsCachedDefFontStillGood` | 按 (面, 语言) 记忆化；语言一变就重解析 |

**注意**：`IsTmpFontCompatibleWithLanguage` 仍然否决 Fallback 名 —— 它用于**挑新主字体**时防止
命中局部 atlas（历史 口口口 来源）。评价原版既有槽位请用 `IsUsableLanguageFace`，**不要混用**。

**保底**：确实找不到 sc 面时，解析器仍会回退到 kr 面并告警，不会退化成 口口口。

---

## B. 情感 / 异想体 / E.G.O.

| 症状 | 根因 | 状态 |
|---|---|---|
| 情感 1 出现 **III 级页**（科技终局「音乐」等） | 模组 script 查不到 EmotionLevel → 当 Tier I | **已修**：别名 + 静态种子 + 未知排除 |
| 中段 E.G.O. 未拥有可选 | 池错误 | **已修**：仅 owned |
| 手牌全是 E.G.O. 只能 Pass | 开局 bulk grant / 默认手 | **已修** |
| 情感满自动胜利 / 清场 | 误 EndBattle / OnPickEgo | **已修** guard |
| SetCardsObject 越界 | sprite 数组短 | **已修** 安全路径 |

### 科技层终局三选一（EmotionLevel 3，队伍情感 5）

| 中文 | 模组/原版 script |
|---|---|
| **音乐** | `SingingMachine1` → `singingMachine` |
| 棺柩 | `Butterfly3` → `butterfly3` |
| 黑炎 | `freischutz3` |

### ⚠ 不要把这些误当成 bug（2026-07-26 核查）

同一异想体的其他页 **不是** III 级，出现在低情感是**正确**的：

| 脚本 | 中文名 | EmotionLevel | 正确出现时机 |
|---|---|:---:|---|
| `SingingMachine2` | **旋律** | I | 情感 1–2 |
| `SingingMachine3` | 成瘾 | II | 情感 3–4 |
| `Butterfly1` | 安息 | II | 情感 3–4 |
| `Butterfly2` | **哀悼** | I | 情感 1–2 |
| `ShyLookToday1` | 今日的表情 | I | 情感 1–2 |

**52 组异想体里 50 组的后缀数字与 EmotionLevel 不一致**，这是原版设定，不是映射错误。
本表上一版把「情感 1 出现旋律」记成已修 bug，属误记 —— 旋律本来就是 I 级页。
真正的故障形态只有一种：**III 级页出现在情感 1–4**。

**另注：** 原版 `bluestar3`（思念之声）英文名也可能译「旋律」，但那是 **EmotionLevel 2**，
与科技层终局无关。判定一律以 EmotionLevel 为准，不看译名。

核查方法：`tools/_vanilla_emotion_level_map.txt`（150 条）vs `SeedStaticVanillaEmotionTiers`
已比对，0 冲突 0 遗漏；运行期 `RegisterEmotionCardTier` 用游戏自身 `EmotionCardXmlList` 覆盖静态种子。
日志 `[RMRAbnormalityUnlockManager] Unknown abno EmotionLevel for script=` 若出现，才说明真有页解析失败。

---

## C. 流程软锁

| 症状 | 根因 | 状态 |
|---|---|---|
| 商店/神秘选完下一层，卡在本场，空格开战但商人免疫 | 残留 NPC + 类型已变 Normal + EndBattle 恢复 | **已修** `NonCombatNodeExitPending` |
| 双方都活却进结算 | 情感 E.G.O. glitch | **已修** recovery |

---

## D. 商店布局

| 症状 | 状态 |
|---|---|
| 战斗书页与中间物品空隙过大 | **已修** `ShopBase.CardShape` Y 下移 ~60，passive 偏移同步 |

---

## E. 仍可能残留 / 需游戏内复验

- 全语言 UI 是否仍有 **裸 `font=`** 的漏网自定义控件（改 UI 时再扫）  
- 极端分辨率下商店重叠  
- 图鉴「显示升级版战斗书页」开关（见根目录 `handoff.md` 历史需求）  
- 与第三方 UI 模组（如 BCEV）共存 NRE  
- Workshop 目录大量 `.bak` 若被错误扫描加载  

---

## F. 关键 script 别名（节选）

完整表：`RMR_AbnormalityUnlocks.ModScriptToVanillaScript`

| 模组 script | 原版 script |
|---|---|
| `ShyLookToday1/2/3` | `shylook` / `shylook2` / `shylook3` |
| `SingingMachine1/2/3` | `singingmachine`… |
| `UniverseZogak2` | `fragmentspace2`（触须类） |
| `HeartofAspiration3` | `doki` |
| `Mountain1/2/3` | `danggocreature*` |
| `Clock1/2/3` | `silence*` |

查层：`GetVanillaAbnoTierForScript` → `EnsureVanillaEmotionTiersLoaded` + 静态种子。

---

## G. 用户验收时请其重启

热重载无效。必须：

1. 完全退出 Library of Ruina  
2. 部署：本机实测走 `deploy_local.ps1`（目标 `LibraryOfRuina_Data\Mods\RMR_REBORN_LOCAL`，
   即 Player.log 里实际加载的那份）；要写 Steam Workshop 才用 `deploy_workshop.ps1`。
   **两个 RMR 条目不要同时启用。**
3. 启动后看 `Player.log` 的 `Build:`  
