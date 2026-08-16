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
| 清道夫限时胜后整局直接「舞台落幕」 | `MarkNonCombatNodeExit` + 二次 `EndBattle`→vanilla FinalEnd（尚无下一波） | **已修** `MarkForcedTimedCombatVictory` + 重复 EndBattle 守卫 |
| 图鉴异想体书页贴图错乱 / 与战斗内图标不一致 | 图鉴用 `info.Artwork`（模组 `Creature_*` CG，且 bossbird/snowwhite 复用错图）；楼层用错误的 tier→楼层映射 | **已修** `Resources.Load(Sprites/CreatureArtworks/…)` + `GetFloorForScript`；`UIEmotionPassiveCardInven` 不再盲写空贴图 |
| 续玩后升级书页要重新塞进牌组 | 升级换牌只 `slot.id == oldId`；牌组先于库存加载；旧档牌组仍存基础 ID | **已修** `NormalizeCardKey` 替换 + `ReconcileDecksWithOwnedUpgrades`；升级后立刻 `SavePlayData_Menu` |
| 双方都活却进结算 | 情感 E.G.O. glitch | **已修** recovery |
| SelectOne 异想体书页（巨目等）可选多人、全队生效后卡住 | `RMR_ItemCatalog` 坏档（`File.Create` 截断）→ `AddToObtainCount` 抛异常打断 `OnClickTargetUnit`；叠加无参 `OnPickUp` 全队 | **已修** 原子 `SaveData` + Load 隔离 + 选页 try/catch + 无参 OnPickUp 置空；静态检查 `RMR_0807_save_atomic_emotion_softlock_static_check.ps1` |
| 宗教层解放战「打得好好的突然失败」 | Hokma 整场 `!IsStageFinishable` → 任意 `EndBattle` 都走「多阶段转发原版」；真实团灭未清理 / 误触发会直接拆场 | **已修** `StageController_EndBattle` 按存活人数分支：团灭=败北清理；双方都活=忽略；仅敌方空=波次转发 |
| 白夜第三阶段全员混乱后被终结攻击团灭、没有获得「忏悔」 | 图鉴临时编队五人均为 `isSephirah=false`；原版白夜只给司书长 100 层保护并发放 `9909999` | **已修（待游戏内验）** 解放战索引 0/中间角色临时绑定当前楼层司书长身份，战后恢复路线身份；静态检查 `RMR_0814_realization_sephirah_identity_static_check.ps1` |
| 情感 V 但只有 3 张异想体生效 | `PickEmotion` 打开异想体时就 Arm 中段 E.G.O.，`EmotionChoice` 下一轮在 `IsEnabled` 未就绪时用 E.G.O. 盖掉 4/5 异想体 | **已修** 改为异想体选完后再 `ArmMidBattleEgoAfterEmotionIfNeeded` |
| Tiphereth / Hokma 解放战卡 1 HP、阶段不推进 | `IsMultiphasePassiveType` 过宽：凡 `_currentPhase` 即挡 Die（误伤 Despair `505211`）；使徒本应可 Die | **已修（待游戏内验）** Die 白名单仅 `105010`/`205010` + 使徒 never-block；探针 `[RMR DieProbe]` |
| 杂质 Ensemble Argalia 卡 1 HP 战不结束 | CheckStage `IsLiveCombatBothSidesAlive` 吞掉 ManagerScript 的 EndBattle | **已修（待游戏内验）** `70020`/`70021` / TwistedReverberationBand / BlackSilence / `specialBattleEnding` 放行 |
| Desiccant（8570029）杂质战斗无效 | ResistAllUp 只写 `equipeffect`；读档/`SetOriginalResists` 后未重应用 | **已修（待游戏内验）** `ReapplyAllPlayerStatAdders` 于续玩加载与开战 CreateLibrarian |
| 解放/中段楼层 EGO 用一次后永久 CD | `CanUsingEgo` 要求楼层 Level≥6；`CreateRmrEmotionCoins` 不刷 `SpecialCardListModel` | **已修（待游戏内验）** `AddFloorEgoCoolTimeForRmr` / `EnsureFloorEgoCoolTimeProgressAfterVanilla` |
| 杂质双 Boss 看起来像随机 / 标签不清 | `70020`+`70021` 同池；早期也可能抽到 Boss；UI 统称 Boss | **已修** `curChStageStep<3` 不给 Boss；节点名区分漆黑噤默 / 残响乐团 |
| The Head 无法战斗 | Stage_ch7 无 Head 节点 | **Intended gap（暂不支持）** — 见下文 Impuritas 说明 |
| Grade5（都市恶疾/梦魇）奖励池缺 1阶收尾人东焕之页 | `EquipReward_ch5.xml` 只列了同组 5 页中的 `243001-243004`（但丁/Seven协会3科/金笠/剑契成员），漏了 `243005`（东焕）；三语 Localize 均已有文本，纯遗漏 | **已修（Wave5）** 补 `<RewardList ID="243005" ... Rarity="Uncommon"/>`，走既有 `GetBookDataOriginAware` 通用 EquipPage 奖励路径，无需新代码 |

