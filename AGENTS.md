# LoR-RMR 项目指令（AGENTS.md）

本文件是 **Library of Ruina — Roguelike Mod Reborn（LoR-RMR）** 仓库的长期项目提示词。  
任何 Codex / Claude / Cursor / 其他 agent 打开本仓库后 **必须优先遵守本文件**；不要反复 `/init` 覆盖它。

> 本文件记录设计约束、禁区与工作方法，**不保证**工作树已 100% 实现每一句。  
> 动手前必须以 **当前源码 + git 状态 + 构建 + Player.log Build 戳** 重新核验。

---

## 0. 新 Agent 启动清单（强制，约 5 分钟）

### 0.1 读这些

| 顺序 | 路径 | 内容 |
|:---:|---|---|
| 1 | [docs/agent-handbook/00-INDEX.md](docs/agent-handbook/00-INDEX.md) | 手册地图 |
| 2 | [docs/agent-handbook/01-localization-fonts.md](docs/agent-handbook/01-localization-fonts.md) | **防 口口口 / 糊字（血泪史）** |
| 2b | [docs/localization/README.md](docs/localization/README.md) | **译者 / 本地化总入口**（术语表、文件地图、EN 指南） |
| 3 | [docs/agent-handbook/05-forbidden-and-safe-patterns.md](docs/agent-handbook/05-forbidden-and-safe-patterns.md) | 禁止与安全写法 |
| 4 | [docs/agent-handbook/03-architecture-map.md](docs/agent-handbook/03-architecture-map.md) | 改哪个文件 |
| 5 | [docs/agent-handbook/04-known-bugs-and-fixes.md](docs/agent-handbook/04-known-bugs-and-fixes.md) | 勿重开已修洞 |
| 6 | [docs/agent-handbook/02-build-deploy-verify.md](docs/agent-handbook/02-build-deploy-verify.md) | 构建部署 |
| 7 | [docs/HANDOFF.md](docs/HANDOFF.md) | 当前会话交接快照 |
| 8 | 本文件其余章节 | 玩法规则与验证等级 |

### 0.2 确认目录

```text
✅ 真源码 / git 根:
   D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main\

❌ 外层不是 git 仓库:
   D:\VS_program\ruina-roguelike-reborn-main\

只读原作者对照:
   D:\VS_program\ruina-roguelike-reborn-main\original-codes\

游戏实际加载（Workshop）:
   E:\Steam\steamapps\workshop\content\1256670\3743867841\Assemblies\dlls\
   （作者自己的工坊物品；原作上游 3503523710 仅作对照，勿当默认部署目标）
```

启动时执行：

```powershell
cd "D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main"
git status --short
git log -5 --oneline
# 确认存在 RogueLike Mod Reborn.csproj 与 RMR_Core.cs
```

### 0.3 历史事故（读过再改 UI / 存档 / 选页）

多名 agent（含 **DeepSeek**）曾造成：

- 中文 **口口口 / □□□（tofu）**
- 中文 **糊到看不清**（伪粗体 + 错误 SDF 材质）
- Localize 文件 **编码污染**
- 改了源码 **未部署 / 用户仍测旧 DLL**
- **情感 SelectOne（巨目等）可选多人、全队生效后卡住**（坏档 + 选页副作用抛异常打断 UI）

**铁律摘要**（细节见 handbook 01 / 05）：

```csharp
// ✅ 唯一推荐
LogLikeMod.ApplyTmpFontPreservingSharpMaterial(tmp, LogLikeMod.DefFont_TMP);
tmp.fontStyle = FontStyles.Normal; // CJK 不要 Bold

// ❌ 禁止
tmp.font = LogLikeMod.DefFont_TMP;
tmp.fontStyle = FontStyles.Bold;
// ❌ 禁止 ANSI 保存 Localize；全部 UTF-8
```

中文主字体应为 **`NotoSansCJKsc-Regular SDF`**（`LocalizedFontSetter.cnFont_notoSansCJKsc`），不是 `font_NotoSans` 韩文路径。

存档 / 选页铁律摘要见 **§3.6**（完整禁区：[05-forbidden-and-safe-patterns.md](docs/agent-handbook/05-forbidden-and-safe-patterns.md) §16–18）。

