# 本地化、字体与「口口口 / 糊字」

> **本文件是历史事故高发区。**  
> 多名 agent（含 DeepSeek）曾在此引入：  
> - 中文变成 **口口口 / □□□ / tofu**  
> - 中文 **显示但极度模糊（糊到看不清）**  
> - 开场 PV / 编队人名 **韩文或空白**  
> - 本地化文件 **GBK/ANSI 污染**

改任何 UI 文本、TMP 字体、Localize 文件前 **必须** 读完本页。

**给人类译者（改文案、不改字体）**：见 [docs/localization/](../localization/README.md)  
（术语表、文件地图、English translator guide）。玩家向英文 **图鉴 = Compendium**，不要写 Atlas。

---

## 1. 三种不同故障（不要混诊）

| 现象 | 常见根因 | 不是这个 |
|---|---|---|
| **口口口 / tofu** | 字体缺字形；错误字体（韩文 Noto 当中文）；Fallback 局部 atlas；文本本身已是错误编码 | 单纯字号太小 |
| **字能认但糊** | `FontStyles.Bold` 伪粗体（CJK SDF 无真粗体）；`tmp.font=` 不绑匹配材质；OS 动态 `CreateFontAsset`；**中文槽位被写成 kr/jp 面或 Serif/Medium 面** | 本地化缺键 |
| **显示英文键 / 空白** | `Localize` 缺键或语言目录错 | 字体问题 |
| **乱码 mojibake** | 文件用系统默认编码保存（中文 Windows 常为 GBK），游戏按 UTF-8 读 | TMP Bold |

诊断顺序：

1. `Player.log` 里 `DefFont_TMP = '...'` 是什么？  
2. 文本在源码 / Localize 文件里用 UTF-8 打开是否正确？  
3. UI 是否走了 `ApplyTmpFontPreservingSharpMaterial`？

---

## 2. 正确字体来源（中文）

原版 LoR 中文 UI 使用 **LocalizedFontSetter** 的：

- `cnFont_notoSansCJKsc` → 通常是 **`NotoSansCJKsc-Regular SDF`**
- `cnFont_notoSerifCJKsc`

**不要**把 `font_NotoSans`（多为 en 路径，历史上曾指向 CJKkr）当中文主字体。

RMR 统一入口：

```csharp
// 获取当前语言匹配的 Noto SDF
TMP_FontAsset font = LogLikeMod.DefFont_TMP;

// 赋给自建 TextMeshProUGUI 的唯一推荐方式
LogLikeMod.ApplyTmpFontPreservingSharpMaterial(tmp, font);
```

`DefFont_TMP` getter 会：

- 优先 `LocalizedFontSetter.cnFont_notoSansCJKsc`
- 拒绝 **空名字**、**Dynamic OS 软字体**、**`[Fallback_*]` 局部表**
- 拒绝把微软雅黑等 OS face 当主 UI 字体（除非 Noto 完全不可用）

日志期望（语言 cn）：

```text
[RMR Localize] DefFont_TMP = 'NotoSansCJKsc-Regular SDF' for lang=cn.
[RMR Localize] Using LocalizedFontSetter.cnFont_notoSansCJKsc='NotoSansCJKsc-Regular SDF'.
```

若看到：

```text
DefFont_TMP = ''
```

说明早期拿到了空名软字体；当前代码应丢弃并重解析。若仍出现，查是否有代码路径绕过 getter 直接写 `_DefFont_TMP`。

### 2.1 字形区域铁律（2026-07-26 加固）

**Noto CJK 是泛 CJK 字族**：`NotoSansCJKkr` / `NotoSerifCJKkr` 同样含有汉字，
`HasCharacter('图')` 会返回 true。**所以「能显示中文」证明不了「是中文字体」。**

```text
❌ 曾经发生：
[RMR Localize] LocalizedFontSetter.cnFont_notoSansCJKsc:
    '[Fallback_1]NotoSansCJKsc-Regular SDF' → 'NotoSerifCJKkr-Medium SDF' (lang=cn).
→ 原版 SetLocalizedFont(cn) 之后把韩文 Serif Medium 发给每一个中文 TMP → 全局糊
```

规则：

- 判定语言可用性 **必须** 走 `IsFaceRegionAcceptableForLanguage`：
  cn/trcn 只收 `sc`；kr 只收 `kr`；jp 只收 `jp`；en 不限（原版 en 本来就用 kr 面）。
- **`[Fallback_N]` 前缀不等于坏字体。** 原版 cn 槽位存的就是
  `[Fallback_1]NotoSansCJKsc-Regular SDF`，那是正牌 SC 资源。判断前先
  `StripTmpFallbackPrefix`。
- 两个判定函数分工，**不要互换**：

| 函数 | 用途 |
|---|---|
| `IsTmpFontCompatibleWithLanguage` | 从零**挑选新主字体**时用；仍否决 Fallback 名，防局部 atlas 导致 口口口 |
| `IsUsableLanguageFace` | 评价**原版已经装好**的面（setter 槽位、live TMP）；忽略 Fallback 标签 |

- 写 `LocalizedFontSetter` 任何语言槽位一律经 `PatchSetterFontField`，
  它会硬拦截区域不符的面并 `LogWarning`；**禁止**再加绕过它的直写路径。
- 字重偏好：Sans + Regular 优先，Serif / Medium / Bold 在小字号 SDF 上就是糊（`ScoreFace`）。