### Impuritas / 解锁备注（2026-08-08）

- **Head**：当前不支持，非半成品战。
- **Olivier / Hana**：`Stage_ch7` 仅有 `70001–70010` Normal + 双 Boss；未单独加 Olivier/Hana 接待（避免无敌人 XML 的半接入）。
- **Red Mist → Binah**：`UnlockBinahAfterRedMistVictory` + 帮助文案 intentional（殷红迷雾胜后记录 Binah）。
- **Roland**：杂质 `70020` 首通记录漆黑噤默；帮助文案 intentional。
- **Realization EGO 奖励池**：`GetUnlockedRealizationEgoCardsForRewards` 已按「已解放楼层 + 章节 tier + 未拥有」过滤；多楼层解放后池变大属设计，非无门控洪水。
- **解放/中段楼层 EGO「用一次永久 CD」**：原版 `CreateEmotionCoin` 仅在 `CanUsingEgo()`（情感≥3 **且** Library 楼层 Level≥6）时给 `SpecialCardListModel.AddEgoCoolTime`；RMR 解放战/肉鸽楼层等级常 &lt;6，SpendCard 后 cool 停在 0、`CheckAddedEgoCard` 永不回手。另：`CreateRmrEmotionCoins` 原先只刷 `personalEgoDetail`，中段选进 `SpecialCardListModel` 的页也永不回冷。**已修** `AddFloorEgoCoolTimeForRmr` + `EnsureFloorEgoCoolTimeProgressAfterVanilla`；静态检查 `RMR_0808_floor_ego_cool_static_check.ps1`。

### Wave5 复核（异想体/书页解锁与奖池，2026-08-08）

独立复核确认以下均为**既有正确实现**，非 bug（未改动逻辑，仅记录证据，避免后续 agent 重新「修」一遍）：

- **殷红迷雾胜利同时记录 Roland + Binah？** 否。`GrantRedMistChallengeVictoryRewards()`（`RMR_AbnormalityUnlocks.cs`）只授予殷红迷雾核心页 `250022` + 战斗页 `607003-607007` + `UnlockBinahAfterRedMistVictory()`；Roland（漆黑噤默）走完全独立的 `RecordBlackSilenceVictoryUnlock()`，只在 `BlackSilenceStageId=70020`（`Stage_ch7.xml`，Grade7）触发。两条路径互不覆盖，且帮助文案（`RMR_HelpHandbookPanel.cs` 183-190 行）明确写明殷红迷雾胜利解锁 Binah，属设计意图。
- **都市之星（Grade6）入口门控是否正确？** 是。`EnsureGrade6SpecialCorePagesUnlocked()` 只在 `OnClearBossWave` 的 `case ChapterGrade.Grade6` 与 `LoguePlayDataSaver.LoadPlayData`（`curchaptergrade >= Grade6` 时）调用；Roland/苍蓝残响是「Grade7 首通后，未来跑到 Grade6 起就补发」的追溯型永久解锁（`IsBlackSilenceUnlockedForUrbanStar`/`IsDistortedEnsembleUnlockedForUrbanStar` 读永久 flag，与当前路线无关），Binah 则是「Grade6 内殷红迷雾本场临时可用 + 首通后当前路线永久」（`BinahUnlockedForCurrentRoute`）。两套门控没有互相污染或漏挡的迹象。
- **Distorted Blade（Yan 专属战斗页 611001-611003）会不会混进普通掉落池？** 不会。全仓搜索确认它们只出现在 `AddData/CardDropTable/CardDropTable_exclusives.xml` 的 `-999999`（显式排除表，静态检查 `RMR_0729_special_unlock_and_redmist_upgrade_static_check.ps1` 保护）与 Yan 自己的 `EquipPage_Librarian_ch6.xml`/`Deck_enemy_ch6.xml`；`SpecialStaticInfo/RewardPassiveInfos/*` 与 `CardDropTable_ch6.xml`（都市之星奖励池）都不含这三个 ID。`LogueBookModels.GetCorePageExclusiveBattleCardIds()` 还会按角色书页 `EquipEffect.OnlyCard` 动态收集专属页并从存档/图鉴中裁剪，属通用防护，不是只硬编码 Yan 一人。

平衡项清单：[`06-balance-backlog.md`](06-balance-backlog.md)。

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