---

## 1. 项目概览

LoR-RMR 把《Library of Ruina》改成按章节推进的 Roguelike：普通战 / 精英 / Boss / 商店 / 休息 / 神秘事件 / 异想体奖励 / 楼层解放战；永久图鉴与解放门控扩展内容。

### 1.1 四类运行输入（必须分开）

1. **C# DLL** — `RogueLike Mod Reborn.dll`（Harmony、流程、UI、存档）
2. **游戏数据** — `AddData/`、`SpecialStaticInfo/`
3. **本地化** — `Localize/{cn,en,kr}/`（**kr ≠ 日文**）  
   - 译者文档：`docs/localization/`（[GLOSSARY](docs/localization/GLOSSARY.md) 英文化名 **Compendium**＝图鉴，禁止玩家文案写 Atlas）  
   - UI 键对齐：`tools/localization/compare_ui_keys.ps1`；导出对照表：`export_ui_keys_csv.ps1`
4. **资源** — `ArtWork/`、`AssetBundle/`、`AudioClip/`、`Spine/`、`StoryInfo/`

| 功能域 | 关键文件 | 主要风险 |
|---|---|---|
| 初始化 / 章节 | `RMR_Core.cs`、`LogLikeMod.cs` | 黑屏、章节错位 |
| 开局 Hub | `RMR_StartHubPanel.cs` | 字体糊、入口门控 |
| 解放战 | `RMR_RealizationManager.cs`、`LogRealizationPanel.cs` | Stage 包语义、配置污染 |
| 异想体 / E.G.O. | `RMR_AbnormalityUnlocks.cs` | EmotionLevel 错、未解放进池 |
| 结算奖励 | `RewardingModel.cs`、`PickUpModel_*` | 队列死循环、误 EndBattle |
| 情感选页 / SelectOne | `LogLikePatches.StageLibraryFloorModel_OnPickPassiveCard`、`PickUpModel_RMRVanillaEmotion` | 连点多人、全队叠效、UI 软锁 |
| 商店 | `ShopBase.cs`、`ShopGoods_*` | 重叠、软锁、布局 |
| 神秘事件 | `MysteryBase.cs`、`MysteryModel_*` | NRE、离开软锁 |
| 图鉴 / 存档 | `LogCompendiumPanel.cs`、`LogueBookModels.cs`、`LogueSaveManager.cs` | 永久 vs 路线混淆；坏档 / 截断写盘 |
| Harmony | `abcdcode_Refactored/LogLikePatches.cs` | 补丁冲突 |
| 字体 / 本地化 | `LogLikeMod.cs`、`ModdingUtils.cs` | 口口口、糊字 |

始终区分：**源码意图 / 工作树现状 / Workshop 已加载内容 / 游戏内实测**。

---

## 2. 工作方法

1. 说明既有规则、根因假设、拟改文件、验证方法（中文）。
2. **最小范围**修改；不碰用户无关改动。
3. 用户明确要求优先于本文旧句；冲突时指出，不静默改规则。
4. 完成后标明验证等级（§8），禁止未实测却写「彻底修好」。
5. 每次给用户试玩的构建：更新 `RMR_Core.BuildTimestamp` → 部署 → 对 `Player.log` 的 `Build:`。

---

## 3. 不可回退的玩法规则

### 3.1 章节与路线

- 第一章、第二章 **不得**生成异想体战斗节点；原节点改 `Rest`。
- 开场动画按当前 `ChapterGrade`，禁止锁死第一章。
- 玩家所选楼层音乐应持续；后幕不得自动回总类层。
- 普通战 / 商店异想体池：本章及更早 + 去重 + 未拥有 + 解放门控。
- 都市传说 / 恶疾 Normal 战异想体掉落目标约 50%；精英 / 异想体战不走该降率。
- Debug 章节直达必须同步章节等级 / 步数 / 起点，禁止「只生成一次节点又回传闻」。

### 3.2 永久图鉴与当前路线

