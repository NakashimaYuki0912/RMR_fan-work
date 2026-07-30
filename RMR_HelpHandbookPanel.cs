// -----------------------------------------------------------------------------
// RogueLike Mod Reborn (RMR): RMR_HelpHandbookPanel
// Namespace/file: ruina-roguelike-reborn-main\RMR_HelpHandbookPanel.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using abcdcode_LOGLIKE_MOD;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace RogueLike_Mod_Reborn
{
    /// <summary>
    /// Player-facing RMR handbook using the selected H-A illustrated index layout.
    /// </summary>
    public class RMRHelpHandbookPanel : MonoBehaviour
    {
        public static RMRHelpHandbookPanel Instance { get; private set; }

        private GameObject _root;
        private TextMeshProUGUI _bodyText;
        private TextMeshProUGUI _sectionTitle;
        private ScrollRect _bodyScroll;
        private RectTransform _bodyContentRt;
        private readonly List<TextMeshProUGUI> _navLabels = new List<TextMeshProUGUI>();
        private readonly List<Image> _navFrames = new List<Image>();
        private int _index;
        public bool IsVisible => _root != null && _root.activeSelf;

        private static readonly Color ColGold = new Color(0.776f, 0.608f, 0.333f, 1f);
        private static readonly Color ColGoldDim = new Color(0.384f, 0.286f, 0.161f, 1f);
        private static readonly Color ColCream = new Color(0.933f, 0.898f, 0.827f, 1f);
        private static readonly Color ColMuted = new Color(0.620f, 0.569f, 0.482f, 1f);
        private static readonly Color ColPanel = new Color(0.051f, 0.043f, 0.031f, 0.96f);
        private static readonly Color ColBodyBg = new Color(0.06f, 0.05f, 0.04f, 1f);
        private static readonly Color ColNavIdle = new Color(0.122f, 0.098f, 0.067f, 0.90f);
        private static readonly Color ColNavOn = new Color(0.314f, 0.216f, 0.114f, 0.96f);

        private struct Page
        {
            public string NavKey;
            public string NavZh;
            public string NavEn;
            public string BodyKey;
            public string BodyZh;
            public string BodyEn;
            public string[] ArtKeys;
        }

        private static readonly Page[] Pages = BuildPlayerPages();
        #region --- UI build / show ---


        private static Page[] BuildPlayerPages()
        {
            return new[]
            {
                new Page
                {
                    NavKey = "ui_RMR_Help_Nav_Overview",
                    NavZh = "基础流程",
                    NavEn = "Core Flow",
                    BodyKey = "ui_RMR_Help_Body_Overview",
                    ArtKeys = new[] { "随机事件背景1", "MysteryButton_Enable", "Shop_CardUpgrade_Icon" },
                    BodyZh =
                        "【核心结构】\n" +
                        "本模组采用漏斗式关卡分布。前期资源较少、战斗压力较低、流程更紧凑，帮助玩家快速完成初期构筑；随着流程推进，关卡数量、敌人强度与构筑要求逐步提高，奖励种类和质量也会随之变化。\n\n" +
                        "【路线选择】\n" +
                        "玩家需要在普通战斗、异想体战斗、Boss 战、商店、休息与特殊事件之间选择下一站，并根据当前队伍、书页构筑和资源规划后续路线。\n\n" +
                        "【节点与楼层】\n" +
                        "第一、第二章不会生成异想体战斗节点，相关位置会改为休息节点。准备界面切换楼层只会改变地图主题与背景音乐，不会改变队伍，也不消耗接待次数。\n\n" +
                        "【旅程终点】\n" +
                        "每次流程都会从较低都市阶段逐步推进。击败章节 Boss 后进入下一阶段，最终击败杂质阶段 Boss 即完成本次旅程。",
                    BodyEn =
                        "[Core structure]\nRMR uses a funnel-shaped route. Early stages are short and relatively forgiving while resources are scarce; later stages expand the route, strengthen enemies, and demand a more complete build while offering broader and better rewards.\n\n" +
                        "[Route choices]\nChoose among normal battles, abnormality battles, bosses, shops, rests, and mysteries according to your team, pages, and resources.\n\n" +
                        "[Nodes and floors]\nChapters 1 and 2 replace abnormality-battle nodes with rests. Switching floors in preparation only changes the map theme and music; it does not change the team or consume a reception.\n\n" +
                        "[Journey's end]\nDefeating each chapter boss advances the run. Defeating the Impuritas Civitatis boss completes the journey."
                },
                new Page
                {
                    NavKey = "ui_RMR_Help_Nav_Rewards",
                    NavZh = "关卡奖励",
                    NavEn = "Stage Rewards",
                    BodyKey = "ui_RMR_Help_Body_Rewards",
                    ArtKeys = new[] { "异想体战斗", "随机事件背景3", "Shop_CardUpgrade_Icon" },
                    BodyZh =
                        "【奖励种类】\n" +
                        "关卡奖励包括战斗书页、核心书页、异想体书页、被动能力与 E.G.O.战斗书页，不同关卡使用不同的奖励规则。\n\n" +
                        "【普通关卡】\n" +
                        "普通关卡可获得战斗书页、核心书页与异想体书页。异想体书页池会随流程阶段累积扩充：传闻至都市传说开放历史、艺术、科技、文学层；都市恶疾至都市梦魇追加语言、自然、社会层；都市之星至杂质再追加宗教、总类、哲学层。已进入池中的书页不会在后续阶段退出。\n\n" +
                        "【战斗书页与升级】\n" +
                        "战斗书页按“种类”获得，而不是按单张库存计算；升级也对整个书页种类生效。\n\n" +
                        "【情感选书与 E.G.O.】\n" +
                        "战斗中的异想体书页只会从当前路线已经拥有的内容中产生：队伍情感 1–2 对应 I 阶，3–4 对应 II 阶，5 对应 III 阶。页名后缀数字不是阶级；中途 E.G.O.同样只会出现当前路线已经拥有的内容。\n\n" +
                        "【异想体战斗】\n" +
                        "异想体战斗不提供战斗书页和核心书页，改为连续进行三次异想体书页选择，用于快速扩充收集并补足当前构筑。\n\n" +
                        "【Boss 战】\n" +
                        "Boss 战除普通基础奖励外，还可额外选择一项被动能力。完成对应楼层解放后，Boss 奖励还会加入该层专属异想体书页与 E.G.O.战斗书页。",
                    BodyEn =
                        "[Reward types]\nStages can award combat pages, key pages, abnormality pages, passives, and E.G.O. combat pages.\n\n" +
                        "[Normal stages]\nNormal stages award combat, key, and abnormality pages. The abnormality pool grows cumulatively: the early stages use Malkuth, Netzach, Yesod, and Hod; Urban Plague/Nightmare add Gebura, Tiphereth, and Chesed; Star of the City/Impuritas add Hokma, Keter, and Binah.\n\n" +
                        "[Combat page types]\nCombat pages are obtained and upgraded by type rather than as individual copies.\n\n" +
                        "[Emotion picks and E.G.O.]\nMid-battle abnormality choices use only pages owned in the current route: team Emotion 1–2 selects tier I, 3–4 tier II, and 5 tier III. A page-name suffix is not its tier. Mid-battle E.G.O. choices are also limited to E.G.O. owned in the route.\n\n" +
                        "[Abnormality battles]\nThese replace combat/key page rewards with three consecutive abnormality-page choices.\n\n" +
                        "[Boss battles]\nBosses also offer a passive. Cleared realizations add their exclusive abnormality and E.G.O. pages to eligible boss rewards."
                },
                new Page
                {
                    NavKey = "ui_RMR_Help_Nav_Shop",
                    NavZh = "商店",
                    NavEn = "Shop",
                    BodyKey = "ui_RMR_Help_Body_Shop",
                    ArtKeys = new[] { "Shop_CardUpgrade_Icon", "Stage_Shop", "随机事件背景2" },
                    BodyZh =
                        "【可购买内容】\n" +
                        "商店可购买战斗书页、核心书页、被动能力、异想体书页与 E.G.O.战斗书页，并提供战斗书页升级。商品受到章节、楼层解放状态与当前库存影响。\n\n" +
                        "【战斗书页升级】\n" +
                        "商店提供战斗书页升级。由于战斗书页按种类获得，升级同样对整个书页种类生效，而不是只强化一张单独书页。\n\n" +
                        "【升级费用】\n" +
                        "首次升级需要 10 眼。每完成一次升级，下一次费用增加 2 眼：10 → 12 → 14 → 16……\n\n" +
                        "该数值仍可能根据资源获取速度、升级收益、商店频率、后期难度与不同构筑强度继续调整。",
                    BodyEn =
                        "[Stock]\nShops sell combat pages, key pages, passives, abnormality pages, and E.G.O. combat pages, and offer combat-page upgrades. Stock depends on the chapter, realization progress, and current inventory.\n\n" +
                        "[Combat page upgrades]\nBecause combat pages are owned by type, an upgrade strengthens that page type rather than one individual copy.\n\n" +
                        "[Upgrade price]\nThe first upgrade costs 10 Ahn. Each successful upgrade raises the next price by 2: 10 → 12 → 14 → 16… These values may still be tuned."
                },
                new Page
                {
                    NavKey = "ui_RMR_Help_Nav_Atlas",
                    NavZh = "图鉴系统",
                    NavEn = "Compendium",
                    BodyKey = "ui_RMR_Help_Body_Atlas",
                    ArtKeys = new[] { "随机事件背景2", "Shop_CardUpgrade_Icon" },
                    BodyZh =
                        "【记录内容】\n" +
                        "战斗过程中获得的核心书页、战斗书页、异想体书页与 E.G.O.战斗书页都会记录在永久图鉴中，升级后的战斗书页效果也会同步记录。\n\n" +
                        "【查看与收集】\n" +
                        "玩家可以随时通过图鉴查看已收集内容的具体效果与当前升级状态。\n\n" +
                        "【玩法用途】\n" +
                        "图鉴不仅用于收藏，也会影响解放战与部分特殊关卡中可选择的书页。永久图鉴与当前路线库存彼此独立，新路线不会自动获得图鉴中的全部内容。\n\n" +
                        "【路线存档】\n" +
                        "队伍配置、库存、金币、章节进度与剩余节点只属于当前旅程；图鉴、解放战与特殊 Boss 首通记录则跨旅程保留。开局菜单只会在存在有效路线存档时显示“继续”。",
                    BodyEn =
                        "[Recorded content]\nKey pages, combat pages, abnormality pages, and E.G.O. combat pages obtained during play are recorded permanently. Upgraded combat-page effects are recorded as well.\n\n" +
                        "[Viewing]\nUse the Compendium to review collected effects and current upgrade states.\n\n" +
                        "[Gameplay use]\nCompendium unlocks determine the pages available in realizations and some special stages. The permanent Compendium remains separate from current-run inventory.\n\n" +
                        "[Run saves]\nTeam setup, inventory, money, chapter progress, and remaining nodes belong only to the current journey. Compendium, realization, and special-boss first-clear records persist. Continue appears only when a valid run save exists."
                },
                new Page
                {
                    NavKey = "ui_RMR_Help_Nav_Realization",
                    NavZh = "解放战",
                    NavEn = "Realizations",
                    BodyKey = "ui_RMR_Help_Body_Realization",
                    ArtKeys = new[] { "异想体战斗", "随机事件背景1" },
                    BodyZh =
                        "【图鉴限制】\n" +
                        "解放战中的核心书页、战斗书页、异想体书页与 E.G.O.战斗书页，都必须从永久图鉴已经收集的内容中选取；未记录的书页不能直接使用。\n\n" +
                        "【章节限制】\n" +
                        "历史、艺术、科技与文学层解放战只能使用都市梦魇及以下章节的战斗书页与核心书页，以保留对应阶段的构筑体验。\n\n" +
                        "【首次通关奖励】\n" +
                        "首次完成某层解放战后，该层专属异想体书页会加入后续关卡掉落池，对应 E.G.O.战斗书页会在满足条件的 Boss 战后出现。已经通关的楼层可以再战，但不会重复发放首通奖励。\n\n" +
                        "【临时编队】\n" +
                        "解放战使用临时编队，战后会恢复正常路线配置，不会污染当前旅程。",
                    BodyEn =
                        "[Compendium limit]\nAll key, combat, abnormality, and E.G.O. pages used in a realization must already be recorded in the permanent Compendium.\n\n" +
                        "[Chapter limit]\nMalkuth, Netzach, Yesod, and Hod realizations only allow key and combat pages from Urban Nightmare or earlier.\n\n" +
                        "[First-clear rewards]\nA first clear adds that floor's exclusive abnormality pages to later reward pools and makes its E.G.O. eligible after suitable bosses. Replays do not repeat these rewards.\n\n" +
                        "[Temporary loadout]\nRealization teams are temporary; the normal-run setup is restored afterward."
                },
                new Page
                {
                    NavKey = "ui_RMR_Help_Nav_Special",
                    NavZh = "特殊掉落与角色解锁",
                    NavEn = "Special Drops & Unlocks",
                    BodyKey = "ui_RMR_Help_Body_Special",
                    ArtKeys = new[] { "随机事件背景3", "异想体战斗" },
                    BodyZh =
                        "【殷红迷雾与 Binah】\n" +
                        "进入都市之星后，殷红迷雾有一定概率以特殊精英战形式出现。迎战时会暂时开放 Binah 的使用权限；胜利后 Binah 会加入图鉴，并可在后续战斗中继续使用。本场普通奖励则替换为殷红迷雾的核心书页与对应战斗书页。\n\n" +
                        "【漆黑噤默】\n" +
                        "首次通关以罗兰为 Boss 的杂质阶段后，漆黑噤默会加入图鉴。此后进入都市之星阶段时，其核心书页会自动加入本次路线的可用核心书页。\n\n" +
                        "【阿尔加利亚】\n" +
                        "首次通关以残响乐团为 Boss 的杂质阶段后，阿尔加利亚会加入图鉴。此后进入都市之星阶段时，其核心书页与对应战斗书页会自动加入本次路线。",
                    BodyEn =
                        "[The Red Mist and Binah]\nAt Star of the City, the Red Mist may appear as a special elite encounter. Binah becomes temporarily available for the fight. Victory records Binah for later battles and replaces normal rewards with the Red Mist's key and combat pages.\n\n" +
                        "[The Black Silence]\nClear the Roland boss at Impuritas Civitatis once to record the Black Silence. Future Star of the City runs automatically add its key page to the route.\n\n" +
                        "[Argalia]\nClear the Distorted Ensemble boss once to record Argalia. Future Star of the City runs automatically add his key and combat pages."
                },
                new Page
                {
                    NavKey = "ui_RMR_Help_Nav_Goal",
                    NavZh = "整体流程目标",
                    NavEn = "Overall Run Goals",
                    BodyKey = "ui_RMR_Help_Body_Goal",
                    ArtKeys = new[] { "MysteryButton_Enable", "Shop_CardUpgrade_Icon" },
                    BodyZh =
                        "【核心目标】\n" +
                        "本玩法并不是单纯重复原版接待，而是在每次流程中不断选择、取舍与构筑。\n\n" +
                        "【构筑方向】\n" +
                        "你需要在获取新的战斗书页与核心书页、收集异想体书页、购买被动与其他补给、强化已有书页、挑战解放战、解锁特殊角色与 E.G.O.、完善 Boss 战构筑之间权衡资源。\n\n" +
                        "【重复游玩】\n" +
                        "随着流程推进，玩家能够使用的书页、遗物与特殊角色会逐步增加，并在每次旅程中形成不同的队伍与战斗方式。",
                    BodyEn =
                        "[Core goal]\nRMR is not simply a sequence of base-game receptions. Every run asks you to choose, trade off, and build.\n\n" +
                        "[Build directions]\nBalance new combat and key pages, abnormality pages, passives and other supplies, upgrades, realizations, special characters, E.G.O., and preparation for later bosses.\n\n" +
                        "[Replay]\nAs the run advances, available pages and special characters expand, letting each journey produce a different team and combat style."
                }
            };
        }

        void Awake()
        {
            Instance = this;
        }

        public static void ShowOrCreate(Transform preferredParent = null)
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("RMRHelpHandbookPanel");
                Instance = go.AddComponent<RMRHelpHandbookPanel>();
            }
            Instance.Show(preferredParent);
        }

        public void Show(Transform preferredParent = null)
        {
            if (_root != null)
                Destroy(_root);
            _navLabels.Clear();
            _navFrames.Clear();
            LogLikeMod.InvalidateTmpFontCache();
            if (LogLikeMod.DefFont_TMP == null)
                Debug.LogWarning("[RMRHelpHandbookPanel] No CJK TMP font.");
            Build(preferredParent);
            Select(0);
        }

        public void Hide()
        {
            if (_root != null)
                Destroy(_root);
            _root = null;
            _navLabels.Clear();
            _navFrames.Clear();
            try { RMRRealizationLaunchHost.DestroyOverlayIfEmpty(); } catch { }
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Help handbook strings: load from <c>Localize/*/UIs.txt</c> by <paramref name="key"/>.
        /// Fallback zh/en only when the key is absent. See docs/localization/GLOSSARY.md for EN terms
        /// (Compendium, Realization, Abnormality page, …).
        /// </summary>
        private static string T(string key, string zh, string en = null)
        {
            try
            {
                string text = TextDataModel.GetText(key);
                if (!string.IsNullOrEmpty(text) && text != key)
                    return text;
            }
            catch { }
            string lang = "";
            try { lang = TextDataModel.CurrentLanguage.ToString().ToLowerInvariant(); } catch { }
            if (lang.Contains("en") && !string.IsNullOrEmpty(en))
                return en;
            return zh;
        }

        private Transform ResolveParent(Transform preferred)
        {
            if (preferred != null)
                return preferred;
            try { return RMRRealizationLaunchHost.GetOrCreateOverlayRoot(); }
            catch { }
            return transform;
        }
        #endregion

        #region --- UI build / show ---


        private void Build(Transform preferredParent)
        {
            Transform parent = ResolveParent(preferredParent);
            _root = new GameObject("HelpHandbookRoot", typeof(RectTransform));
            _root.transform.SetParent(parent, false);
            _root.transform.SetAsLastSibling();

            RectTransform rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var dim = _root.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.015f, 0.01f, 1f);
            dim.raycastTarget = true;

            // Scheme A / H-A: double gold rim + dark archive card.
            MakeSolid(_root.transform, "OuterRim", Vector2.zero, new Vector2(1192f, 732f),
                new Color(ColGold.r, ColGold.g, ColGold.b, 0.22f));
            var frame = MakeSolid(_root.transform, "Frame", Vector2.zero, new Vector2(1180f, 720f), ColGoldDim);
            var card = MakeSolid(frame.transform, "Card", Vector2.zero, new Vector2(1168f, 708f), ColPanel);
            StretchInset(card.GetComponent<RectTransform>(), 4f);

            MakeTmp(card.transform, "Title", new Vector2(0f, 310f), new Vector2(1000f, 40f), 30,
                TextAlignmentOptions.Center, T("ui_RMR_Hub_Help", "玩法介绍", "How to Play")).color = ColGold;
            MakeTmp(card.transform, "Sub", new Vector2(0f, 280f), new Vector2(1000f, 22f), 13,
                TextAlignmentOptions.Center, "\u5728\u4e0d\u65ad\u53d8\u5316\u7684\u63a5\u5f85\u4e2d\uff0c\u6784\u7b51\u5c5e\u4e8e\u4f60\u7684\u56fe\u4e66\u9986  \u00b7  BUILD YOUR OWN LIBRARY").color = ColGoldDim;
            MakeSolid(card.transform, "TitleRule", new Vector2(0f, 262f), new Vector2(520f, 1.5f),
                new Color(ColGoldDim.r, ColGoldDim.g, ColGoldDim.b, 0.75f));

            // H-A left illustrated chapter index.
            float navTop = 210f;
            float navStep = 56f;
            for (int i = 0; i < Pages.Length; i++)
            {
                int idx = i;
                float y = navTop - i * navStep;
                var btnGo = new GameObject("Nav" + i, typeof(RectTransform));
                btnGo.transform.SetParent(card.transform, false);
                var img = btnGo.AddComponent<Image>();
                img.color = ColNavIdle;
                var brt = btnGo.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = new Vector2(230f, 48f);
                brt.anchoredPosition = new Vector2(-430f, y);
                _navFrames.Add(img);
                MakeSolid(btnGo.transform, "NavAccent", new Vector2(-112f, 0f), new Vector2(3f, 40f), ColGoldDim);
                var navIcon = MakeSolid(btnGo.transform, "NavIcon", new Vector2(-88f, 0f), new Vector2(38f, 38f),
                    new Color(0.10f, 0.08f, 0.06f, 1f)).GetComponent<Image>();
                Sprite navArt = ResolveArt(Pages[i].ArtKeys);
                if (navArt != null)
                {
                    navIcon.sprite = navArt;
                    navIcon.preserveAspect = true;
                    navIcon.color = Color.white;
                }
                else
                    navIcon.enabled = false;
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;
                var label = MakeTmp(btnGo.transform, "L", new Vector2(18f, 0f), new Vector2(172f, 42f), 16,
                    TextAlignmentOptions.Center, T(Pages[i].NavKey, Pages[i].NavZh, Pages[i].NavEn));
                // Keep the full player-facing category name readable in all three languages.
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = 16f;
                label.color = ColCream;
                _navLabels.Add(label);
                btn.onClick.AddListener(() =>
                {
                    try { UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click); } catch { }
                    Select(idx);
                });
            }

            // H-A body: title + continuous scroll only (no stretched banner artwork — user request).
            var bodyBg = MakeSolid(card.transform, "BodyBg", new Vector2(130f, 10f), new Vector2(800f, 520f), ColBodyBg);
            try { bodyBg.GetComponent<Image>().raycastTarget = true; } catch { }

            _sectionTitle = MakeTmp(bodyBg.transform, "SecTitle", new Vector2(0f, 220f), new Vector2(740f, 36f), 22,
                TextAlignmentOptions.Left, "");
            _sectionTitle.color = ColGold;

            MakeSolid(bodyBg.transform, "Rule", new Vector2(0f, 198f), new Vector2(740f, 1.5f),
                new Color(ColGoldDim.r, ColGoldDim.g, ColGoldDim.b, 0.7f));

            // Scrollbar is the only body navigation; no redundant 1/2/3 paging.
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(bodyBg.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = viewportRt.anchorMax = new Vector2(0.5f, 0.5f);
            viewportRt.sizeDelta = new Vector2(760f, 380f);
            viewportRt.anchoredPosition = new Vector2(-6f, -20f);
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0.05f, 0.04f, 0.03f, 0.55f);
            viewportImg.raycastTarget = true;
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _bodyContentRt = contentGo.GetComponent<RectTransform>();
            _bodyContentRt.anchorMin = new Vector2(0f, 1f);
            _bodyContentRt.anchorMax = new Vector2(1f, 1f);
            _bodyContentRt.pivot = new Vector2(0.5f, 1f);
            _bodyContentRt.anchoredPosition = Vector2.zero;
            _bodyContentRt.sizeDelta = new Vector2(0f, 320f);

            _bodyText = MakeTmp(contentGo.transform, "Body", Vector2.zero, new Vector2(720f, 300f), 17,
                TextAlignmentOptions.TopLeft, "");
            _bodyText.color = ColCream;
            _bodyText.enableWordWrapping = true;
            _bodyText.overflowMode = TextOverflowModes.Overflow;
            _bodyText.lineSpacing = 10f;
            var bodyRt = _bodyText.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 1f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.anchoredPosition = new Vector2(0f, -10f);
            bodyRt.offsetMin = new Vector2(18f, bodyRt.offsetMin.y);
            bodyRt.offsetMax = new Vector2(-18f, -10f);
            bodyRt.sizeDelta = new Vector2(-36f, 300f);

            _bodyScroll = bodyBg.AddComponent<ScrollRect>();
            _bodyScroll.viewport = viewportRt;
            _bodyScroll.content = _bodyContentRt;
            _bodyScroll.horizontal = false;
            _bodyScroll.vertical = true;
            _bodyScroll.movementType = ScrollRect.MovementType.Clamped;
            _bodyScroll.scrollSensitivity = 30f;
            _bodyScroll.inertia = true;

            var sbGo = new GameObject("Scrollbar", typeof(RectTransform));
            sbGo.transform.SetParent(bodyBg.transform, false);
            var sbRt = sbGo.GetComponent<RectTransform>();
            sbRt.anchorMin = sbRt.anchorMax = new Vector2(0.5f, 0.5f);
            sbRt.sizeDelta = new Vector2(8f, 380f);
            sbRt.anchoredPosition = new Vector2(382f, -20f);
            var sbBg = sbGo.AddComponent<Image>();
            sbBg.color = new Color(0.18f, 0.14f, 0.10f, 0.9f);
            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var handleArea = new GameObject("SlidingArea", typeof(RectTransform));
            handleArea.transform.SetParent(sbGo.transform, false);
            var haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = haRt.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var hRt = handle.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.offsetMin = hRt.offsetMax = Vector2.zero;
            var hImg = handle.AddComponent<Image>();
            hImg.color = ColGoldDim;
            scrollbar.handleRect = hRt;
            scrollbar.targetGraphic = hImg;
            _bodyScroll.verticalScrollbar = scrollbar;
            try { _bodyScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent; }
            catch { }

            var closeGo = new GameObject("Close", typeof(RectTransform));
            closeGo.transform.SetParent(card.transform, false);
            var cEdge = new GameObject("CloseEdge", typeof(RectTransform));
            cEdge.transform.SetParent(closeGo.transform, false);
            var ceImg = cEdge.AddComponent<Image>();
            ceImg.color = ColGoldDim;
            ceImg.raycastTarget = false;
            var ceRt = cEdge.GetComponent<RectTransform>();
            ceRt.anchorMin = Vector2.zero;
            ceRt.anchorMax = Vector2.one;
            ceRt.offsetMin = new Vector2(-2f, -2f);
            ceRt.offsetMax = new Vector2(2f, 2f);
            var cimg = closeGo.AddComponent<Image>();
            cimg.color = new Color(0.22f, 0.17f, 0.12f, 1f);
            var crt = closeGo.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(200f, 48f);
            crt.anchoredPosition = new Vector2(0f, -320f);
            var cbtn = closeGo.AddComponent<Button>();
            cbtn.targetGraphic = cimg;
            var cl = MakeTmp(closeGo.transform, "CT", Vector2.zero, new Vector2(180f, 40f), 20,
                TextAlignmentOptions.Center, T("ui_RMR_Help_Close", "关闭", "Close"));
            cl.color = ColCream;
            StretchFull(cl.rectTransform, 0f);
            cbtn.onClick.AddListener(() =>
            {
                try { UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click); } catch { }
                Hide();
            });
        }
        #endregion

        #region --- Content / pages ---


        private void ApplyBodyText(string text)
        {
            if (_bodyText != null)
            {
                _bodyText.text = text ?? "";
                try
                {
                    _bodyText.ForceMeshUpdate();
                    float h = Math.Max(300f, _bodyText.preferredHeight + 32f);
                    if (_bodyContentRt != null)
                        _bodyContentRt.sizeDelta = new Vector2(0f, h);
                    var brt = _bodyText.rectTransform;
                    brt.sizeDelta = new Vector2(brt.sizeDelta.x, h - 8f);
                }
                catch { }
            }
            if (_bodyScroll != null)
                _bodyScroll.verticalNormalizedPosition = 1f;
        }
        #endregion

        #region --- Other helpers ---


        private static GameObject MakeSolid(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return go;
        }

        private static void StretchInset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            rt.sizeDelta = Vector2.zero;
        }

        private static void StretchFull(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static TextMeshProUGUI MakeTmp(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize,
            TextAlignmentOptions align, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            // Sharp Noto SDF material; never synthetic Bold on CJK.
            LogLikeMod.ApplyTmpFontPreservingSharpMaterial(tmp, LogLikeMod.DefFont_TMP);
            tmp.fontSize = fontSize;
            tmp.color = ColCream;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.fontStyle = FontStyles.Normal;
            tmp.richText = false;
            try { tmp.enableAutoSizing = false; } catch { /* older TMP */ }
            tmp.raycastTarget = false;
            tmp.text = text ?? "";
            return tmp;
        }

        private static Sprite ResolveArt(string[] keys, int startIndex = 0)
        {
            if (keys == null || LogLikeMod.ArtWorks == null)
                return null;
            for (int i = Math.Max(0, startIndex); i < keys.Length; i++)
            {
                string key = keys[i];
                if (string.IsNullOrEmpty(key))
                    continue;
                try
                {
                    if (LogLikeMod.ArtWorks.ContainsKey(key))
                    {
                        Sprite sprite = LogLikeMod.ArtWorks[key];
                        if (sprite != null)
                            return sprite;
                    }
                }
                catch { }
            }
            try
            {
                for (int i = Math.Max(0, startIndex); i < keys.Length; i++)
                {
                    string key = keys[i];
                    if (string.IsNullOrEmpty(key))
                        continue;
                    foreach (var pair in LogLikeMod.ArtWorks.dic)
                    {
                        if (pair.Key != null && pair.Value != null
                            && pair.Key.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                            return pair.Value;
                    }
                }
            }
            catch { }
            return null;
        }

        private void Select(int index)
        {
            _index = Math.Max(0, Math.Min(index, Pages.Length - 1));
            Page page = Pages[_index];

            if (_sectionTitle != null)
                _sectionTitle.text = T(page.NavKey, page.NavZh, page.NavEn);

            ApplyBodyText(T(page.BodyKey, page.BodyZh, page.BodyEn));
            // Banner artwork removed — stretched backgrounds looked bad.

            for (int i = 0; i < _navLabels.Count; i++)
            {
                bool on = i == _index;
                if (_navLabels[i] != null)
                    _navLabels[i].color = on ? ColGold : ColCream;
                if (i < _navFrames.Count && _navFrames[i] != null)
                    _navFrames[i].color = on ? ColNavOn : ColNavIdle;
            }
        }
        #endregion

    }
}
