// -----------------------------------------------------------------------------
// LOGLIKE core UI/data: LogCompendiumPanel
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\LogCompendiumPanel.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using HarmonyLib;
using LOR_DiceSystem;
using LOR_XML;
using RogueLike_Mod_Reborn;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace abcdcode_LOGLIKE_MOD
{
    // =========================================================================
    // Compendium UI (CN 图鉴 / EN Compendium / KR 도감)
    // C# types: Compendium* / LogCompendiumPanel (formerly LogAtlasPanel).
    // Localize key IDs may still use _Atlas (ui_RMR_Atlas_Title, ui_RMR_Hub_Atlas);
    // EN display text is "Compendium". Disk save: RMR_AtlasPermanentUnlocks.
    // Docs: docs/localization/GLOSSARY.md, CODE_TERMINOLOGY.md
    // =========================================================================

    #region --- Types: category / section / entry ---

    /// <summary>Compendium category tabs (key pages / combat / abnormality / E.G.O.).</summary>
    public enum CompendiumCategory
    {
        RoleBook,
        BattleCard,
        AbnormalityPage,
        EgoPage
    }

    /// <summary>Urban-stage filters for key/combat pages (abno/EGO use flat lists).</summary>
    public enum CompendiumSection
    {
        All = 0,
        Rumor,
        UrbanLegend,
        UrbanMyth,
        UrbanIllness,
        UrbanNightmare,
        UrbanStar,
        Impurity
    }

    /// <summary>One row in the Compendium grid (artwork + unlock state + metadata).</summary>
    public sealed class CompendiumEntry
    {
        public LorId Id;
        public string Title;
        public string Description;
        public Sprite Artwork;
        public bool Unlocked;
        public CompendiumCategory Category;
        public CompendiumSection Section;
        public SephirahType Floor;
    }

    #endregion

    /// <summary>
    /// Permanent Compendium UI (图鉴 / 도감). Formerly LogAtlasPanel.
    /// Localize key ids may still end with _Atlas; EN display uses Compendium. See docs/localization/CODE_TERMINOLOGY.md.
    /// </summary>
    public class LogCompendiumPanel : Singleton<LogCompendiumPanel>
    {
        #region --- Localization helpers & grid constants ---

        public const string LockedTitle = "?";

        private static string CompendiumUi(string zh, string en, string kr)
        {
            string lang = "";
            try { lang = TextDataModel.CurrentLanguage.ToString().ToLowerInvariant(); } catch { }
            if (lang.Contains("kr") || lang.Contains("ko")) return kr;
            if (lang.Contains("en")) return en;
            return zh;
        }

        private static string CompendiumUpgradeToggleLabelText()
        {
            return LocalizedUi("ui_RMR_Atlas_ShowUpgraded",
                CompendiumUi("显示升级版", "Show upgraded", "강화판 표시"));
        }

        private static readonly CompendiumSection[] Sections =
        {
            CompendiumSection.All,
            CompendiumSection.Rumor,
            CompendiumSection.UrbanLegend,
            CompendiumSection.UrbanMyth,
            CompendiumSection.UrbanIllness,
            CompendiumSection.UrbanNightmare,
            CompendiumSection.UrbanStar,
            CompendiumSection.Impurity
        };

        private static readonly CompendiumCategory[] Categories =
        {
            CompendiumCategory.RoleBook,
            CompendiumCategory.BattleCard,
            CompendiumCategory.AbnormalityPage,
            CompendiumCategory.EgoPage
        };

        // Selected A-A archive wall: left categories, center 4x5 cards, right detail.
        private const int CenterCols = 4;
        private const int CenterRows = 5;
        private const int CenterPageSize = CenterCols * CenterRows;

        #endregion

        #region --- UI fields (chrome, grid, detail, hub) ---

        public GameObject root;
        /// <summary>Full-screen shell under RMR overlay (parent of <see cref="root"/> content).</summary>
        private GameObject _hostRoot;
        private readonly List<LogAtlasTile> tiles = new List<LogAtlasTile>();
        private readonly List<TextMeshProUGUI> sectionLabels = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> categoryLabels = new List<TextMeshProUGUI>();
        private readonly List<Image> categoryFrames = new List<Image>();
        private readonly List<Image> sectionFrames = new List<Image>();
        private GameObject upgradeToggleRoot;
        private Image upgradeToggleFrame;
        private TextMeshProUGUI upgradeToggleLabel;
        private TextMeshProUGUI headerTitle;
        private TextMeshProUGUI headerCount;
        private TextMeshProUGUI sectionHeader;
        private TextMeshProUGUI pageLabel;
        private Button pagePrevBtn;
        private Button pageNextBtn;
        private int currentPage;
        private CompendiumSection currentSection = CompendiumSection.All;
        private CompendiumCategory currentCategory = CompendiumCategory.RoleBook;
        private TextMeshProUGUI _emptyHint;
        private bool showUpgradedBattleCards;

        // Detail panel fields
        private GameObject detailPanelRoot;
        private Image detailArtwork;
        private TextMeshProUGUI detailTitle;
        private TextMeshProUGUI detailDescription;
        private TextMeshProUGUI detailMeta;
        private CompendiumEntry currentDetailEntry;
        private bool _openedFromHub;
        private GameObject _hubCloseBtn;
        private Action _onHubClose;

        /// <summary>True while Compendium UI is shown (hub or legacy).</summary>
        public bool IsVisible =>
            (root != null && root.activeInHierarchy)
            || (_hostRoot != null && _hostRoot.activeInHierarchy);

        // Scheme A palette + BG2 (金晕暗室)
        private static readonly Color ColGold = new Color(0.773f, 0.608f, 0.333f, 1f);
        private static readonly Color ColGoldDim = new Color(0.42f, 0.31f, 0.18f, 1f);
        private static readonly Color ColCream = new Color(0.925f, 0.878f, 0.784f, 1f);
        private static readonly Color ColMuted = new Color(0.604f, 0.549f, 0.459f, 1f);
        private static readonly Color ColPanel = new Color(0.078f, 0.067f, 0.055f, 0.96f);
        private static readonly Color ColNavIdle = new Color(0.118f, 0.094f, 0.071f, 0.96f);
        private static readonly Color ColNavOn = new Color(0.227f, 0.165f, 0.086f, 0.98f);
        private static readonly Color ColTile = new Color(0.110f, 0.090f, 0.071f, 0.96f);
        private static readonly Color ColTileLocked = new Color(0.078f, 0.071f, 0.063f, 0.92f);
        private static readonly Color ColBgVoid = new Color(0.020f, 0.016f, 0.012f, 1f);
        private static readonly Color ColBgGlow = new Color(0.102f, 0.082f, 0.055f, 0.55f);

        #endregion

        #region --- Lifecycle: Init / ShowFromHub / Close ---

        public static GameObject GetLogUIObj(int index)
        {
            GameObject source = (UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel).EditPanel.BattleCardPanel.gameObject;
            GameObject root = UnityEngine.Object.Instantiate<Transform>(source.transform, source.transform.parent).gameObject;
            UnityEngine.Object.Destroy(root.GetComponent<UIBattleSettingPanel>());
            for (int i = 0; i < root.transform.childCount; ++i)
                UnityEngine.Object.Destroy(root.transform.GetChild(i).gameObject);
            root.SetActive(true);
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = source.transform.localScale;
            root.GetComponent<Canvas>().enabled = true;
            root.GetComponent<Canvas>().sortingOrder += index;
            root.GetComponent<CanvasGroup>().alpha = 1f;
            root.GetComponent<CanvasGroup>().blocksRaycasts = true;
            root.GetComponent<CanvasGroup>().interactable = true;
            return root;
        }

        public void Init()
        {
            if (root != null)
            {
                root.SetActive(true);
                if (_hostRoot != null)
                    _hostRoot.SetActive(true);
                EnsureHubCloseButton();
                return;
            }

            // Prefer BattleSetting clone only when that panel is live; else overlay.
            root = TryCreateBattleSettingCloneRoot();
            if (root == null)
                root = CreateOverlayCanvasRoot();
            BuildArchiveWallChrome();
            EnsureHubCloseButton();
            root.SetActive(true);
        }

        /// <summary>
        /// Open Compendium from RMR start hub (invitation). Not from battle prepare tabs.
        /// Always builds on RMR overlay — never depends on UIBattleSettingPanel.
        /// </summary>
        public void ShowFromHub(Action onClose = null)
        {
            _openedFromHub = true;
            _onHubClose = onClose;
            try { LogLikeMod.InvalidateTmpFontCache(); } catch { }
            try
            {
                // Hub can open before CreatePlayer; always rehydrate permanent Compendium from disk first.
                LogueBookModels.EnsureCompendiumUnlocks();
                LogueBookModels.LoadPermanentCompendiumData();
                LogueBookModels.SyncCurrentInventoryToPermanentAtlas();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR Atlas] EnsureCompendiumUnlocks/LoadPermanent: " + ex.Message);
            }

            try
            {
                // Full rebuild every open — stale BattleSetting clones / half-built hosts break hub.
                DestroyCompendiumUiTree();
                currentPage = 0;

                root = CreateOverlayCanvasRoot();
                if (root == null)
                    throw new InvalidOperationException("CreateOverlayCanvasRoot returned null");

                BuildArchiveWallChrome();
                EnsureHubCloseButton();

                if (_hostRoot != null)
                {
                    _hostRoot.SetActive(true);
                    try { _hostRoot.transform.SetAsLastSibling(); } catch { }
                }
                root.SetActive(true);
                try { root.transform.SetAsLastSibling(); } catch { }

                // Bring overlay canvas above invitation UI.
                try
                {
                    Transform overlay = RMRRealizationLaunchHost.GetOrCreateOverlayRoot();
                    if (overlay != null)
                    {
                        overlay.SetAsLastSibling();
                        if (overlay.GetComponent<GraphicRaycaster>() == null)
                            overlay.gameObject.AddComponent<GraphicRaycaster>();
                    }
                }
                catch { }

                try
                {
                    // Ensure book/card localize before building titles.
                    try
                    {
                        string lang = TextDataModel.CurrentLanguage.ToString();
                        LogLikeMod.RefreshVanillaBookAndPassiveLocalize(lang, "Atlas.ShowFromHub");
                        LogLikeMod.EnsureLocalizedFonts("Atlas.ShowFromHub", repairActiveUi: true);
                    }
                    catch { }

                    UpdateTiles();
                    int total = 0;
                    try { total = BuildEntries(false).Count; } catch { }
                    Debug.Log($"[RMR Atlas] Opened from start hub. entries≈{total} tiles={tiles.Count} "
                        + $"RoleBooks={LogueBookModels.CompendiumUnlockedRoleBooks?.Count ?? 0} "
                        + $"BattleCards={LogueBookModels.CompendiumUnlockedBattleCards?.Count ?? 0} "
                        + $"Abno={LogueBookModels.CompendiumUnlockedAbnormalityPages?.Count ?? 0} "
                        + $"Ego={LogueBookModels.CompendiumUnlockedEgoPages?.Count ?? 0} "
                        + $"host={(_hostRoot != null ? _hostRoot.name : "null")} root={(root != null ? root.name : "null")}");
                }
                catch (Exception tileEx)
                {
                    Debug.LogWarning("[RMR Atlas] UpdateTiles failed (UI still shown): " + tileEx);
                    try { ShowEmptyHint(CompendiumUi("图鉴数据加载失败，请查看日志。", "Failed to load atlas data. Check the log.", "도감 데이터를 불러오지 못했습니다. 로그를 확인하세요.")); } catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[RMR Atlas] ShowFromHub failed: " + ex);
                try { DestroyCompendiumUiTree(); } catch { }
                _openedFromHub = false;
                Action cb = _onHubClose;
                _onHubClose = null;
                try { cb?.Invoke(); } catch { }
                // Do not rethrow — hub already restored.
            }
        }

        public void CloseFromHub()
        {
            _openedFromHub = false;
            // Hide shell so black host does not cover restored hub.
            if (_hostRoot != null)
                _hostRoot.SetActive(false);
            else if (root != null)
                root.SetActive(false);
            Action cb = _onHubClose;
            _onHubClose = null;
            try { cb?.Invoke(); } catch { }
        }

        #endregion

        #region --- UI construction: chrome, nav, cards, page controls ---

        private void BuildArchiveWallChrome()
        {
            // Scheme A-A on BG2: thin content plate (no ornate ShopGoodRewardFrame oval).
            Image wall = CreateSolidImage(root.transform, "ArchivePlate", new Vector2(0f, 10f),
                new Vector2(1480f, 700f), new Color(0.055f, 0.045f, 0.035f, 0.82f), false);
            try { wall.transform.SetAsFirstSibling(); } catch { }
            Image wallEdge = CreateSolidImage(root.transform, "ArchivePlateEdge", new Vector2(0f, 10f),
                new Vector2(1484f, 704f), new Color(ColGoldDim.r, ColGoldDim.g, ColGoldDim.b, 0.55f), false);
            try { wallEdge.transform.SetAsFirstSibling(); } catch { }

            headerTitle = CreateLabel(root.transform, new Vector2(0f, 380f), 28, ColGold, TextAlignmentOptions.Center);
            headerTitle.text = LocalizedUi("ui_RMR_Atlas_Title", "\u6c38\u4e45\u56fe\u9274");
            try { headerTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 40f); } catch { }
            TextMeshProUGUI sub = CreateLabel(root.transform, new Vector2(0f, 350f), 13, ColGoldDim, TextAlignmentOptions.Center);
            sub.text = "PERMANENT ARCHIVE  \u00b7  LIBRARY ATLAS";
            try { sub.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 24f); } catch { }
            headerCount = CreateLabel(root.transform, new Vector2(560f, 380f), 13, ColMuted, TextAlignmentOptions.Right);
            headerCount.text = "0 / 0";
            try { headerCount.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 24f); } catch { }

            Image rule = CreateSolidImage(root.transform, "HeaderRule", new Vector2(0f, 332f),
                new Vector2(1200f, 1.5f), new Color(ColGoldDim.r, ColGoldDim.g, ColGoldDim.b, 0.5f), false);

            CreateLeftNav();
            CreateBattleCardUpgradeToggle();
            CreateCenterCards();
            CreatePageControls();
            CreateDetailPanel();
        }

        private void DestroyCompendiumUiTree()
        {
            try
            {
                if (_hostRoot != null)
                {
                    UnityEngine.Object.Destroy(_hostRoot);
                }
                else if (root != null)
                {
                    Transform p = null;
                    try { p = root.transform != null ? root.transform.parent : null; } catch { }
                    if (p != null && p.name == "LogAtlasOverlayHost")
                        UnityEngine.Object.Destroy(p.gameObject);
                    else
                        UnityEngine.Object.Destroy(root);
                }
            }
            catch { }
            _hostRoot = null;
            root = null;
            tiles.Clear();
            sectionLabels.Clear();
            categoryLabels.Clear();
            categoryFrames.Clear();
            sectionFrames.Clear();
            upgradeToggleRoot = null;
            upgradeToggleFrame = null;
            upgradeToggleLabel = null;
            detailPanelRoot = null;
            detailArtwork = null;
            detailTitle = null;
            detailDescription = null;
            detailMeta = null;
            headerTitle = null;
            headerCount = null;
            sectionHeader = null;
            pageLabel = null;
            pagePrevBtn = null;
            pageNextBtn = null;
            _hubCloseBtn = null;
        }

        public bool TryHandleBack()
        {
            if (!IsVisible && (root == null || !root.activeSelf))
                return false;
            if (_openedFromHub)
            {
                CloseFromHub();
                return true;
            }
            SetActive(false);
            return true;
        }

        private static GameObject TryCreateBattleSettingCloneRoot()
        {
            try
            {
                // Only use when BattleSetting is actually the live UI phase.
                // At invitation hub, clone parents under inactive prepare tree → invisible.
                if (UI.UIController.Instance == null)
                    return null;
                var panel = UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel;
                if (panel == null || panel.gameObject == null || !panel.gameObject.activeInHierarchy)
                    return null;
                if (panel.EditPanel == null || panel.EditPanel.BattleCardPanel == null)
                    return null;
                return GetLogUIObj(2);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR Atlas] BattleSetting clone failed, using overlay: " + ex.Message);
                return null;
            }
        }

        private GameObject CreateOverlayCanvasRoot()
        {
            Transform parent = null;
            try { parent = RMRRealizationLaunchHost.GetOrCreateOverlayRoot(); } catch { }

            // Full-screen host under overlay canvas (must stay under Canvas for raycasts).
            GameObject host = new GameObject("LogAtlasOverlayHost", typeof(RectTransform));
            if (parent != null)
            {
                host.transform.SetParent(parent, false);
                try
                {
                    if (parent.GetComponent<GraphicRaycaster>() == null)
                        parent.gameObject.AddComponent<GraphicRaycaster>();
                    parent.SetAsLastSibling();
                }
                catch { }
            }
            else
            {
                // Last resort: own DontDestroyOnLoad canvas
                var canvasGo = new GameObject("LogAtlasCanvas", typeof(RectTransform));
                UnityEngine.Object.DontDestroyOnLoad(canvasGo);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9000;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
                host.transform.SetParent(canvasGo.transform, false);
            }

            var hrt = host.GetComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero;
            hrt.anchorMax = Vector2.one;
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;
            hrt.localScale = Vector3.one;
            hrt.localPosition = Vector3.zero;
            hrt.SetAsLastSibling();

            // BG2 · 金晕暗室 — layered solid Images (no CSS / no new assets).
            var bg = host.AddComponent<Image>();
            bg.color = ColBgVoid;
            bg.raycastTarget = true;
            BuildBg2Atmosphere(host.transform);

            // Content host centered — child UI uses absolute offsets around origin.
            var content = new GameObject("CompendiumContent", typeof(RectTransform));
            content.transform.SetParent(host.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(1600f, 900f);
            crt.anchoredPosition = Vector2.zero;
            crt.localScale = Vector3.one;
            crt.localPosition = Vector3.zero;

            _hostRoot = host;
            return content;
        }

        /// <summary>
        /// BG2 vignette: warm center glow + edge crush, Scheme A archive lighting.
        /// </summary>
        private static void BuildBg2Atmosphere(Transform host)
        {
            // Soft warm pool in the middle of the screen.
            Image glow = CreateSolidImage(host, "Bg2Glow", Vector2.zero, new Vector2(1400f, 900f), ColBgGlow, false);
            try
            {
                var grt = glow.rectTransform;
                grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
                grt.anchoredPosition = new Vector2(0f, 20f);
            }
            catch { }

            // Edge vignette plates (approximate radial falloff).
            CreateEdgeShade(host, "VignetteTop", new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0.22f),
                new Color(0f, 0f, 0f, 0.55f));
            CreateEdgeShade(host, "VignetteBottom", new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0.22f),
                new Color(0f, 0f, 0f, 0.62f));
            CreateEdgeShade(host, "VignetteLeft", new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(0.18f, 1f),
                new Color(0f, 0f, 0f, 0.45f));
            CreateEdgeShade(host, "VignetteRight", new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(0.18f, 1f),
                new Color(0f, 0f, 0f, 0.45f));
        }

        private static void CreateEdgeShade(Transform parent, string name, Vector2 pivot, Vector2 offsetMin, Vector2 sizeFrac, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            // sizeFrac.x/y used as anchor span from pivot edge
            if (pivot.y >= 0.99f) // top strip
            {
                rt.anchorMin = new Vector2(0f, 1f - sizeFrac.y);
                rt.anchorMax = new Vector2(1f, 1f);
            }
            else if (pivot.y <= 0.01f) // bottom
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, sizeFrac.y);
            }
            else if (pivot.x <= 0.01f) // left
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(sizeFrac.x, 1f);
            }
            else // right
            {
                rt.anchorMin = new Vector2(1f - sizeFrac.x, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
            }
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = pivot;
        }

        /// <summary>Solid UI panel — no ShopGoodRewardFrame (that frame fought Scheme A look).</summary>
        private static Image CreateSolidImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = null;
            img.color = color;
            img.raycastTarget = raycast;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one;
            return img;
        }

        /// <summary>
        /// Solid panel image — Scheme A: pure color, optional thin gold edge child.
        /// </summary>
        private static Image CreatePanelImage(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            return CreatePanelImage(parent, pos, size, color, false);
        }

        private static Image CreatePanelImage(Transform parent, Vector2 pos, Vector2 size, Color color, bool goldEdge)
        {
            Image img = CreateSolidImage(parent, "CompendiumPanel", pos, size, color, true);
            if (goldEdge)
            {
                Image edge = CreateSolidImage(img.transform, "GoldEdge", Vector2.zero,
                    new Vector2(size.x + 2f, size.y + 2f),
                    new Color(ColGoldDim.r, ColGoldDim.g, ColGoldDim.b, 0.75f), false);
                edge.transform.SetAsFirstSibling();
                try
                {
                    var ert = edge.rectTransform;
                    ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0.5f);
                    ert.anchoredPosition = Vector2.zero;
                }
                catch { }
            }
            return img;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, Vector2 pos, int size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("CompendiumLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var rt = go.GetComponent<RectTransform>();
            // Center-point labels only — never stretch to parent (blocks raycasts if raycastTarget leaks).
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(80f, size * 12f), size + 16f);
            rt.anchoredPosition = pos;
            try { LogLikeMod.ApplyTmpFontPreservingSharpMaterial(tmp, LogLikeMod.DefFont_TMP); } catch { }
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = FontStyles.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void EnsureHubCloseButton()
        {
            if (root == null)
                return;
            if (!_openedFromHub)
            {
                if (_hubCloseBtn != null)
                    _hubCloseBtn.SetActive(false);
                return;
            }
            if (_hubCloseBtn == null)
            {
                // Bottom-right — must not sit on paging (was y=-320 overlapping "1/N").
                Image edge = CreatePanelImage(root.transform, new Vector2(520f, -360f), new Vector2(224f, 52f), ColGoldDim);
                edge.raycastTarget = false;
                Image img = CreatePanelImage(root.transform, new Vector2(520f, -360f), new Vector2(216f, 44f),
                    new Color(0.18f, 0.14f, 0.10f, 1f));
                _hubCloseBtn = img.gameObject;
                Button btn = _hubCloseBtn.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(() =>
                {
                    try { UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click); } catch { }
                    CloseFromHub();
                });
                TextMeshProUGUI backLabel = CreateLabel(_hubCloseBtn.transform, Vector2.zero, 20, ColCream, TextAlignmentOptions.Center);
                backLabel.text = CompendiumUi("\u8fd4\u56de", "Back", "\ub3cc\uc544\uac00\uae30"); // 返回 / Back / 돌아가기
                backLabel.raycastTarget = false;
                try
                {
                    var lrt = backLabel.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero;
                    lrt.offsetMax = Vector2.zero;
                    lrt.anchoredPosition = Vector2.zero;
                    lrt.sizeDelta = Vector2.zero;
                }
                catch { }
            }
            _hubCloseBtn.SetActive(true);
            try { _hubCloseBtn.transform.SetAsLastSibling(); } catch { }
        }

        /// <summary>
        /// Left nav: 4 fixed category rails (角色 / 战斗 / 异想体 / EGO战斗) + chapter/floor sections.
        /// </summary>
        private void CreateLeftNav()
        {
            // Scheme A left rails: solid bar + gold accent strip when selected.
            for (int i = 0; i < Categories.Length; i++)
            {
                CompendiumCategory category = Categories[i];
                Image image = CreateSolidImage(root.transform, "CatRail",
                    new Vector2(-580f, 270f - i * 50f), new Vector2(200f, 42f), ColNavIdle, true);
                categoryFrames.Add(image);

                Image accent = CreateSolidImage(image.transform, "Accent",
                    new Vector2(-98f, 0f), new Vector2(3f, 34f), ColGoldDim, false);
                accent.gameObject.name = "CatAccent";

                Button button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener((UnityAction)(() => SelectCategory(category)));
                TextMeshProUGUI label = CreateLabel(image.transform, new Vector2(6f, 0f), 15, ColCream, TextAlignmentOptions.Left);
                string numeral = i == 0 ? "I" : (i == 1 ? "II" : (i == 2 ? "III" : "IV"));
                label.text = numeral + "  \u00b7  " + GetCategoryName(category);
                try
                {
                    var lrt = label.GetComponent<RectTransform>();
                    lrt.anchorMin = new Vector2(0f, 0.5f);
                    lrt.anchorMax = new Vector2(1f, 0.5f);
                    lrt.pivot = new Vector2(0f, 0.5f);
                    lrt.anchoredPosition = new Vector2(16f, 0f);
                    lrt.sizeDelta = new Vector2(-24f, 36f);
                }
                catch { }
                categoryLabels.Add(label);
            }

            sectionHeader = CreateLabel(root.transform, new Vector2(-580f, 50f), 12, ColGoldDim, TextAlignmentOptions.Left);
            sectionHeader.text = LocalizedUi("ui_RMR_Atlas_Progress", "\u83b7\u5f97\u9636\u6bb5");
            try
            {
                var srt = sectionHeader.GetComponent<RectTransform>();
                srt.pivot = new Vector2(0f, 0.5f);
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(-680f, 50f);
                srt.sizeDelta = new Vector2(200f, 22f);
            }
            catch { }

            for (int i = 0; i < Sections.Length; i++)
            {
                CompendiumSection section = Sections[i];
                Image image = CreateSolidImage(root.transform, "SecRail",
                    new Vector2(-580f, 18f - i * 34f), new Vector2(200f, 28f), ColNavIdle, true);
                sectionFrames.Add(image);
                Image accent = CreateSolidImage(image.transform, "Accent",
                    new Vector2(-98f, 0f), new Vector2(3f, 22f), ColGoldDim, false);
                accent.gameObject.name = "SecAccent";

                Button button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener((UnityAction)(() => SelectSection(section)));
                TextMeshProUGUI label = CreateLabel(image.transform, new Vector2(6f, 0f), 13, ColMuted, TextAlignmentOptions.Left);
                label.text = GetSectionName(section);
                try
                {
                    var lrt = label.GetComponent<RectTransform>();
                    lrt.anchorMin = new Vector2(0f, 0.5f);
                    lrt.anchorMax = new Vector2(1f, 0.5f);
                    lrt.pivot = new Vector2(0f, 0.5f);
                    lrt.anchoredPosition = new Vector2(16f, 0f);
                    lrt.sizeDelta = new Vector2(-24f, 26f);
                }
                catch { }
                sectionLabels.Add(label);
            }
        }

        private void CreateCenterCards()
        {
            // A-A wall: 4 x 5 solid tiles with thin gold edge (Scheme A mockup).
            float startX = -260f;
            float startY = 240f;
            float stepX = 148f;
            float stepY = 108f;
            for (int row = 0; row < CenterRows; row++)
            {
                for (int col = 0; col < CenterCols; col++)
                {
                    Vector2 pos = new Vector2(startX + col * stepX, startY - row * stepY);
                    Image edge = CreateSolidImage(root.transform, "TileEdge", pos,
                        new Vector2(140f, 100f), new Color(ColGoldDim.r, ColGoldDim.g, ColGoldDim.b, 0.45f), false);
                    Image image = CreateSolidImage(edge.transform, "TileFace", Vector2.zero,
                        new Vector2(136f, 96f), ColTile, true);
                    try
                    {
                        var irt = image.rectTransform;
                        irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                        irt.anchoredPosition = Vector2.zero;
                    }
                    catch { }
                    tiles.Add(image.gameObject.AddComponent<LogAtlasTile>());
                }
            }

            _emptyHint = CreateLabel(root.transform, new Vector2(-40f, 40f), 16, ColMuted, TextAlignmentOptions.Center);
            _emptyHint.text = "";
            try { _emptyHint.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 80f); } catch { }
        }

        private void ShowEmptyHint(string msg)
        {
            if (_emptyHint == null)
                return;
            _emptyHint.text = msg ?? "";
            _emptyHint.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        }

        private void CreatePageControls()
        {
            // Center under card wall. Return button lives separately at bottom-right.
            // CRITICAL: never use anchors (0,0)-(1,1) on root — CreateText_TMP stretch blocked all clicks.
            const float pagingY = -300f;

            pageLabel = CreateLabel(root.transform, new Vector2(-30f, pagingY), 16, ColCream, TextAlignmentOptions.Center);
            pageLabel.text = "1 / 1";
            try { pageLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 28f); } catch { }

            Image prevImg = CreatePanelImage(root.transform, new Vector2(-130f, pagingY), new Vector2(56f, 34f),
                new Color(0.18f, 0.14f, 0.10f, 1f));
            pagePrevBtn = prevImg.gameObject.AddComponent<Button>();
            pagePrevBtn.targetGraphic = prevImg;
            pagePrevBtn.onClick = new Button.ButtonClickedEvent();
            pagePrevBtn.onClick.AddListener((UnityAction)(() =>
            {
                if (currentPage > 0)
                {
                    currentPage--;
                    UpdateTiles();
                }
            }));
            TextMeshProUGUI prevTxt = CreateLabel(prevImg.transform, Vector2.zero, 18, ColCream, TextAlignmentOptions.Center);
            prevTxt.text = "<";
            try
            {
                var prt = prevTxt.GetComponent<RectTransform>();
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.offsetMin = prt.offsetMax = Vector2.zero;
                prt.sizeDelta = Vector2.zero;
            }
            catch { }

            Image nextImg = CreatePanelImage(root.transform, new Vector2(70f, pagingY), new Vector2(56f, 34f),
                new Color(0.18f, 0.14f, 0.10f, 1f));
            pageNextBtn = nextImg.gameObject.AddComponent<Button>();
            pageNextBtn.targetGraphic = nextImg;
            pageNextBtn.onClick = new Button.ButtonClickedEvent();
            pageNextBtn.onClick.AddListener((UnityAction)(() =>
            {
                currentPage++;
                UpdateTiles();
            }));
            TextMeshProUGUI nextTxt = CreateLabel(nextImg.transform, Vector2.zero, 18, ColCream, TextAlignmentOptions.Center);
            nextTxt.text = ">";
            try
            {
                var nrt = nextTxt.GetComponent<RectTransform>();
                nrt.anchorMin = Vector2.zero;
                nrt.anchorMax = Vector2.one;
                nrt.offsetMin = nrt.offsetMax = Vector2.zero;
                nrt.sizeDelta = Vector2.zero;
            }
            catch { }
        }

        private void CreateDetailPanel()
        {
            // Scheme A right reading desk.
            Image deskEdge = CreateSolidImage(root.transform, "DeskEdge", new Vector2(540f, 30f),
                new Vector2(304f, 524f), new Color(ColGoldDim.r, ColGoldDim.g, ColGoldDim.b, 0.5f), false);
            Image desk = CreateSolidImage(root.transform, "Desk", new Vector2(540f, 30f),
                new Vector2(296f, 516f), ColPanel, true);
            detailPanelRoot = desk.gameObject;

            detailTitle = CreateLabel(detailPanelRoot.transform, new Vector2(0f, 220f), 18, ColGold, TextAlignmentOptions.Center);
            detailTitle.text = "";
            try { detailTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(270f, 36f); } catch { }

            detailMeta = CreateLabel(detailPanelRoot.transform, new Vector2(0f, 190f), 12, ColMuted, TextAlignmentOptions.Center);
            detailMeta.text = "";
            try { detailMeta.GetComponent<RectTransform>().sizeDelta = new Vector2(270f, 24f); } catch { }

            detailArtwork = CreateSolidImage(detailPanelRoot.transform, "Art", new Vector2(0f, 80f),
                new Vector2(150f, 150f), new Color(0.08f, 0.07f, 0.06f, 1f), false);
            detailArtwork.preserveAspect = true;

            detailDescription = CreateLabel(detailPanelRoot.transform, new Vector2(0f, -130f), 13, ColCream, TextAlignmentOptions.TopLeft);
            detailDescription.text = "";
            detailDescription.enableWordWrapping = true;
            detailDescription.overflowMode = TextOverflowModes.Overflow;
            try
            {
                var rt = detailDescription.GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = new Vector2(260f, 240f);
            }
            catch { }

            detailPanelRoot.SetActive(true);
        }

        /// <summary>
        /// Populates the detail panel with information for the given atlas entry.
        /// </summary>
        #endregion

        #region --- Detail panel (right side) ---

        public void ShowDetail(CompendiumEntry entry)
        {
            currentDetailEntry = entry;
            if (entry == null || detailPanelRoot == null || detailTitle == null || detailArtwork == null || detailDescription == null)
                return;

            try
            {
                if (!entry.Unlocked)
                {
                    detailTitle.text = "???";
                    if (detailMeta != null)
                        detailMeta.text = GetCategoryName(entry.Category);
                    detailArtwork.sprite = LogLikeMod.ArtWorks.ContainsKey("ItemNotFoundIcon") ? LogLikeMod.ArtWorks["ItemNotFoundIcon"] : null;
                    detailArtwork.enabled = detailArtwork.sprite != null;
                    ApplyArtworkLayout(detailArtwork, CompendiumCategory.RoleBook, true);
                    detailDescription.text = CompendiumUi("\u5c1a\u672a\u89e3\u9501\u3002", "Not unlocked yet.", "\uc544\uc9c1 \ud574\uae08\ub418\uc9c0 \uc54a\uc558\uc2b5\ub2c8\ub2e4.");
                    return;
                }

                detailTitle.text = entry.Title ?? "?";
                if (detailMeta != null)
                    detailMeta.text = BuildDetailMetaLine(entry);
                Sprite art = entry.Artwork;
                if (art == null && LogLikeMod.ArtWorks.ContainsKey("ItemNotFoundIcon"))
                    art = LogLikeMod.ArtWorks["ItemNotFoundIcon"];
                detailArtwork.sprite = art;
                if (art != null)
                {
                    detailArtwork.color = Color.white;
                    detailArtwork.enabled = true;
                }
                else
                {
                    detailArtwork.color = new Color(0.12f, 0.1f, 0.08f, 1f);
                    detailArtwork.enabled = true;
                }
                ApplyArtworkLayout(detailArtwork, entry.Category, true);
                detailDescription.text = entry.Description ?? "";

                // Enrich detail with category-specific extra info (no raw LorId dump).
                try
                {
                    string extraInfo = BuildDetailExtraInfo(entry);
                    if (!string.IsNullOrEmpty(extraInfo))
                        detailDescription.text = extraInfo;
                }
                catch { /* detail enrichment is optional */ }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RMR Atlas] ShowDetail error: {ex.Message}");
            }
        }

        private static string BuildDetailMetaLine(CompendiumEntry entry)
        {
            if (entry == null)
                return "";
            string cat = GetCategoryName(entry.Category);
            if (entry.Category == CompendiumCategory.AbnormalityPage || entry.Category == CompendiumCategory.EgoPage)
            {
                string floor = RMRRealizationManager.FloorDisplayNames != null
                    && RMRRealizationManager.FloorDisplayNames.TryGetValue(entry.Floor, out string fn)
                    ? fn : entry.Floor.ToString();
                return cat + "  \u00b7  " + floor;
            }
            return cat + "  \u00b7  " + GetSectionName(entry.Section);
        }

        /// <summary>
        /// Builds enriched detail text for the detail panel, based on category.
        /// </summary>
        private static string BuildDetailExtraInfo(CompendiumEntry entry)
        {
            if (entry == null || !entry.Unlocked)
                return null;

            switch (entry.Category)
            {
                case CompendiumCategory.RoleBook:
                    return BuildRoleBookDetail(entry.Id);
                case CompendiumCategory.BattleCard:
                    return BuildBattleCardDetail(entry.Id, entry.Title);
                case CompendiumCategory.AbnormalityPage:
                    return BuildAbnormalityPageDetail(entry.Id);
                case CompendiumCategory.EgoPage:
                    return BuildEgoPageDetail(entry.Id);
                default:
                    return null;
            }
        }

        private static string BuildRoleBookDetail(LorId id)
        {
            try
            {
                BookXmlInfo book = Singleton<BookXmlList>.Instance.GetData(id);
                if (book == null)
                    return null;
                var lines = new List<string>();
                string displayName = RewardingModel.GetLocalizedBookName(book);
                if (string.IsNullOrEmpty(displayName) && book.CharacterSkin != null && book.CharacterSkin.Count > 0)
                    displayName = book.CharacterSkin[0];
                if (string.IsNullOrEmpty(displayName))
                    displayName = id.ToString();
                lines.Add(CompendiumUi("名称: ", "Name: ", "이름: ") + displayName);
                lines.Add("HP: " + book.EquipEffect.Hp.ToString());
                lines.Add(CompendiumUi("速度: ", "Speed: ", "속도: ") + book.EquipEffect.SpeedMin.ToString() + "-" + book.EquipEffect.Speed.ToString());
                lines.Add(CompendiumUi("速度骰子: ", "Speed Dice: ", "속도 주사위: ") + book.EquipEffect.SpeedDiceNum.ToString());
                lines.Add(CompendiumUi("耐性: 斩", "Resist: Slash ", "내성: 참격 ") + ResToText(book.EquipEffect.SResist)
                    + CompendiumUi(" 突", " Pierce ", " 관통 ") + ResToText(book.EquipEffect.PResist)
                    + CompendiumUi(" 打", " Blunt ", " 타격 ") + ResToText(book.EquipEffect.HResist));
                lines.Add(CompendiumUi("混乱耐性: 斩", "Stagger Resist: Slash ", "흐트러짐 내성: 참격 ") + ResToText(book.EquipEffect.SBResist)
                    + CompendiumUi(" 突", " Pierce ", " 관통 ") + ResToText(book.EquipEffect.PBResist)
                    + CompendiumUi(" 打", " Blunt ", " 타격 ") + ResToText(book.EquipEffect.HBResist));
                if (book.EquipEffect.PassiveList != null && book.EquipEffect.PassiveList.Count > 0)
                {
                    lines.Add(CompendiumUi("被动:", "Passives:", "패시브:"));
                    foreach (LorId pid in book.EquipEffect.PassiveList)
                    {
                        string pName = RewardingModel.GetPassiveName(pid);
                        string pDesc = RewardingModel.SanitizeDisplayText(Singleton<PassiveDescXmlList>.Instance.GetDesc(pid));
                        if (!string.IsNullOrEmpty(pName))
                            lines.Add("  • " + pName + ": " + (pDesc ?? ""));
                        else
                            lines.Add("  • " + pid.ToString());
                    }
                }
                return string.Join("\n", lines.ToArray());
            }
            catch
            {
                return null;
            }
        }

        private static string BuildBattleCardDetail(LorId id, string title)
        {
            try
            {
                DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(id, true);
                if (card == null)
                    return null;
                var lines = new List<string>();
                if (!string.IsNullOrEmpty(title))
                    lines.Add(title);
                lines.Add("\u8d39\u7528: " + card.Spec.Cost.ToString());
                lines.Add("\u7ae0\u8282: " + card.Chapter.ToString());
                if (!string.IsNullOrEmpty(card.Script))
                    lines.Add("\u80fd\u529b: " + card.Script);
                if (card.DiceBehaviourList != null && card.DiceBehaviourList.Count > 0)
                {
                    lines.Add("\u9ab0\u5b50:");
                    for (int i = 0; i < card.DiceBehaviourList.Count; i++)
                    {
                        DiceBehaviour dice = card.DiceBehaviourList[i];
                        string script = string.IsNullOrEmpty(dice.Script) ? "" : " / " + dice.Script;
                        lines.Add("  " + (i + 1).ToString() + ". " + GetDiceDetailText(dice.Detail) + " " + dice.Min.ToString() + "-" + dice.Dice.ToString() + script);
                    }
                }
                return string.Join("\n", lines.ToArray());
            }
            catch
            {
                return null;
            }
        }

        private static string BuildAbnormalityPageDetail(LorId id)
        {
            try
            {
                RewardPassiveInfo info = Singleton<RewardPassivesList>.Instance.GetPassiveInfo(id);
                if (info == null)
                    return null;
                EmotionCardXmlInfo card = LogLikeMod.GetRegisteredPickUpXml(info);
                AbnormalityCard desc = GetAbnormalityCardDesc(card, info);
                var lines = new List<string>();
                string name = (desc != null && !string.IsNullOrEmpty(desc.cardName)) ? desc.cardName : info.script;
                string ability = desc != null ? desc.abilityDesc : (info.script ?? "");
                string flavor = desc != null ? desc.flavorText : "";
                SephirahType floor = RMRAbnormalityUnlockManager.GetFloorForScript(info.script);
                bool unlockedInCompendium = LogueBookModels.IsCompendiumAbnormalityPageUnlocked(id);

                lines.Add(name);
                if (!string.IsNullOrEmpty(ability))
                    lines.Add("\u80fd\u529b: " + ability);
                if (!string.IsNullOrEmpty(flavor))
                    lines.Add(flavor);
                lines.Add("\u697c\u5c42: " + (RMRRealizationManager.FloorDisplayNames.TryGetValue(floor, out string floorName) ? floorName : floor.ToString()));
                lines.Add(unlockedInCompendium ? "\u56fe\u9274: \u5df2\u89e3\u9501" : "\u56fe\u9274: \u672a\u89e3\u9501");
                return string.Join("\n", lines.ToArray());
            }
            catch
            {
                return null;
            }
        }

        private static string BuildEgoPageDetail(LorId id)
        {
            // E.G.O. combat pages — battle-card style detail, labeled as EGO.
            string body = BuildBattleCardDetail(id, "");
            if (string.IsNullOrEmpty(body))
                return GetCategoryName(CompendiumCategory.EgoPage);
            return "E.G.O.\n" + body;
        }

        private static string ResToText(AtkResist res)
        {
            switch (res)
            {
                case AtkResist.Weak:
                    return LocalizedUi("ui_RMR_Atlas_Res_Weak", CompendiumUi("耐性", "Weak", "취약"));
                case AtkResist.Normal:
                    return LocalizedUi("ui_RMR_Atlas_Res_Normal", CompendiumUi("一般", "Normal", "보통"));
                case AtkResist.Endure:
                    return LocalizedUi("ui_RMR_Atlas_Res_Endure", CompendiumUi("耐受", "Endured", "내성"));
                case AtkResist.Immune:
                    return LocalizedUi("ui_RMR_Atlas_Res_Immune", CompendiumUi("免疫", "Immune", "면역"));
                case AtkResist.Vulnerable:
                    return LocalizedUi("ui_RMR_Atlas_Res_Vulnerable", CompendiumUi("脆弱", "Fatal", "치명"));
                default:
                    return res.ToString();
            }
        }

        /// <summary>
        /// Gets the card artwork sprite for a battle card or E.G.O. card.
        /// Vanilla cards use AssetBundleManagerRemake; mod cards use CustomizingCardArtworkLoader.
        /// </summary>
        #endregion

        #region --- Artwork helpers ---

        public static Sprite GetCardArtwork(DiceCardXmlInfo card)
        {
            if (card == null)
                return null;
            try
            {
                string packageId = !string.IsNullOrEmpty(card.workshopID) ? card.workshopID : card.id.packageId;
                string[] artworkKeys = new string[2]
                {
                    card.Artwork,
                    card.id.id.ToString()
                };

                if (!string.IsNullOrEmpty(packageId) && LorId.IsModId(packageId))
                {
                    foreach (string artworkKey in artworkKeys)
                    {
                        if (string.IsNullOrEmpty(artworkKey))
                            continue;
                        try
                        {
                            Sprite sprite = Singleton<CustomizingCardArtworkLoader>.Instance.GetSpecificArtworkSprite(packageId, artworkKey);
                            if (sprite != null)
                                return sprite;
                        }
                        catch { }
                    }
                }

                foreach (string artworkKey in artworkKeys)
                {
                    if (string.IsNullOrEmpty(artworkKey))
                        continue;
                    try
                    {
                        Sprite sprite = Singleton<AssetBundleManagerRemake>.Instance.LoadCardSprite(artworkKey);
                        if (sprite != null)
                            return sprite;
                    }
                    catch { }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static Sprite GetCardArtworkById(LorId id)
        {
            if (id == LorId.None)
                return null;
            try
            {
                DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(id, true);
                return GetCardArtwork(card);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region --- Navigation, filters, tile refresh ---

        public void SetActive(bool value)
        {
            if (value)
            {
                // Prepare-tab path disabled — atlas is opened from start hub via ShowFromHub.
                // Keep method for legacy callers; still builds panel if forced.
                _openedFromHub = false;
                Init();
                LogueBookModels.EnsureCompendiumUnlocks();
                LogueBookModels.LoadPermanentCompendiumData();
                LogueBookModels.SyncCurrentInventoryToPermanentAtlas();
                UpdateTiles();
                EnsureHubCloseButton();
                Debug.Log($"[RMR Atlas] Opened. Unlocked: RoleBooks={LogueBookModels.CompendiumUnlockedRoleBooks?.Count ?? 0}, BattleCards={LogueBookModels.CompendiumUnlockedBattleCards?.Count ?? 0}, AbnoPages={LogueBookModels.CompendiumUnlockedAbnormalityPages?.Count ?? 0}, EgoPages={LogueBookModels.CompendiumUnlockedEgoPages?.Count ?? 0}. Total entries built={BuildEntries().Count}.");
            }
            else if (root != null)
            {
                root.SetActive(false);
                _openedFromHub = false;
            }
        }

        private void CreateBattleCardUpgradeToggle()
        {
            Image image = CreateSolidImage(root.transform, "UpgradeToggle",
                new Vector2(-580f, -250f), new Vector2(200f, 34f), ColNavIdle, true);
            upgradeToggleRoot = image.gameObject;
            upgradeToggleFrame = image;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener((UnityAction)(() =>
            {
                showUpgradedBattleCards = !showUpgradedBattleCards;
                currentPage = 0;
                UpdateTiles();
            }));
            upgradeToggleLabel = CreateLabel(image.transform, Vector2.zero, 13, ColCream, TextAlignmentOptions.Center);
            try
            {
                var lrt = upgradeToggleLabel.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                lrt.sizeDelta = Vector2.zero;
            }
            catch { }
            SetBattleCardUpgradeToggleVisible(false);
        }

        private void SelectSection(CompendiumSection section)
        {
            currentSection = section;
            currentPage = 0;
            UpdateTiles();
        }

        private void SelectCategory(CompendiumCategory category)
        {
            currentCategory = category;
            // Abnormality and E.G.O. pages are permanent flat collections, not urban-progress lists.
            if (category == CompendiumCategory.AbnormalityPage || category == CompendiumCategory.EgoPage)
                currentSection = CompendiumSection.All;
            currentPage = 0;
            UpdateTiles();
        }

        private void UpdateTiles()
        {
            // Highlight category rails (Scheme A: gold accent + fill).
            for (int i = 0; i < categoryLabels.Count; i++)
            {
                bool on = Categories[i] == currentCategory;
                if (categoryLabels[i] != null)
                    categoryLabels[i].color = on ? ColGold : ColCream;
                if (i < categoryFrames.Count && categoryFrames[i] != null)
                {
                    try { categoryFrames[i].color = on ? ColNavOn : ColNavIdle; } catch { }
                    try
                    {
                        Transform acc = categoryFrames[i].transform.Find("CatAccent");
                        if (acc != null)
                        {
                            var accImg = acc.GetComponent<Image>();
                            if (accImg != null)
                                accImg.color = on ? ColGold : ColGoldDim;
                        }
                    }
                    catch { }
                }
            }

            SetBattleCardUpgradeToggleVisible(currentCategory == CompendiumCategory.BattleCard);

            // Urban-chapter filter only for role/battle; abno/EGO use flat list (sections hide).
            bool flatCategory = currentCategory == CompendiumCategory.AbnormalityPage
                || currentCategory == CompendiumCategory.EgoPage;
            if (sectionHeader != null)
                sectionHeader.gameObject.SetActive(!flatCategory);
            for (int i = 0; i < sectionLabels.Count; i++)
            {
                if (sectionLabels[i] != null)
                    sectionLabels[i].gameObject.SetActive(!flatCategory);
                if (i < sectionFrames.Count && sectionFrames[i] != null)
                    sectionFrames[i].gameObject.SetActive(!flatCategory);
            }
            if (!flatCategory)
            {
                for (int i = 0; i < sectionLabels.Count; i++)
                {
                    bool on = Sections[i] == currentSection;
                    if (sectionLabels[i] != null)
                        sectionLabels[i].color = on ? ColGold : ColMuted;
                    if (i < sectionFrames.Count && sectionFrames[i] != null)
                    {
                        try { sectionFrames[i].color = on ? ColNavOn : ColNavIdle; } catch { }
                        try
                        {
                            Transform acc = sectionFrames[i].transform.Find("SecAccent");
                            if (acc != null)
                            {
                                var accImg = acc.GetComponent<Image>();
                                if (accImg != null)
                                    accImg.color = on ? ColGold : ColGoldDim;
                            }
                        }
                        catch { }
                    }
                }
            }

            List<CompendiumEntry> entries;
            try
            {
                entries = BuildEntries(showUpgradedBattleCards)
                    .Where(x => x != null && x.Category == currentCategory)
                    .Where(x => flatCategory || currentSection == CompendiumSection.All || x.Section == currentSection)
                    .OrderByDescending(x => x.Unlocked)
                    .ThenBy(x => (x.Category == CompendiumCategory.EgoPage || x.Category == CompendiumCategory.AbnormalityPage)
                        ? (int)x.Floor : 0)
                    .ThenBy(x => x.Title ?? "")
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError("[RMR Atlas] BuildEntries filter failed: " + ex);
                entries = new List<CompendiumEntry>();
            }

            Debug.Log($"[RMR Atlas] UpdateTiles cat={currentCategory} sec={currentSection} count={entries.Count}");

            if (headerCount != null)
            {
                int unlockedCount = entries.Count(x => x != null && x.Unlocked);
                headerCount.text = LocalizedUi("ui_RMR_Atlas_Unlocked", "\u5df2\u89e3\u9501") + "  " + unlockedCount.ToString() + " / " + entries.Count.ToString();
            }

            if (entries.Count == 0)
            {
                ShowEmptyHint(flatCategory
                    ? LocalizedUi("ui_RMR_Atlas_EmptyFlat",
                        CompendiumUi(
                            "本分类暂无条目（异想体/EGO 需游玩或解放战后解锁）。",
                            "No entries in this category (abnormality / E.G.O. unlock via play or realization).",
                            "이 분류에 항목이 없습니다(환상체/E.G.O.는 플레이 또는 개방전 후 해금)."))
                    : LocalizedUi("ui_RMR_Atlas_EmptySection",
                        CompendiumUi(
                            "本节暂无条目，请点左侧「全部」或切换分类。",
                            "No entries in this stage. Try \"All\" or another category.",
                            "이 단계에 항목이 없습니다. 왼쪽 \"전체\" 또는 다른 분류를 선택하세요.")));
            }
            else
            {
                ShowEmptyHint("");
            }

            int totalPages = Math.Max(1, (entries.Count + CenterPageSize - 1) / CenterPageSize);
            if (currentPage >= totalPages)
                currentPage = totalPages - 1;
            if (currentPage < 0)
                currentPage = 0;
            int start = currentPage * CenterPageSize;

            for (int i = 0; i < tiles.Count; i++)
            {
                int idx = start + i;
                try
                {
                    tiles[i].Init(idx < entries.Count ? entries[idx] : null);
                }
                catch (Exception tileEx)
                {
                    Debug.LogWarning("[RMR Atlas] tile.Init failed: " + tileEx.Message);
                    try { tiles[i].gameObject.SetActive(false); } catch { }
                }
            }

            if (pageLabel != null)
                pageLabel.text = (currentPage + 1).ToString() + " / " + totalPages.ToString();
            if (pagePrevBtn != null)
                pagePrevBtn.interactable = currentPage > 0;
            if (pageNextBtn != null)
                pageNextBtn.interactable = currentPage < totalPages - 1;
        }

        #endregion

        #region --- Entry builders (role / battle / abno / ego) ---

        public static List<CompendiumEntry> BuildEntries(bool showUpgradedBattleCards = false)
        {
            List<CompendiumEntry> entries = new List<CompendiumEntry>();
            try { entries.AddRange(BuildRoleBookEntries()); }
            catch (Exception ex) { Debug.LogWarning("[RMR Atlas] BuildRoleBookEntries: " + ex.Message); }
            try { entries.AddRange(BuildBattleCardEntries(showUpgradedBattleCards)); }
            catch (Exception ex) { Debug.LogWarning("[RMR Atlas] BuildBattleCardEntries: " + ex.Message); }
            try { entries.AddRange(BuildAbnormalityEntries()); }
            catch (Exception ex) { Debug.LogWarning("[RMR Atlas] BuildAbnormalityEntries: " + ex.Message); }
            try { entries.AddRange(BuildEgoEntries()); }
            catch (Exception ex) { Debug.LogWarning("[RMR Atlas] BuildEgoEntries: " + ex.Message); }
            return entries;
        }

        private static List<BookXmlInfo> EnumerateAllBooks()
        {
            var result = new List<BookXmlInfo>();
            var seen = new HashSet<string>();
            void Add(BookXmlInfo info)
            {
                if (info == null || info.id == null || info.id == LorId.None)
                    return;
                string key = (info.id.packageId ?? "") + ":" + info.id.id;
                if (!seen.Add(key))
                    return;
                result.Add(info);
            }

            try
            {
                List<BookXmlInfo> list = Singleton<BookXmlList>.Instance?.GetList();
                if (list != null)
                {
                    foreach (BookXmlInfo b in list)
                        Add(b);
                }
            }
            catch (Exception ex) { Debug.LogWarning("[RMR Atlas] BookXmlList.GetList: " + ex.Message); }

            // Workshop packages
            try
            {
                Dictionary<string, List<BookXmlInfo>> workshop = Singleton<BookXmlList>.Instance?.GetAllWorkshopData();
                if (workshop != null)
                {
                    foreach (var kv in workshop)
                    {
                        if (kv.Value == null) continue;
                        foreach (BookXmlInfo b in kv.Value)
                            Add(b);
                    }
                }
            }
            catch { /* ignore */ }

            // Atlas unlocks that might not be in GetList
            try
            {
                LogueBookModels.EnsureCompendiumUnlocks();
                if (LogueBookModels.CompendiumUnlockedRoleBooks != null)
                {
                    foreach (LorId id in LogueBookModels.CompendiumUnlockedRoleBooks)
                    {
                        if (id == null || id == LorId.None) continue;
                        BookXmlInfo b = Singleton<BookXmlList>.Instance?.GetData(id);
                        Add(b);
                    }
                }
            }
            catch { /* ignore */ }

            // Fallback reflection walk if still empty
            if (result.Count == 0)
            {
                try
                {
                    foreach (object obj in ExtractObjects(Singleton<BookXmlList>.Instance, typeof(BookXmlInfo)))
                        Add(obj as BookXmlInfo);
                }
                catch (Exception ex) { Debug.LogWarning("[RMR Atlas] ExtractObjects books: " + ex.Message); }
            }

            return result;
        }

        /// <summary>RMR starter librarian shells — not collectible atlas content.</summary>
        private static bool IsRmrInternalRoleBook(LorId id)
        {
            // 旅途指定司书之页 (-854), 旅途助理司书之页 (-855) and assistant slots -856..
            return RMRCore.IsInternalLibrarianShell(id);
        }

        private static bool IsRmrInternalRoleBookTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;
            // 旅途指定司书 / 旅途助理司书
            return title.IndexOf("\u65c5\u9014\u6307\u5b9a\u53f8\u4e66", StringComparison.Ordinal) >= 0
                || title.IndexOf("\u65c5\u9014\u52a9\u7406\u53f8\u4e66", StringComparison.Ordinal) >= 0
                || title.IndexOf("Journey Librarian", StringComparison.OrdinalIgnoreCase) >= 0
                || title.IndexOf("Assistant Librarian", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<CompendiumEntry> BuildRoleBookEntries()
        {
            foreach (BookXmlInfo info in EnumerateAllBooks())
            {
                if (info == null || info.id == null)
                    continue;
                // Skip error stubs
                try
                {
                    if (info.isError)
                        continue;
                }
                catch { /* ignore */ }

                // Hide RMR internal starter pages from permanent atlas.
                if (IsRmrInternalRoleBook(info.id))
                    continue;

                string title = RewardingModel.GetLocalizedBookName(info);
                if (string.IsNullOrEmpty(title) || RewardingModel.IsPoorDisplayNamePublic(title))
                    title = GetDisplayName(info, info.id);
                if (string.IsNullOrEmpty(title))
                    title = info.id.id.ToString();
                if (IsRmrInternalRoleBookTitle(title))
                    continue;

                yield return new CompendiumEntry
                {
                    Id = info.id,
                    Title = title,
                    Description = title,
                    Artwork = GetBookArtwork(info),
                    Unlocked = IsRoleBookUnlocked(info.id),
                    Category = CompendiumCategory.RoleBook,
                    Section = SectionFromChapter(GetIntMember(info, "Chapter", 1)),
                    Floor = SephirahType.None
                };
            }
        }

        private static List<DiceCardXmlInfo> EnumerateAllCards()
        {
            var result = new List<DiceCardXmlInfo>();
            var seen = new HashSet<string>();
            void Add(DiceCardXmlInfo info)
            {
                if (info == null || info.id == null || info.id == LorId.None)
                    return;
                string key = (info.id.packageId ?? "") + ":" + info.id.id;
                if (!seen.Add(key))
                    return;
                result.Add(info);
            }

            // Prefer known APIs over deep reflection (ExtractObjects can throw/stack).
            try
            {
                var instance = ItemXmlDataList.instance;
                if (instance != null)
                {
                    // Try GetList / GetAllCardList via reflection
                    foreach (string methodName in new[] { "GetList", "GetCardList", "GetAllCardList" })
                    {
                        try
                        {
                            var mi = instance.GetType().GetMethod(methodName, AccessTools.all, null, Type.EmptyTypes, null);
                            if (mi == null) continue;
                            object ret = mi.Invoke(instance, null);
                            if (ret is System.Collections.IEnumerable en)
                            {
                                foreach (object o in en)
                                {
                                    if (o is DiceCardXmlInfo c)
                                        Add(c);
                                }
                                if (result.Count > 0)
                                    break;
                            }
                        }
                        catch { /* try next */ }
                    }
                }
            }
            catch { /* ignore */ }

            if (result.Count == 0)
            {
                try
                {
                    foreach (object obj in ExtractObjects(ItemXmlDataList.instance, typeof(DiceCardXmlInfo)))
                        Add(obj as DiceCardXmlInfo);
                }
                catch (Exception ex) { Debug.LogWarning("[RMR Atlas] ExtractObjects cards: " + ex.Message); }
            }

            try
            {
                LogueBookModels.EnsureCompendiumUnlocks();
                if (LogueBookModels.CompendiumUnlockedBattleCards != null)
                {
                    foreach (LorId id in LogueBookModels.CompendiumUnlockedBattleCards)
                    {
                        if (id == null) continue;
                        DiceCardXmlInfo c = ItemXmlDataList.instance?.GetCardItem(id, true);
                        Add(c);
                    }
                }
            }
            catch { /* ignore */ }

            return result;
        }

        private static IEnumerable<CompendiumEntry> BuildBattleCardEntries(bool showUpgraded)
        {
            foreach (DiceCardXmlInfo info in EnumerateAllCards())
            {
                if (info == null || info.id == null)
                    continue;
                if (RMRAbnormalityUnlockManager.IsRealizationEgoCard(info.id))
                    continue;
                try
                {
                    if (info.CheckUpgradeCard())
                        continue;
                }
                catch { /* ignore */ }

                DiceCardXmlInfo displayInfo = GetDisplayCardInfo(info, showUpgraded);
                // Always localize from base card first — upgraded workshopName is often non-CJK "Name+".
                string title = null;
                try
                {
                    title = RewardingModel.GetLocalizedCardName(info);
                    if (showUpgraded && displayInfo != null && displayInfo != info
                        && !string.IsNullOrEmpty(title) && !title.EndsWith("+", StringComparison.Ordinal))
                        title += "+";
                    if (RewardingModel.IsPoorDisplayNamePublic(title))
                    {
                        string locUp = RewardingModel.GetLocalizedCardName(displayInfo);
                        if (!RewardingModel.IsPoorDisplayNamePublic(locUp))
                            title = locUp;
                    }
                }
                catch { title = null; }
                if (string.IsNullOrEmpty(title) || RewardingModel.IsPoorDisplayNamePublic(title))
                    title = GetDisplayName(info, info.id);

                yield return new CompendiumEntry
                {
                    Id = info.id,
                    Title = title,
                    Description = BuildBattleCardDescription(displayInfo, info.id, showUpgraded),
                    Artwork = GetCardArtwork(displayInfo),
                    Unlocked = IsBattleCardUnlocked(info.id),
                    Category = CompendiumCategory.BattleCard,
                    Section = SectionFromChapter(GetIntMember(info, "Chapter", 1)),
                    Floor = SephirahType.None
                };
            }
        }

        private void SetBattleCardUpgradeToggleVisible(bool visible)
        {
            if (upgradeToggleRoot == null)
                return;
            upgradeToggleRoot.SetActive(visible);
            if (upgradeToggleFrame != null)
                upgradeToggleFrame.color = showUpgradedBattleCards ? UIColorManager.Manager.GetUIColor(UIColor.Highlighted) : UIColorManager.Manager.GetUIColor(UIColor.Default);
            if (upgradeToggleLabel != null)
                upgradeToggleLabel.text = (showUpgradedBattleCards ? "[x] " : "[ ] ") + CompendiumUpgradeToggleLabelText();
        }

        private static DiceCardXmlInfo GetDisplayCardInfo(DiceCardXmlInfo info, bool showUpgraded)
        {
            if (!showUpgraded)
                return info;
            try
            {
                DiceCardXmlInfo upgraded = Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(info.id);
                return upgraded ?? info;
            }
            catch
            {
                return info;
            }
        }

        private static string BuildBattleCardDescription(DiceCardXmlInfo displayInfo, LorId id, bool showUpgraded)
        {
            if (displayInfo == null)
                return "";
            List<string> lines = new List<string>();
            if (showUpgraded)
                lines.Add(LocalizedUi("ui_RMR_Atlas_UpgradePreview", CompendiumUi("升级预览", "Upgrade preview", "강화 미리보기")));
            if (displayInfo.Spec != null)
                lines.Add(CompendiumUi("费用: ", "Cost: ", "비용: ") + displayInfo.Spec.Cost.ToString());
            lines.Add(CompendiumUi("章节: ", "Chapter: ", "장: ") + displayInfo.Chapter.ToString());

            // Localized page ability (never dump raw Script class names — they tofu / English-only).
            try
            {
                string ability = RewardingModel.GetLocalizedCardAbilityDesc(displayInfo);
                if (string.IsNullOrEmpty(ability) && id != null)
                {
                    DiceCardXmlInfo baseCard = ItemXmlDataList.instance != null
                        ? ItemXmlDataList.instance.GetCardItem(id, true) : null;
                    if (baseCard != null)
                        ability = RewardingModel.GetLocalizedCardAbilityDesc(baseCard);
                }
                if (!string.IsNullOrEmpty(ability) && !RewardingModel.IsPoorDisplayNamePublic(ability))
                    lines.Add(ability);
            }
            catch { /* ignore */ }

            if (displayInfo.DiceBehaviourList != null && displayInfo.DiceBehaviourList.Count > 0)
            {
                lines.Add(CompendiumUi("骰子:", "Dice:", "주사위:"));
                for (int i = 0; i < displayInfo.DiceBehaviourList.Count; i++)
                {
                    DiceBehaviour dice = displayInfo.DiceBehaviourList[i];
                    // Do not append raw dice.Script class ids (e.g. English ability keys → tofu risk).
                    lines.Add((i + 1).ToString() + ". " + GetDiceDetailText(dice.Detail) + " "
                        + dice.Min.ToString() + "-" + dice.Dice.ToString());
                }
            }
            return string.Join("\n", lines.ToArray());
        }

        private static string GetDiceDetailText(BehaviourDetail detail)
        {
            switch (detail)
            {
                case BehaviourDetail.Slash:
                    return CompendiumUi("斩击", "Slash", "참격");
                case BehaviourDetail.Penetrate:
                    return CompendiumUi("突刺", "Pierce", "관통");
                case BehaviourDetail.Hit:
                    return CompendiumUi("打击", "Blunt", "타격");
                case BehaviourDetail.Guard:
                    return CompendiumUi("防御", "Block", "방어");
                case BehaviourDetail.Evasion:
                    return CompendiumUi("闪避", "Evade", "회피");
                default:
                    return detail.ToString();
            }
        }

        private static IEnumerable<CompendiumEntry> BuildAbnormalityEntries()
        {
            HashSet<string> seenAbnormalities = new HashSet<string>();
            var root = Singleton<RewardPassivesList>.Instance;
            if (root?.infos == null)
                yield break;
            foreach (RewardPassivesInfo list in root.infos)
            {
                if (list == null || list.RewardPassiveList == null)
                    continue;
                // Include Custom + any Creature rewards (not only Custom list type).
                foreach (RewardPassiveInfo info in list.RewardPassiveList)
                {
                    if (info == null || info.rewardtype != RewardType.Creature)
                        continue;
                    if (RMRAbnormalityUnlockManager.IsNoAbnormalityFallback(info.id))
                        continue;
                    EmotionCardXmlInfo card = LogLikeMod.GetRegisteredPickUpXml(info);
                    string key = GetAbnormalityAtlasKey(card, info);
                    if (!seenAbnormalities.Add(key))
                        continue;
                    AbnormalityCard desc = GetAbnormalityCardDesc(card, info);
                    yield return new CompendiumEntry
                    {
                        Id = info.id,
                        Title = desc == null || string.IsNullOrEmpty(desc.cardName) ? info.script : desc.cardName,
                        Description = desc == null ? info.script : desc.abilityDesc,
                        Artwork = info.Artwork,
                        Unlocked = IsAbnormalityUnlocked(info),
                        Category = CompendiumCategory.AbnormalityPage,
                        Section = SectionFromTier(RMRAbnormalityUnlockManager.GetTierForScript(info.script)),
                        Floor = GetFloorFromScript(info.script)
                    };
                }
            }
        }

        private static IEnumerable<CompendiumEntry> BuildEgoEntries()
        {
            HashSet<LorId> yielded = new HashSet<LorId>();

            // 1. EGO from RewardCardDic_Dummy (mod-defined EGO cards)
            if (LogLikeMod.RewardCardDic_Dummy != null)
            {
                foreach (KeyValuePair<string, List<EmotionEgoXmlInfo>> pair in LogLikeMod.RewardCardDic_Dummy)
                {
                    foreach (EmotionEgoXmlInfo ego in pair.Value)
                    {
                        DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(ego.CardId);
                        if (card == null || !yielded.Add(ego.CardId))
                            continue;
                        SephirahType floor = GetSephirah(ego);
                        yield return new CompendiumEntry
                        {
                            Id = ego.CardId,
                            Title = GetDisplayName(card, ego.CardId),
                            Description = floor.ToString(),
                            Artwork = GetCardArtwork(card),
                            Unlocked = LogueBookModels.IsCompendiumEgoPageUnlocked(ego.CardId),
                            Category = CompendiumCategory.EgoPage,
                            Section = SectionFromTier(TierFromFloor(floor)),
                            Floor = floor
                        };
                    }
                }
            }

            // 2. Realization EGO cards (910001-910090) — always included regardless of RewardCardDic_Dummy
            foreach (var kvp in RMRAbnormalityUnlockManager.RealizationEgoCardsByFloor)
            {
                SephirahType floor = kvp.Key;
                foreach (LorId egoId in kvp.Value)
                {
                    if (!yielded.Add(egoId))
                        continue;
                    DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(egoId, true);
                    if (card == null)
                        continue;
                    yield return new CompendiumEntry
                    {
                        Id = egoId,
                        Title = GetDisplayName(card, egoId),
                        Description = floor.ToString(),
                        Artwork = GetCardArtwork(card),
                        Unlocked = LogueBookModels.IsCompendiumEgoPageUnlocked(egoId),
                        Category = CompendiumCategory.EgoPage,
                        Section = SectionFromTier(RMRAbnormalityUnlockManager.GetTierForFloor(floor)),
                        Floor = floor
                    };
                }
            }
        }

        private static IEnumerable<object> ExtractObjects(object rootObject, Type wantedType)
        {
            HashSet<object> visited = new HashSet<object>();
            foreach (object obj in ExtractObjects(rootObject, wantedType, 0, visited))
                yield return obj;
        }

        private static IEnumerable<object> ExtractObjects(object rootObject, Type wantedType, int depth, HashSet<object> visited)
        {
            if (rootObject == null || depth > 3 || visited.Contains(rootObject))
                yield break;
            visited.Add(rootObject);
            if (wantedType.IsInstanceOfType(rootObject))
            {
                yield return rootObject;
                yield break;
            }
            IEnumerable enumerable = rootObject as IEnumerable;
            if (enumerable != null && !(rootObject is string))
            {
                foreach (object item in enumerable)
                {
                    foreach (object found in ExtractObjects(item, wantedType, depth + 1, visited))
                        yield return found;
                }
            }
            foreach (FieldInfo field in rootObject.GetType().GetFields(AccessTools.all))
            {
                if (field.FieldType == typeof(string) || field.FieldType.IsPrimitive)
                    continue;
                foreach (object found in ExtractObjects(field.GetValue(rootObject), wantedType, depth + 1, visited))
                    yield return found;
            }
        }

        private static bool IsRoleBookUnlocked(LorId id)
        {
            try
            {
                if (LogueBookModels.IsCompendiumRoleBookUnlocked(id))
                    return true;
            }
            catch { /* ignore */ }
            try
            {
                if (LogueBookModels.booklist != null
                    && LogueBookModels.booklist.Exists(x => x != null
                        && (x.BookId == id || (x.ClassInfo != null && x.ClassInfo.id == id))))
                    return true;
            }
            catch { /* ignore */ }
            return false;
        }

        private static bool IsBattleCardUnlocked(LorId id)
        {
            try
            {
                if (LogueBookModels.IsCompendiumBattleCardUnlocked(id))
                    return true;
            }
            catch { /* ignore */ }
            try
            {
                var cards = LogueBookModels.GetCardList(false, true);
                if (cards != null && cards.Exists(x => x != null && x.GetID() == id))
                    return true;
            }
            catch { /* invitation hub may not have run inventory */ }
            return false;
        }

        private static string GetAbnormalityAtlasKey(EmotionCardXmlInfo card, RewardPassiveInfo info)
        {
            if (card != null && card.Script != null && card.Script.Count > 0 && !string.IsNullOrEmpty(card.Script[0]))
                return card.Script[0];
            if (info?.id == null)
                return Guid.NewGuid().ToString();
            return (info.id.packageId ?? "") + ":" + info.id.id.ToString();
        }

        private static bool IsAbnormalityUnlocked(RewardPassiveInfo info)
        {
            return LogueBookModels.IsCompendiumAbnormalityPageUnlocked(info.id)
                || LogueBookModels.selectedEmotion != null && LogueBookModels.selectedEmotion.Exists(x => x.id == info.id)
                || RMRAbnormalityUnlockManager.GetUnlockedEmotionCardsForBattle().Exists(x => x.id == info.id);
        }

        private static AbnormalityCard GetAbnormalityCardDesc(EmotionCardXmlInfo card, RewardPassiveInfo info)
        {
            if (card == null || card.Script == null || card.Script.Count == 0)
                return null;
            if (info != null)
                RMRAbnormalityUnlockManager.EnsureVanillaEmotionPresentation(info, card);
            PickUpModelBase pickUp = LogLikeMod.FindPickUp(card.Script[0]);
            PickUpModel_RMRVanillaEmotion.InjectResolvedDesc(card, pickUp);
            // UI key is card.Name after ApplyVanillaEmotionPresentation (e.g. SnowWhite_Vine).
            string key = !string.IsNullOrEmpty(card.Name) ? card.Name : card.Script[0];
            return Singleton<AbnormalityCardDescXmlList>.Instance.GetAbnormalityCard(key);
        }

        private static Sprite GetBookArtwork(BookXmlInfo info)
        {
            try { return info.GetThumbSprite(); }
            catch { return null; }
        }

        private static void ApplyArtworkLayout(Image target, CompendiumCategory category, bool detail)
        {
            if (target == null)
                return;
            bool portrait = category == CompendiumCategory.BattleCard || category == CompendiumCategory.EgoPage;
            target.preserveAspect = true;
            target.rectTransform.sizeDelta = portrait
                ? (detail ? new Vector2(150f, 214f) : new Vector2(50f, 70f))
                : (detail ? new Vector2(180f, 180f) : new Vector2(64f, 64f));
            target.rectTransform.anchoredPosition = detail
                ? new Vector2(0f, portrait ? 76f : 80f)
                : new Vector2(0f, portrait ? 14f : 10f);
        }

        private static LorId GetLorId(object obj, string name)
        {
            object value = GetMember(obj, name);
            if (value is LorId)
                return (LorId)value;
            return LorId.None;
        }

        private static int GetIntMember(object obj, string name, int fallback)
        {
            object value = GetMember(obj, name);
            return value is int ? (int)value : fallback;
        }

        private static object GetMember(object obj, string name)
        {
            if (obj == null)
                return null;
            FieldInfo field = obj.GetType().GetField(name, AccessTools.all);
            if (field != null)
                return field.GetValue(obj);
            PropertyInfo prop = obj.GetType().GetProperty(name, AccessTools.all);
            return prop == null ? null : prop.GetValue(obj, null);
        }

        private static string GetDisplayName(object obj, LorId id)
        {
            foreach (string name in new[] { "Name", "name", "BookName", "bookName", "cardName" })
            {
                object value = GetMember(obj, name);
                if (value != null && !string.IsNullOrEmpty(value.ToString()))
                    return value.ToString();
            }
            return id.ToString();
        }

        private static CompendiumSection SectionFromChapter(int chapter)
        {
            if (chapter <= 1)
                return CompendiumSection.Rumor;
            if (chapter == 2)
                return CompendiumSection.UrbanLegend;
            if (chapter == 3)
                return CompendiumSection.UrbanMyth;
            if (chapter == 4)
                return CompendiumSection.UrbanIllness;
            if (chapter == 5)
                return CompendiumSection.UrbanNightmare;
            if (chapter == 6)
                return CompendiumSection.UrbanStar;
            return CompendiumSection.Impurity;
        }

        private static CompendiumSection SectionFromTier(int tier)
        {
            if (tier == 1)
                return CompendiumSection.Rumor;
            if (tier == 2)
                return CompendiumSection.UrbanIllness;
            return CompendiumSection.UrbanStar;
        }

        // GetSectionName — include 全部

        private static SephirahType GetSephirah(EmotionEgoXmlInfo ego)
        {
            object value = GetMember(ego, "Sephirah");
            if (value is SephirahType)
                return (SephirahType)value;
            return SephirahType.None;
        }

        private static SephirahType GetFloorFromScript(string script)
        {
            int tier = RMRAbnormalityUnlockManager.GetTierForScript(script);
            if (tier == 1)
                return SephirahType.Malkuth;
            if (tier == 2)
                return SephirahType.Tiphereth;
            return SephirahType.Keter;
        }

        private static int TierFromFloor(SephirahType floor)
        {
            if (floor == SephirahType.Malkuth || floor == SephirahType.Yesod || floor == SephirahType.Hod || floor == SephirahType.Netzach)
                return 1;
            if (floor == SephirahType.Tiphereth || floor == SephirahType.Gebura || floor == SephirahType.Chesed)
                return 2;
            return 3;
        }

        private static string GetSectionName(CompendiumSection section)
        {
            switch (section)
            {
                case CompendiumSection.All:
                    return LocalizedUi("ui_RMR_Atlas_Sec_All", CompendiumUi("全部", "All", "전체"));
                case CompendiumSection.Rumor:
                    return LocalizedUi("ui_RMR_Atlas_Sec_Rumor", CompendiumUi("传闻", "Canard", "소문"));
                case CompendiumSection.UrbanLegend:
                    // Enum name is historical; CN 都市怪谈 == EN Urban Myth / KR 도시괴담
                    return LocalizedUi("ui_RMR_Atlas_Sec_UrbanLegend", CompendiumUi("都市怪谈", "Urban Myth", "도시괴담"));
                case CompendiumSection.UrbanMyth:
                    // CN 都市传说 == EN Urban Legend / KR 도시전설
                    return LocalizedUi("ui_RMR_Atlas_Sec_UrbanMyth", CompendiumUi("都市传说", "Urban Legend", "도시전설"));
                case CompendiumSection.UrbanIllness:
                    return LocalizedUi("ui_RMR_Atlas_Sec_UrbanIllness", CompendiumUi("都市恶疾", "Urban Plague", "도시질병"));
                case CompendiumSection.UrbanNightmare:
                    return LocalizedUi("ui_RMR_Atlas_Sec_UrbanNightmare", CompendiumUi("都市梦魇", "Urban Nightmare", "도시악몽"));
                case CompendiumSection.UrbanStar:
                    return LocalizedUi("ui_RMR_Atlas_Sec_UrbanStar", CompendiumUi("都市之星", "Star of the City", "도시의 별"));
                default:
                    return LocalizedUi("ui_RMR_Atlas_Sec_Impurity", CompendiumUi("杂质", "Impurity", "불순물"));
            }
        }

        private static string GetCategoryName(CompendiumCategory category)
        {
            switch (category)
            {
                case CompendiumCategory.RoleBook:
                    return LocalizedUi("ui_RMR_Atlas_Cat_RoleBook", CompendiumUi("角色书页", "Key Pages", "핵심 책장"));
                case CompendiumCategory.BattleCard:
                    return LocalizedUi("ui_RMR_Atlas_Cat_BattleCard", CompendiumUi("战斗书页", "Combat Pages", "전투 책장"));
                case CompendiumCategory.AbnormalityPage:
                    return LocalizedUi("ui_RMR_Atlas_Cat_Abno", CompendiumUi("异想体书页", "Abnormality Pages", "환상체 책장"));
                case CompendiumCategory.EgoPage:
                    return LocalizedUi("ui_RMR_Atlas_Cat_Ego", CompendiumUi("EGO战斗书页", "E.G.O. Pages", "E.G.O. 전투 책장"));
                default:
                    return LocalizedUi("ui_RMR_Atlas_Cat_Ego", CompendiumUi("EGO战斗书页", "E.G.O. Pages", "E.G.O. 전투 책장"));
            }
        }

        private static string LocalizedUi(string key, string fallback)
        {
            try
            {
                string text = TextDataModel.GetText(key);
                if (!string.IsNullOrEmpty(text) && text != key)
                    return text;
            }
            catch { }
            return fallback;
        }

        #endregion

        #region --- Nested tile cell (LogAtlasTile) ---

        /// <summary>
        /// Archive-wall card cell: centered artwork with title and collection metadata.
        /// Type name keeps "Atlas" for older references; product term is Compendium.
        /// </summary>
        public class LogAtlasTile : MonoBehaviour
        {
            private CompendiumEntry entry;
            private Image image;
            private Image artwork;
            private TextMeshProUGUI title;
            private TextMeshProUGUI subtitle;
            private UILogCustomSelectable selectable;

            public void Init(CompendiumEntry value)
            {
                entry = value;
                if (entry == null)
                {
                    gameObject.SetActive(false);
                    return;
                }
                gameObject.SetActive(true);
                image = image ?? GetComponent<Image>();
                // Scheme A tiles: solid face only (no ShopGoodRewardFrame).
                image.sprite = null;
                image.color = entry.Unlocked ? ColTile : ColTileLocked;

                if (artwork == null)
                {
                    var artGo = new GameObject("Art", typeof(RectTransform));
                    artGo.transform.SetParent(transform, false);
                    artwork = artGo.AddComponent<Image>();
                    artwork.preserveAspect = true;
                    artwork.raycastTarget = false;
                    var artRt = artGo.GetComponent<RectTransform>();
                    artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
                    artRt.sizeDelta = new Vector2(54f, 54f);
                    artRt.anchoredPosition = new Vector2(0f, 18f);
                }
                if (title == null)
                {
                    title = CreateLabel(transform, new Vector2(0f, -24f), 14, ColCream, TextAlignmentOptions.Center);
                    try { title.GetComponent<RectTransform>().sizeDelta = new Vector2(136f, 22f); } catch { }
                }
                if (subtitle == null)
                {
                    subtitle = CreateLabel(transform, new Vector2(0f, -42f), 10, ColMuted, TextAlignmentOptions.Center);
                    try { subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(136f, 18f); } catch { }
                }

                try
                {
                    if (title != null && LogLikeMod.DefFont_TMP != null
                        && (title.font == null || string.IsNullOrEmpty(title.font.name)))
                        LogLikeMod.ApplyTmpFontPreservingSharpMaterial(title, LogLikeMod.DefFont_TMP);
                    if (subtitle != null && LogLikeMod.DefFont_TMP != null
                        && (subtitle.font == null || string.IsNullOrEmpty(subtitle.font.name)))
                        LogLikeMod.ApplyTmpFontPreservingSharpMaterial(subtitle, LogLikeMod.DefFont_TMP);
                }
                catch { }

                if (entry.Unlocked)
                {
                    artwork.sprite = entry.Artwork;
                    if (artwork.sprite == null && LogLikeMod.ArtWorks != null && LogLikeMod.ArtWorks.ContainsKey("ItemNotFoundIcon"))
                        artwork.sprite = LogLikeMod.ArtWorks["ItemNotFoundIcon"];
                    title.text = Truncate(entry.Title ?? "?", 11);
                    title.color = ColCream;
                    subtitle.text = BuildSubtitle(entry);
                    subtitle.color = ColMuted;
                }
                else
                {
                    artwork.sprite = LogLikeMod.ArtWorks != null && LogLikeMod.ArtWorks.ContainsKey("ItemNotFoundIcon")
                        ? LogLikeMod.ArtWorks["ItemNotFoundIcon"] : null;
                    title.text = LockedTitle;
                    title.color = ColMuted;
                    subtitle.text = LocalizedUi("ui_RMR_Atlas_Locked", CompendiumUi("未解锁", "Locked", "미해금"));
                    subtitle.color = new Color(0.45f, 0.40f, 0.32f, 1f);
                }
                // Always show art node; use solid tint if no sprite (avoid white default square).
                if (artwork.sprite == null)
                {
                    artwork.sprite = null;
                    artwork.color = new Color(0.2f, 0.16f, 0.12f, 1f);
                    artwork.enabled = true;
                }
                else
                {
                    artwork.color = Color.white;
                    artwork.enabled = true;
                }
                try
                {
                    artwork.rectTransform.sizeDelta = new Vector2(54f, 54f);
                    artwork.rectTransform.anchoredPosition = new Vector2(0f, 18f);
                }
                catch { }

                // Prefer plain Button — UILogCustomSelectable can fail silently outside battle UI,
                // leaving tiles visible but not clickable on hub atlas.
                var btn = gameObject.GetComponent<Button>();
                if (btn == null)
                {
                    btn = gameObject.AddComponent<Button>();
                    btn.targetGraphic = image;
                    btn.onClick.AddListener(() => OnSelect());
                }
                if (selectable == null)
                {
                    try
                    {
                        selectable = gameObject.AddComponent<UILogCustomSelectable>();
                        selectable.targetGraphic = image;
                        selectable.SelectEvent = new UnityEventBasedata();
                        selectable.SelectEvent.AddListener((UnityAction<BaseEventData>)(e => OnSelect()));
                        selectable.DeselectEvent = new UnityEventBasedata();
                        selectable.DeselectEvent.AddListener((UnityAction<BaseEventData>)(e => OnExit()));
                    }
                    catch { /* Button already handles click */ }
                }
            }

            private static string Truncate(string s, int max)
            {
                if (string.IsNullOrEmpty(s) || s.Length <= max)
                    return s ?? "";
                return s.Substring(0, max) + "\u2026";
            }

            private static string BuildSubtitle(CompendiumEntry e)
            {
                if (e == null)
                    return "";
                switch (e.Category)
                {
                    case CompendiumCategory.BattleCard:
                    case CompendiumCategory.EgoPage:
                        return GetCategoryName(e.Category);
                    case CompendiumCategory.AbnormalityPage:
                        if (RMRRealizationManager.FloorDisplayNames != null
                            && RMRRealizationManager.FloorDisplayNames.TryGetValue(e.Floor, out string fn))
                            return fn;
                        return GetCategoryName(e.Category);
                    default:
                        return GetSectionName(e.Section);
                }
            }

            private void OnSelect()
            {
                if (entry == null)
                    return;
                string name = entry.Unlocked ? entry.Title : LockedTitle;
                string desc = entry.Unlocked
                    ? (string.IsNullOrEmpty(entry.Description) ? GetCategoryName(entry.Category) : entry.Description)
                    : LocalizedUi("ui_RMR_Atlas_LockedHint", CompendiumUi("尚未解锁。", "Not unlocked yet.", "아직 해금되지 않았습니다."));
                try
                {
                    SingletonBehavior<UIMainOverlayManager>.Instance.SetTooltip(name, desc, transform as RectTransform, Rarity.Special, UIToolTipPanelType.OnlyContent);
                }
                catch { }
                try { Singleton<LogCompendiumPanel>.Instance.ShowDetail(entry); } catch { }
            }

            private void OnExit()
            {
                try { SingletonBehavior<UIMainOverlayManager>.Instance.Close(); } catch { }
            }
        }

        #endregion
    }
}