- 角色书页、战斗书页：永久图鉴；路线装备 / 牌组同步；兼容旧档。
- **普通异想体书页**也写入永久图鉴（供解放战选页），但 **不等于**新开路线自动塞入持有。
- 解放战 **首次**胜利：专属异想体 + E.G.O. 入永久池；再战不重复发奖。
- 解放战临时编队用永久图鉴四类内容；战后 **必须**恢复路线配置。
- 图鉴 UI：异想体 / E.G.O. **不按**都市章节分段；角色 / 战斗书页可分段。

### 3.3 解放战入口与 Stage

- **唯一入口**：开局 Hub「挑战解放战」（`RMRStartHubPanel`）。
- 「正常游玩」后本局关闭解放入口；放弃进度重开后可再挑战。
- 已通关楼层可再战练手；不重复入队奖励。
- 必须进 **原版**楼层解放 Stage，ID **不得**错误附加模组 WorkshopId：

| 楼层 | Sephirah | Stage ID |
|---|---|---:|
| 历史 | Malkuth | 201005 |
| 科技 | Yesod | 202005 |
| 文学 | Hod | 203005 |
| 艺术 | Netzach | 204005 |
| 自然 | Tiphereth | 205005 |
| 语言 | Gebura | 206005 |
| 社会 | Chesed | 207005 |
| 哲学 | Binah | 208004 |
| 宗教 | Hokma | 209004 |
| 总类 | Keter | 210009 |

### 3.4 解放战奖励与 EmotionLevel

- 专属奖励仅首胜后进入商店 / 结算候选；仍去重、未拥有、升级过滤。
- E.G.O. ID 段见历史表（910001–910005 历史层 … 910086–910090 总类层）。
- 科技层终局三选一（**EmotionLevel 3**，队伍情感 **5**）：**音乐**、**棺柩**、**黑炎**  
  (`SingingMachine1` / `Butterfly3` / `freischutz3`)。
- **中段情感选书**（非解放奖励池）规则：

| 队伍情感 | 页阶 EmotionLevel |
|:---:|:---:|
| 1–2 | 1 |
| 3–4 | 2 |
| 5 | 3 |

  实现：`GetRequiredAbnoTierForTeamEmotion` + `IsOwnedPageEligibleForTeamEmotion`。  
  模组 script 必须 `ModScriptToVanillaScript` 后查层；**未知 script 排除，禁止当 Tier I**。

#### 3.4.1 script 后缀数字 **不是** 页阶（误报高发区）

52 组异想体里有 **50 组**后缀顺序与 EmotionLevel 不一致。判定页阶只看 EmotionLevel，
**永远不要从脚本名末尾的数字推断**。

| 脚本 | 中文名 | EmotionLevel |
|---|---|:---:|
| `SingingMachine1` | 音乐 | **III** |
| `SingingMachine2` | **旋律** | **I** |
| `SingingMachine3` | 成瘾 | II |
| `Butterfly1` | 安息 | II |
| `Butterfly2` | **哀悼** | **I** |
| `Butterfly3` | 棺柩 | **III** |

因此：**情感 1–2 出现「旋律」「哀悼」「今日的表情」是正确行为，不是 bug。**
只有「音乐 / 棺柩 / 黑炎」这类 EmotionLevel 3 的页出现在低情感才是故障。

核对来源（按可信度）：

1. 运行期 `EmotionCardXmlList`（`RegisterEmotionCardTier` 直接覆盖静态种子，以游戏数据为准）
2. `SeedStaticVanillaEmotionTiers` 静态种子（加载前兜底）
3. `tools/_vanilla_emotion_level_map.txt` 参考表（150 条，已与静态种子核对：0 冲突 0 遗漏）

### 3.5 中段 E.G.O. 与手牌

- 仅 **已拥有** E.G.O. 可出现在中段选择。
- 禁止开局 bulk grant 淹没手牌导致只能 Pass。
- 禁止误触发 EndBattle / 清场（见 `IsLiveCombatBothSidesAlive` 等 guard）。
- 情感 1–5 **每次**先选异想体；3/4/5 在异想体**选完之后**再开中段 E.G.O.（`ArmMidBattleEgoAfterEmotionIfNeeded` 只能在 `OnEmotionPagePicked` 之后调用）。
- **禁止**在 `PickEmotion` / 打开异想体 UI 时就 Arm E.G.O.——会与 `EmotionChoice` 竞态，E.G.O. 盖掉异想体，出现「情感 V 但只有 3 张异想体生效」。

