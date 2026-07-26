# 架构地图（改哪里）

## 1. 模块分层

```text
RMR_Core (ModInitializer)
  ├─ 初始化 / BuildTimestamp / 邀请启动后意图
  ├─ RMR_StartHubPanel          开局全屏菜单
  ├─ RMR_RealizationManager     解放战状态机
  ├─ RMR_RealizationLaunchHost  覆盖层 Canvas
  ├─ RMR_AbnormalityUnlocks     异想体/E.G.O. 门控与 EmotionLevel
  ├─ RMR_PrepareRestrictions    编队章节过滤、空库存提示(已静默)
  └─ abcdcode_LOGLIKE_MOD/*
        LogLikeMod              全局状态、字体、工具
        RewardingModel          战斗结算、中段情感/E.G.O.、非战斗离开标记
        ShopBase / ShopGoods_*  商店
        MysteryBase / MysteryModel_*  神秘事件
        LogAtlasPanel           图鉴
        LogRealizationPanel     解放战选层 UI
        LogueBookModels / Saver 存档与永久图鉴
  └─ abcdcode_Refactored/LogLikePatches.cs
        Harmony 补丁大全（StartBattle、EndBattle、UI、本地化钩子…）
```

约 **20** 个 `RMR_*.cs` 入口文件 + **~500** 个 `abcdcode_LOGLIKE_MOD` 文件。  
改功能前用 grep 追完整调用链，禁止「只改看起来相关的一个方法」。

---

## 2. 开局流程（2026-07）

```text
邀请书进入 RMR
  → RMRStartHubPanel（正常游玩 / 继续 / 解放战 / 玩法 / 图鉴 / 重置 / 退出）
  → 正常游玩：初始遗物 mystery → 路线
  → 解放战：LogRealizationPanel 选层 → 原版 Stage ID（无 mod package 污染）
  → 继续：读 Logue 存档
```

- 解放战入口 **仅** Hub「挑战解放战」  
- `CanEnterRealizationFromHub` 门控  
- 原版 Stage ID 表见 `AGENTS.md` §3.3  

---

## 3. 战斗中情感 / 异想体 / E.G.O.

### 情感书页（中段选择）

- 池：**本路线已获得**的异想体书页（`RouteUnlockedPages`），不是全图鉴  
- 过滤：`RMRAbnormalityUnlockManager.IsOwnedPageEligibleForTeamEmotion`  
- **队伍情感等级 → 原版 EmotionLevel（页阶）**

| 队伍情感 | 需要页阶 (EmotionLevel) | 例子 |
|:---:|:---:|---|
| 1–2 | I (1) | 今日的表情 `ShyLookToday1`、**旋律 `SingingMachine2`**、**哀悼 `Butterfly2`** |
| 3–4 | II (2) | 成瘾 `SingingMachine3`、安息 `Butterfly1` |
| 5 | III (3) | 科技终局：**音乐 `SingingMachine1`**、棺柩 `Butterfly3`、黑炎 `freischutz3` |

- 模组 script（`SingingMachine1`）必须映射到原版 script（`singingMachine`）再查 EmotionLevel  
- 未知页：**排除**（禁止再当 Tier I 倾倒）  
- 参考：`tools/_vanilla_emotion_level_map.txt`、`GetVanillaAbnoTierForScript`

> **后缀数字不是页阶。** 52 组异想体里 50 组的后缀顺序与 EmotionLevel 不一致
> （`SingingMachine1`=III 而 `SingingMachine2`=I；`Butterfly2`=I 而 `Butterfly3`=III）。
> 收到「低情感出现某某页」的反馈时，**先查这张页真实的 EmotionLevel**，不要看名字或后缀。
> 详见 `AGENTS.md` §3.4.1。

### 中段 E.G.O.

- 仅 **已拥有** E.G.O.  
- 禁止开局批量 `GrantOwnedEgo` 塞满手牌  
- 禁止中段误走原版 `OnPickEgo` 导致清场 / 误 EndBattle  
- 手牌默认不应被 E.G.O. 淹没导致只能 Pass  

相关：`RewardingModel`、`RMR_PrepareRestrictions`、`LogLikePatches` StartBattle/手牌  

---

## 4. 商店 / 神秘 / 休息（非战斗节点）

离开时：

1. `RewardingModel.MarkNonCombatNodeExit(reason)`  
2. 清场上残留敌方（免疫商人 NPC 如「奇悭球体」）  
3. `Wave.Defeat` + `EndBattle` → 奖励 / 下一层选择  

`NonCombatNodeExitPending` 在 **选完下一层** 后仍保持 true，直到真正 `RoundStart` 才 `ClearNonCombatNodeExit`。  

否则：`curstagetype` 已变 Normal，但免疫 NPC 仍在 → EndBattlePhase 误恢复战斗 → **软锁**。

调用点：

- `ShopBase.LeaveShop`  
- `MysteryBase` 结束选择  
- `MysteryModel_Rest.LeaveRest`  

商店布局：`ShopBase.CardShape` 静态坐标；战斗书页 Y 已下移（2026-07）。  
改布局后需 **重启游戏**（静态表首次构造填充）。

---

## 5. 奖励队列

- `RewardingModel` 驱动 `RewardFlag` 队列  
- 跳过 / 选完必须推进，禁止死循环  
- Boss / 解放奖励池见 `RMR_AbnormalityUnlocks` + 静态脚本 `tools/static_checks/rewards/`  

---

## 6. 存档

- 路线存档 vs 永久图鉴：见 `AGENTS.md` §3.2  
- `LoguePlayDataSaver` / `LogueBookModels`  
- 新增字段必须有默认值 + 旧档兼容  

路径（本机）：

```text
%USERPROFILE%\AppData\LocalLow\Project Moon\LibraryOfRuina\
  Player.log
  ModConfigs\RMR_Config.xml
  LogueSave\ ...
```

---

## 7. 数据目录

| 目录 | 内容 |
|---|---|
| `AddData/` | 卡牌、敌人、装备页、Stage 等 |
| `SpecialStaticInfo/StagesXmlInfos/` | 章节节点图 |
| `SpecialStaticInfo/MysteryXmlInfos/` | 神秘事件 |
| `SpecialStaticInfo/RewardPassiveInfos/` | 奖励被动/异想体定义 |
| `Localize/{cn,en,kr}/` | 本地化（**kr 不是日文**） |

XML 修改后：UTF-8 解析校验 + 部署同步到 Workshop `Assemblies\dlls\...`。

---

## 8. Harmony 补丁

几乎所有运行时钩子在：

`abcdcode_Refactored/LogLikePatches.cs`（大文件）

改 EndBattle / StartBattle / UIPhase 前：

- 搜索现有 guard（`IsLiveCombatBothSidesAlive`、`IsNonCombatNodeStage`、realization 中段）  
- 禁止再叠一个冲突的「一刀切 EndBattle」  

---

## 9. 原作者对照

```text
D:\VS_program\ruina-roguelike-reborn-main\original-codes\
```

只读。大故障时对比同名文件生命周期，**禁止整仓覆盖**。  
见 `AGENTS.md` §2.2。