---

## 3. 禁止与推荐（自建 UI）

### 禁止

```csharp
// 1) 裸赋值 — 容易留下错误 material → 糊 / 坏 SDF
tmp.font = LogLikeMod.DefFont_TMP;

// 2) CJK 伪粗体 — NotoSansCJKsc-Regular 没有 Bold face，会「胀边」成糊字
tmp.fontStyle = FontStyles.Bold;

// 3) 再叠 outline 加粗 — 更糊
tmp.outlineWidth = 0.14f;

// 4) 自己 CreateFontAsset(OS字体) 当主 UI
TMP_FontAsset.CreateFontAsset(Font.CreateDynamicFontFromOSFont(...));

// 5) 把韩文/日文字体塞给 cn
// 6) 用系统默认编码写 Localize 或含中文的 .cs / .ps1
```

### 推荐

```csharp
var tmp = go.AddComponent<TextMeshProUGUI>();
LogLikeMod.ApplyTmpFontPreservingSharpMaterial(tmp, LogLikeMod.DefFont_TMP);
tmp.fontSize = 28;                 // 需要「加重」时用更大字号，不要 Bold
tmp.fontStyle = FontStyles.Normal;
tmp.richText = false;
// 或走统一工厂（商店/神秘事件等）
ModdingUtils.CreateText_TMP(parent, pos, size, ..., color, LogLikeMod.DefFont_TMP);
```

`CreateText_TMP` 内部已调用 `ApplyTmpFontPreservingSharpMaterial`（2026-07-12 起）。

### 已对齐的自定义面板

| 面板 | 文件 |
|---|---|
| 开局主菜单 | `RMR_StartHubPanel.cs` |
| 解放战选层 | `LogRealizationPanel.cs` |
| 玩法介绍 | `RMR_HelpHandbookPanel.cs` |
| 图鉴 | `LogAtlasPanel.cs` |
| 工厂 | `ModdingUtils.CreateText_TMP` |

**新增任何 `AddComponent<TextMeshProUGUI>()` 必须走同样路径。**

---

## 4. 文本内容从哪来

| 用途 | 机制 |
|---|---|
| 模组 UI 键 | `abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("key")` + `Localize/{lang}/` |
| Hub 文案 fallback | `RMR_StartHubPanel.T(key, zh, en, kr)` — 键缺失时用参数 |
| 战斗书页名 | 游戏 + 模组 CardInfo 本地化 |
| 异想体书页名/描述 | 原版 `AbnormalityCards` + 模组 script → vanilla Name 映射（见 `RMR_AbnormalityUnlocks`） |

C# 源码里长期保留中文时优先：

```csharp
"\u6b63\u5e38\u6e38\u73a9"  // 正常游玩
```

避免依赖终端代码页显示中文再复制进文件。

---

## 5. 文件编码铁律

- **所有** `Localize/**`、含中文的 `.cs` / `.xml` / `.txt` / `.ps1`：**UTF-8**
- PowerShell 读写：

```powershell
Get-Content -Path $f -Encoding UTF8 -Raw
[System.IO.File]::WriteAllText($f, $content, [System.Text.UTF8Encoding]::new($false))  # 无 BOM 亦可；有 BOM 一般也可
```

- 禁止：记事本「ANSI」保存、某些 agent 用系统默认 `Out-File` 无 `-Encoding utf8`
- 修改后可用：在编辑器中打开确认中文；或检查是否出现 `Ã` `æ` 等 mojibake

---

## 6. 异想体名「口口口」另一类原因

与字体无关时：

- 模组 `script`（如 `UniverseZogak2`）与原版 `Script` / `Name` 不一致  
- 必须用 `RMR_AbnormalityUnlocks.ModScriptToVanillaScript` + `FindVanillaEmotionCard` 取 **Name** 再本地化  
- 缺映射 → 名称/描述失败 → 空或豆腐  

见 `04-known-bugs-and-fixes.md` 中的 script 别名表。

---

## 7. 开场 PV / 编队人名

- `OpeningLyrics` 等资源编码错误 → 歌词 tofu  
- `UICharacterSlot.Name` 未刷字体 → 人名 tofu  
相关补丁在 `LogLikePatches` + `LogLikeMod` 本地化修复路径；改这些时保持 **只补字体/编码，不改业务流程**。

---

## 8. 自检清单（改 UI 后）

- [ ] 无新增 `tmp.font = ...` 裸赋值（应用 `ApplyTmpFont...`）
- [ ] 无 CJK 上 `FontStyles.Bold`（除非字体名含 Bold 且已验证）
- [ ] `Localize` 文件 UTF-8，中文在编辑器可读
- [ ] 部署后 `Player.log`：`DefFont_TMP = 'NotoSansCJKsc-Regular SDF'`
- [ ] 游戏内看：Hub、解放战选层、图鉴、商店按钮、神秘事件选项

---

## 9. 关键 API 索引

| API | 文件 |
|---|---|
| `LogLikeMod.DefFont_TMP` | `abcdcode_LOGLIKE_MOD/LogLikeMod.cs` |
| `ApplyTmpFontPreservingSharpMaterial` | 同上 |
| `EnsureLocalizedFonts` / `RepairActiveTmpFonts` | 同上 |
| `CreateText_TMP` | `abcdcode_LOGLIKE_MOD/ModdingUtils.cs` |
| `BuildTimestamp` 日志 | `RMR_Core.cs` |