### 3.6 存档写入与情感选页软锁（不可回退）

> 2026-08-07 血泪史：SelectOne「巨目」选完一个司书后仍可继续选、作用全队并卡住。  
> 根因链：`File.Create(目标档)` 截断 → 崩溃留下空/全 0 的 `RMR_ItemCatalog` → 选页收尾 `AddToObtainCount` → `LoadData` 抛 `SerializationException` → `LevelUpUI.OnClickTargetUnit` 未跑完 → UI 不关。  
> 次因：`PickUpModel_RMRVanillaEmotion` 无参 `OnPickUp()` 曾对全队生效，与 SelectOne 叠打。

#### 3.6.1 磁盘存档（`LogueSave/*`）

- **唯一写入口**：`LogueSaveManager.SaveData` / `LoadData` / `RemoveData` / `AddToObtainCount`。
- **禁止**对最终路径直接 `File.Create` + `BinaryFormatter.Serialize`（会先截断；进程死掉 → 空档）。
- `SaveData` 必须：**先写 `*.tmp` → 校验非空 → `File.Replace`（或等价原子替换）到目标名**。
- `LoadData` 必须：捕获反序列化失败 / 空文件 → **隔离**（`.corrupt_时间戳`）或删除 → 返回 `null`，**禁止抛到战斗 / 选页 UI**。
- `AddToObtainCount`、图鉴统计等 **非关键副作用**：失败只打日志，不得阻断商店购买、情感选页、奖励队列推进。
- 调用方读 catalog：`LoadData(...)` 后必须 **null 检查**再 `GetInt` / `GetData`（勿写 `LoadData(...).GetInt(...)` 裸链）。
- 磁盘目录：`%USERPROFILE%\AppData\LocalLow\Project Moon\LibraryOfRuina\LogueSave\`（键名如 `RMR_ItemCatalog`、`Lastest`、永久图鉴等 **勿擅自改名**）。

#### 3.6.2 情感 / 奖励选页收尾（`OnPickPassiveCard`）

- Hook：`LogLikePatches.StageLibraryFloorModel_OnPickPassiveCard`（替换原版路径时，抛异常 = 选人 UI 软锁）。
- **Apply 整段必须 try/catch**；catch 后仍应推进 `PassiveReward` / `RewardInStage` 队列（空集合短路）。
- 目标施加必须 **互斥**：
  - `All` / `AllIncludingEnemy` → 只走全队循环；
  - **否则** `target != null` → 只对该司书 `OnPickUp(target)`；
  - `SelectOne` 且 `target == null` → 打 Warning，**禁止**回落成全队。
- 禁止 `if (All) {…} if (target != null) {…}` 这种双分支叠打。

#### 3.6.3 原版情感 Pickup（`PickUpModel_RMRVanillaEmotion`）

- **无参** `OnPickUp()`：**必须为空**（Hook 在 SelectOne/All 施加之后总会再调一次）。
- 真正生效只在 `OnPickUp(BattleUnitModel model)`。
- 禁止在无参里 `GetList(Faction.Player)` 全队 `ApplyTo`——否则 SelectOne 双份图标 / 双战斗书页 /「全队都有」。

#### 3.6.4 改动后必跑静态检查

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\static_checks\runtime_release\RMR_0807_save_atomic_emotion_softlock_static_check.ps1
```

游戏内抽测：中段 / 结算出现 **SelectOne** 异想体书页（如巨目）→ 只点一名司书 → UI 关闭且战斗可继续；`Player.log` 无 `Binary stream '0' does not contain a valid BinaryHeader` 源于选页瞬间。

---

## 4. 商店、神秘与非战斗离开

