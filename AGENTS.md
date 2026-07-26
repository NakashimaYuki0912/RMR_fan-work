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

### 0.3 历史事故（读过再改 UI）

多名 agent（含 **DeepSeek**）曾造成：

- 中文 **口口口 / □□□（tofu）**
- 中文 **糊到看不清**（伪粗体 + 错误 SDF 材质）
- Localize 文件 **编码污染**
- 改了源码 **未部署 / 用户仍测旧 DLL**

**铁律摘要**（细节见 handbook 01）：

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
| 商店 | `ShopBase.cs`、`ShopGoods_*` | 重叠、软锁、布局 |
| 神秘事件 | `MysteryBase.cs`、`MysteryModel_*` | NRE、离开软锁 |
| 图鉴 / 存档 | `LogCompendiumPanel.cs`、`LogueBookModels.cs`（Compendium* API） | 永久 vs 路线混淆；磁盘键仍可能含 atlas |
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
- 存档字段：默认值 + 迁移 + 损坏回退。
- 空集合短路正确，避免 NRE。
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

## 10. 2026-07-12 交接快照（随 HANDOFF 更新）

| 项 | 状态 |
|---|---|
| 最新源码 Build 戳 | `2026-07-12Tfix-cjk-sharp-sweep+08:00`（以 `RMR_Core.cs` 为准） |
| 已部署 Workshop | 是（以本机 `deploy_workshop` 与 DLL 时间为准） |
| Git | `main`，曾 commit `3291d33`；CJK 全扫可能仍有未提交改动——**以 `git status` 为准** |
| 情感层过滤 | 已修 |
| 商店/神秘软锁 | 已修 |
| Hub / 选层 / 图鉴字体 | 已修 |
| 空库存提示 | 已删（静默） |
| Steam 上传 | Agent **不能**代登；用户自行 Workshop 更新 |

详细修复表：`docs/agent-handbook/04-known-bugs-and-fixes.md`。

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
| `handoff.md` | 图鉴升级开关等**历史**任务（可能过时） |

---

*维护者：后续 agent 修改重大规则时，请同步更新本文件 §3–5 与 `docs/agent-handbook/`。*
