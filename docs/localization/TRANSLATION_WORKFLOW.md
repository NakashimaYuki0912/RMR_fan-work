# RMR 翻译工作流与内容索引

这份文档面向翻译作者。目标是：**不用阅读 C#，也能先按内容类型找到正确的文本文件**。

适用语言目录：`Localize/cn/`、`Localize/en/`、`Localize/kr/`。`kr` 是韩语目录，**不是日语**；日语目前会回退到已有语言文件，新增日语目录须由开发者先接入加载逻辑。

开始前请阅读 [GLOSSARY.md](GLOSSARY.md)，英文翻译另见 [TRANSLATOR_GUIDE_EN.md](TRANSLATOR_GUIDE_EN.md)。

## 1. 先按“我要翻译什么”找文件

| 要修改的内容 | 首选位置 | 识别字段 | 不要改什么 |
|---|---|---|---|
| Hub、商店、休息、图鉴、确认弹窗、节点名称 | `UIs.txt` | `<text id="ui_RMR_*">`、`Stage_*`、`Shop_*` | `id` |
| 开局镜子、章节开场、分支选项 | `MysteryEvents/RMR_chstart.xml` | `<Mystery ID>`、`<Frame ID>`、`<Choice ID>` | 三类 ID 与分支结构 |
| 第 1–6 章随机事件 | `Mystery1.xml` … `Mystery6.txt`、`MysteryEvents/Loglike_Mystery_Ch_*.xml` | `<Dialog>`、`<Choice><Desc>` | Mystery / Frame / Choice ID |
| RMR 战斗书页名称与效果说明 | `CardInfo/RMR_CardInfo_*.xml` | `<BattleCardDesc ID>`、`<LocalizedName>`、`<Desc>` | ID、骰子/卡牌数据 |
| 既有章节或敌方战斗书页 | `CardInfo/CardInfo_ch*.txt`、`CardInfo_*` | `<BattleCardDesc ID>` | ID、文件名 |
| 卡牌“使用时/命中时”等能力文本 | `DiceAbilityInfo/*.txt`、`DiceAbilityInfo/RMR_SpecialCardAbility.xml` | `<DiceCardAbilityDesc ID>` 或能力 ID | ID 与 XML 标签 |
| 角色书页名称、故事、被动列表 | `BookInfo/*.txt` | `<BookDesc BookID>`、`<BookName>`、`<TextList>` | `BookID` |
| 被动名称与说明 | `PassiveInfo/*.txt`、`PassiveInfo/RMR_PassiveList_Special.xml` | `<PassiveDesc ID>`、`<Name>`、`<Desc>` | ID |
| 敌人/角色显示名称 | `EnemyNameInfo/*` | 单位名称 ID | ID 与文件名 |
| 异想体奖励名称、说明、拿取界面 | `CreaturePickUp.txt`、`CreaturePickUp_Table.xml` | `PickUpCreature_*`、奖励 ID | ID、奖励逻辑 |
| 全局遗物、商店/首胜奖励、制作效果 | `LogueEffectText/*.xml`、`GlobalEffect.xml`、`CraftEffect.txt` | `<Name>`、`<Desc>`、效果 ID | 效果 ID/脚本名 |
| 战斗状态、Buff、关键词说明 | `EffectTexts/RMR_bufs.xml`、`CustomBufs.xml` | 状态 ID、名称、说明 | ID/脚本名 |

`AddData/`、`SpecialStaticInfo/` 是数值、掉落表和事件/关卡逻辑，**不是普通翻译入口**。如果文本没有出现在上表位置，再向开发者报告，不要靠改数据 ID 猜测。

## 2. 用文本或 ID 反查位置

在仓库根目录运行：

```powershell
# 按现有文字寻找（适合收到截图或玩家反馈）
powershell -ExecutionPolicy Bypass -File .\tools\localization\find_localization_text.ps1 -Language cn -Query '闪光之镜'

# 按 UI key / 卡牌 ID / 事件 ID 寻找
powershell -ExecutionPolicy Bypass -File .\tools\localization\find_localization_text.ps1 -Language en -Query 'ui_RMR_Hub_Atlas'
```

脚本会输出 `Localize/<语言>/<文件>:<行号>`。先在中文文件确认语义和 ID，再编辑目标语言同一路径的对应条目。

## 3. 安全编辑规则

1. 只改人类可读文字：例如 `<Desc>...</Desc>`、`<LocalizedName>...</LocalizedName>`、`<Dialog>...</Dialog>`。
2. **绝不重命名** `id`、`ID`、`BookID`、`Frame ID`、`Choice ID`，也不要随意移动节点或改文件名。
3. 保存为 **UTF-8**。不要使用 ANSI、GBK；不要把 `kr` 当日文目录。
4. XML 中的 `&`、`<`、`>` 要写成 `&amp;`、`&lt;`、`&gt;`；多行 UI 文本使用 `&#10;`。
5. 保留 `{0}`、`{1}` 等占位符，以及 `[common]`、`[mirror]` 这类富文本标记。
6. 玩家英语把“图鉴”译为 **Compendium**，不是 Atlas；详见术语表。

## 4. 新文本与代码的边界

已有 key 的翻译不需要编译 C#。只有以下情况才需要开发者：

- 代码中没有对应文本 key；
- 需要新增事件分支、卡牌、被动或状态 ID；
- 需要日语/繁中独立目录；
- 文本显示成方块、乱码、旧语言残留，或修改后游戏完全没变化。

给开发者的最小信息应为：语言、文件相对路径、条目 ID、期望显示文本、截图（如有）。不要修改 `_release_packages/` 或游戏安装目录里的临时副本。

## 5. 交付前自检

```powershell
cd 'D:\VS_program\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main'

# 检查 cn/en/kr 文件结构、全部 XML、UIs.txt key 对齐
powershell -ExecutionPolicy Bypass -File .\tools\localization\validate_localization.ps1

# 只检查 UI key（更快）
powershell -ExecutionPolicy Bypass -File .\tools\localization\compare_ui_keys.ps1
```

翻译作者提交时应附上：修改的语言、文件清单、保留的疑问（术语、上下文或长度限制）。开发者负责 Release 构建、部署及游戏内确认。