- 战斗书页商品 = 获得种类，不显示库存递减。
- 分区布局：核心 / 战斗 / 被动 / 异想体 / E.G.O. / 升级；可紧凑，**禁止重叠**。
- 核心页常驻 2、异想体常驻 2；候选不足安全降级。
- 未解放楼层专属页不得进商店。
- 升级价初始 20，成功 +5，路线存档；删除旧卡加新卡。
- 升级选择按都市章节分类。
- 升级图标：`ArtWork/Shop_CardUpgrade_Icon.png`；无 Emoji 图标。
- **离开商店 / 神秘 / 休息**：必须 `RewardingModel.MarkNonCombatNodeExit`，清免疫 NPC，并在选完下一层后仍保持 pending，直到 `RoundStart` 清除——否则卡死在奇悭球体类软锁。
- **限时战斗胜利**（清道夫 `MysterySweeperTimer` / 三幕计时等）：用 `MarkForcedTimedCombatVictory`，**禁止** `MarkNonCombatNodeExit`。后者会跳过普通战入队奖励，且 `Die` 后再 `EndBattle` 易在未选下一层时走 vanilla FinalEnd（舞台落幕 / 整局结束）。重复 `EndBattle` 在奖励/nextlist 未清空时必须忽略。
- 商店坐标：`ShopBase.CardShape`（静态，改后需重启游戏）。

---

## 5. UI、本地化与字体（扩展）

完整规范：**[docs/agent-handbook/01-localization-fonts.md](docs/agent-handbook/01-localization-fonts.md)**。

摘要：

- 语言目录：`cn` / `en` / `kr`；缺键可 fallback，**禁止**用硬编码英文盖掉中文界面。
- 中文升级描述不得残留未本地化英文触发词（On Use 等），除非专有名词。
- 所有文本文件 **UTF-8**。
- C# 长期中文可用 `\uXXXX`。
- 自建 `TextMeshProUGUI`：**必须** `ApplyTmpFontPreservingSharpMaterial`；**禁止** CJK `FontStyles.Bold`。
- 工厂：`ModdingUtils.CreateText_TMP`（已接锐利路径）。
- 已对齐面板：Hub、解放选层、玩法介绍、图鉴。
- 空库存提示 `NotifyInventoryEmptyIfNeeded`：**静默**，勿恢复弹窗。

---

## 6. 编码与修改原则

- 4 空格；PascalCase 类型 / 公开成员；camelCase 局部变量。
- 修根因，禁魔法偏移堆叠。
- 无无关大重构、无整文件格式化。
- 存档字段：默认值 + 迁移 + 损坏回退；**写盘走 §3.6**，禁止旁路截断写。
- 空集合短路正确，避免 NRE；`LoadData` 结果一律当可能为 null。
- 战斗 / 选页 / 商店收尾路径上的 **非关键 I/O**（获得次数、统计、可选 UI）必须吞异常或走已加固 API。
- 高频日志必须开关；勿在 `Update` 无条件刷 log。
- 写权限失败时说明，不假装已写入。

---

## 7. 构建、部署、静态检查

详见 **[docs/agent-handbook/02-build-deploy-verify.md](docs/agent-handbook/02-build-deploy-verify.md)**。

```powershell
cd "D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main"
# 游戏必须退出
powershell -ExecutionPolicy Bypass -File .\tools\packaging\deploy_workshop.ps1 -Configuration Release
powershell -ExecutionPolicy Bypass -File .\tools\packaging\pack_mod.ps1   # 可选 zip
```

