# Balance backlog (Wave 4 audit)

Audit-only notes from fan-work feedback (2026-08-08). **No nerfs shipped in this pass** unless a clear infinite-stack bug is found.
IDs below were confirmed by grepping `Localize/en/PassiveInfo/*.txt` and `AddData/CardInfo/*.xml`; none have bespoke
`PassiveAbility_<id>Log.cs` / `DiceCardAbility_<id>Log.cs` files, so they run on generic vanilla-style passive/ability
plumbing rather than custom RMR retrigger code (lowers likelihood of a code-level infinite-stack bug; see below).

| Item | EN name / area | Passive/Card ID | Note | Follow-up |
|---|---|---|---|---|
| Ensemble weakness | Distorted Ensemble | Stage `70021` (`Stage_ch7.xml`, Grade7) | Vanilla scaling vs RMR player power | Chapter HP/dice multipliers for Grade7 bosses |
| Roland weakness | Black Silence | Stage `70020` (`Stage_ch7.xml`, Grade7) | Same | Same |
| Vanilla enemies weak | All chapters | Stage pools | RMR-exclusive enemies feel OK | Per-chapter enemy buff table |
| Stigma Workshop | Membership Stigma | `8582001`-`8582008` (`PassiveList_MemberShip_Stigma.txt`: Talented Person Suffers Harm, Bringing up the Past for Nothing, Mutual Hatred, Parting Flame, Ash, Fast and Urgent Like a Star, Stigma) | Underwhelming vs Mook/Union | Passive pass design review |
| Apnea | Mook | `8583009` (`PassiveList_MemberShip_Mook.txt`) — "Pages can be played without sufficient Light by losing 3 Stagger Resist per point of insufficient Light" | Light cost trivial | Cap / cost floor |
| Mook passives | Mook set | `8583001`-`8583010` range, same file | "Consistently broken" | Per-passive audit |
| Afterimage | Mook | `8583008` — "first Offensive die that wins a clash is reused as a one-sided attack" | Strong even without dedicated build | Trigger frequency |
| Sixth Finger | Union / Membership | `8581003` "Bionic Equipment - Sixth Finger" (`PassiveList_MemberShip_Union.txt`) — recycles a losing die once per die | Cap on multi-die pages | Likely fine; recycle is once-per-die already |
| Tattered Wings | Union | `8581006` "Bionic Equipment - Tattered Wings" | Cancel one one-sided page | Likely intended power |
| Bloodsucking + Hunter | Union + chstart item | `8581002` "Bionic Equipment - Bloodsucking Fingernail" (Bleed on hit + Bleed-stack bonus dmg) + `RMR_HunterCloak` (`Localize/en/LogueEffectText/RMR_chstart_items.xml`) | Synergy abuse risk | Instrument triggers |
| Shocking Blow + Apnea stack | Card `303005` (`CardInfo_ch3.xml` / `Localize/*/CardInfo/CardInfo_ch3.txt`) | Combo with Afterimage | Report of +50 power | Add stack cap if confirmed |
| Distorted Blade + Mook | Yan exclusive combat pages `611001`/`611002`/`611003` (player), `611008` (enemy) | Craft exclusive via `CraftExclusiveCardChapter6` | Abnormal multiplier risk | **Verified (Wave5): not in any normal reward/drop pool** — see below |

## Wave5 verification: Distorted Blade exclusivity

Confirmed `611001`-`611003` only appear in:
- `AddData/CardDropTable/CardDropTable_exclusives.xml` `DropTable ID="-999999"` (an explicit non-random exclusion/registry
  table, protected by `RMR_0729_special_unlock_and_redmist_upgrade_static_check.ps1`), and
- Yan's own `AddData/EquipPage/EquipPage_Librarian_ch6.xml` / `AddData/Deck/Deck_enemy_ch6.xml` (her `OnlyCard` list).

They do **not** appear in `SpecialStaticInfo/RewardPassiveInfos/*.xml` or `AddData/CardDropTable/CardDropTable_ch6.xml`
(the Urban Star normal/battle-page reward pools). `LogueBookModels.GetCorePageExclusiveBattleCardIds()` additionally
prunes any role book's `EquipEffect.OnlyCard` pages from `cardlist`/Compendium on load (generic, not Yan-specific),
except the explicitly-obtainable `608004`. No code change made — exclusivity is already enforced.

## Infinite / clear bugs

None confirmed in this audit pass. Prefer Player.log counters before nerfing.
`Apnea`/`Afterimage`/`Sixth Finger`/`Tattered Wings`/`Bloodsucking Fingernail` have no dedicated RMR C# retrigger
script (declarative passive text only), which reduces (but doesn't fully rule out, since the underlying vanilla
passive-type engine is closed-source) the chance of an RMR-introduced infinite-stack bug specifically.
