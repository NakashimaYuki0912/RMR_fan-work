# 禁止模式与安全模式

## 绝对禁止

1. **用错误编码保存中文文件**（ANSI/GBK 当 UTF-8）→ 口口口 / mojibake  
2. **自建 TMP 裸 `font=` + Bold** → 糊字  
3. **把未测试的 DLL 声称「游戏内已修好」**  
4. **在游戏运行时覆盖 Workshop DLL**  
5. **整仓用 `original-codes` 覆盖当前仓库**  
6. **修改 `_release_packages/` 代替改源码**  
7. **解放战 Stage 加上错误的 mod packageId**  
8. **情感选书把未知 script 当 Tier I**  
9. **中段 E.G.O. 发放未拥有页 / 批量塞手牌**  
10. **商店/神秘 EndBattle 后不设 NonCombat 标记就改 curstagetype**  
11. **为让过时 static_check 变绿而回退正确设计**  
12. **在外层非 git 目录提交 / 改错树**  
13. **git push 未得用户明确要求**  
14. **批量无关格式化 / 大重构夹带修复**  
15. **Unicode Emoji 当最终 UI 图标**（AGENTS 商店规则）  
16. **对 `LogueSave/*` 目标文件直接 `File.Create` 再序列化** → 崩溃留下空/全 0 档 → 情感 SelectOne（如巨目）选完抛 `SerializationException` 软锁；必须走 `LogueSaveManager.SaveData`（先写 `.tmp` 再 `File.Replace`）  
17. **在 `OnPickPassiveCard` / 选人收尾里让非关键副作用（图鉴统计、存档）抛出未捕获异常** → UI 关不掉、可连点多人；副作用必须 try/catch 或走已加固的 Save API  
18. **`PickUpModel_RMRVanillaEmotion.OnPickUp()` 无参对全队生效** → SelectOne 叠双重/全队；无参必须空，只在 `OnPickUp(BattleUnitModel)` 施加

---

## 强烈不推荐

- 在 `Update()` 无开关刷 Debug.Log  
- 新魔法坐标叠商店布局而不改 `CardShape`  
- 复制 DeepSeek/其他 agent 的「全局替换字体」补丁而不走 `ApplyTmpFontPreservingSharpMaterial`  
- 假设 `Level`（章节）== `EmotionLevel`（页阶）  

---

## 安全修改模板

### 新 UI 文本

```csharp
var tmp = go.AddComponent<TextMeshProUGUI>();
LogLikeMod.ApplyTmpFontPreservingSharpMaterial(tmp, LogLikeMod.DefFont_TMP);
tmp.fontSize = 24;
tmp.fontStyle = FontStyles.Normal;
tmp.text = TextDataModel.GetText("my_key"); // + Localize 三语
```

### 新本地化键

1. `Localize/cn/...` 写中文（UTF-8）  
2. `Localize/en/...`、`Localize/kr/...` 同步  
3. 部署同步 `Assemblies\dlls\Localize`  

### 新异想体奖励 script

1. 在 `ModScriptToVanillaScript` 加映射  
2. 在 `SeedStaticVanillaEmotionTiers` 写 EmotionLevel  
3. 确认解放门控楼层表  
4. 跑 `tools/static_checks/rewards/` 相关脚本  

### 修软锁 / EndBattle

1. 读现有 `IsLiveCombatBothSidesAlive` / `IsNonCombatNodeStage`  
2. 只加最小条件，不要新开「全局跳过 EndBattle」  
3. 非战斗离开用 `MarkNonCombatNodeExit`  

### 完成一次可测构建

1. 改 `BuildTimestamp`  
2. `deploy_workshop.ps1 -Configuration Release`  
3. 用户重启 → 对 log Build  
4. 汇报验证等级 1–5  

---

## 提交习惯

- 仓库：`...\ruina-roguelike-reborn-main\ruina-roguelike-reborn-main`  
- 用户要求 `git add .` 时注意：`tools/ui_options/server.err` 等噪音文件  
- 提交信息风格：祈使句说明 **为什么**（参考 `3291d33`）  
- 不 push 除非用户明确说  

---

## 与用户沟通

- 使用中文回复（用户偏好）  
- 区分「源码已改 / 已部署 / 已游戏内验证」  
- 截图路径若在微信 temp，可读图但勿依赖长期存在  