- 部署目标：`E:\Steam\...\3743867841\Assemblies\dlls\`（作者物品；上游原作为 3503523710）
- 日志：`%USERPROFILE%\AppData\LocalLow\Project Moon\LibraryOfRuina\Player.log`
- 搜索：`[RMR] RogueLike Mod Reborn initializing. Build:`
- 静态脚本：`tools/static_checks/{realization,rewards,shop_atlas,events_abnormality,runtime_release}/`
  - 改存档 / 情感选页时必跑：`runtime_release/RMR_0807_save_atomic_emotion_softlock_static_check.ps1`
- 扫描 XML 时 **排除** `.bak`；备份在 `Assemblies\_codex_backups\`
- 不要改 `_release_packages/` 代替源码
- **禁止**游戏运行中覆盖 DLL

### 原作者对照（§2.2 保留）

大故障时对比 `original-codes` 同名文件生命周期；**禁止整仓覆盖**；回归后重跑检查再部署。

---

## 8. 验证等级（汇报必写）

1. 源码检查  
2. 静态脚本  
3. Release 编译  
4. 部署 + Build 戳一致  
5. **游戏内实测**  

未达 5 禁止「已经彻底修好 / 游戏内验证无误」。

游戏内重点：

- Hub 中文清晰、无口口口  
- 解放战十层 Stage、配置恢复  
- 情感 1–2 不出现科技终局 III 页  
- **SelectOne 异想体书页只作用一名司书，选完 UI 关闭可继续**  
- 商店 / 神秘离开后进入真正下一场  
- 手牌可打出战斗页；中段 E.G.O. 仅已拥有  
- 图鉴 / 永久记录不污染路线  

---

## 9. 完成任务时的输出（中文）

- 根因与修法  
- 实际改动文件  
- 脚本 / 构建 / 解析结果  
- 是否部署、路径、哈希 / Build 戳  
- 未做的游戏内测试与残余风险  

---

## 10. 2026-07-27 交接快照（随 HANDOFF 更新）

| 项 | 状态 |
|---|---|
| 最新源码 Build 戳 | `2026-07-27Tcontinue-deck-restore-fix+08:00`（以 `RMR_Core.cs` 为准） |
| Workshop | **已发布** item 3743867841（2026-07-27，91.40 MB） |
| GitHub | 已推送，远端已迁移到 `NakashimaYuki0912/RMR_fan-work.git` |
| Git | `main`，工作树干净——**仍以 `git status` 为准** |
| 存档续玩 | **已修**：读档隔离 + 写盘守卫 + 牌组恢复跳过楼层上限检查 |
| 异想体战斗节点 | **已修**：补回 `e998173` 误删的占位 Stage |
| 苍蓝残响 / Roland 解锁 | **已修**：改为按 TextId 解析，不再硬编码 |
| 中文清晰度 | **已修**：移除强制 `extraPadding` |
| 启动 / 奖励界面卡顿 | **已修**：美术索引化、缩略图缓存、字体修复改局部（启动 15s→6s） |
| 源码编码污染 | **已清**：全仓字节级 UTF-8 校验通过 |
| 语言切换文本残留 | ❌ **未修**，已埋探针 |
| Localization Manager 冲突 | ❌ **未修**，根因已定位 |
| Steam 上传 | 本机 steamcmd 用**缓存凭据**，当前无需密码；凭据失效则需人工交互 |

详细修复表：`docs/agent-handbook/04-known-bugs-and-fixes.md`，完整交接见 `docs/HANDOFF.md`。

### 10.1 本机部署目标（易错）

**日常实测用 `deploy_local.ps1`**（→ `Mods\RMR_REBORN_LOCAL`），那才是 Player.log 实际加载的。
`deploy_workshop.ps1` 只在准备上传时用。两者 ModId 相同，Mod 列表里**只能启用一个**。

### 10.2 打包上传陷阱（2026-07-27 已修）

- 描述抓取会带入商店页评论区 JavaScript，导致 steamcmd `KeyValues Error: got } in key` 拒绝解析。
- Steam 的 KeyValues **不认 `\"` 转义**——裸引号即终止字符串。写 VDF 校验器必须照此实现。
- `prepare_workshop_upload.ps1` 需排除 `*.old`，否则旧 DLL 会一起发给订阅者。

---

## 11. 文档索引

| 文档 | 用途 |
|---|---|
| [README.md](README.md) | 人类总览、安装、agent 入口 |
| [docs/agent-handbook/00-INDEX.md](docs/agent-handbook/00-INDEX.md) | 手册入口 |
| [docs/HANDOFF.md](docs/HANDOFF.md) | 会话交接 |
| [RELEASE.md](RELEASE.md) | 旧版发布笔记（可能过时） |
| [LoR_modding_background.md](LoR_modding_background.md) | LoR 模组背景 |
| `RMR_abnormality_*.md` | 异想体设计草案 |
| `tools/_vanilla_emotion_level_map.txt` | 原版 EmotionLevel 参考 |
| `tools/static_checks/runtime_release/RMR_0807_save_atomic_emotion_softlock_static_check.ps1` | 存档原子写 + SelectOne 软锁防回归 |
| `handoff.md` | 图鉴升级开关等**历史**任务（可能过时） |

---

*维护者：后续 agent 修改重大规则时，请同步更新本文件 §3–6 与 `docs/agent-handbook/`（含 §3.6 存档/选页软锁）。*
