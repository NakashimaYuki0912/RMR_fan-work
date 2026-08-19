// -----------------------------------------------------------------------------
// LOGLIKE core UI/data: LogRealizationPanel
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\LogRealizationPanel.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using LOR_DiceSystem;
using RogueLike_Mod_Reborn;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace abcdcode_LOGLIKE_MOD
{
    /// <summary>
    /// Realization floor pick — Scheme C: dual-column invitation / pass tickets.
    /// Triple gold frame, progress strip, roman stub, sephirah accent rail, status seal.
    /// </summary>
    public class LogRealizationPanel : MonoBehaviour
    {
        public static LogRealizationPanel Instance { get; private set; }

        private GameObject _rootObject;
        private bool _isVisible;
        public bool IsVisible => _isVisible;

        /// <summary>
        /// When set, floor click only invokes this callback (invitation-time pick).
        /// When null, floor click starts realization battle immediately (in-run fallback).
        /// </summary>
        private Action<SephirahType> _onFloorPicked;

        void Awake()
        {
            Instance = this;
        }
        #region --- UI lifecycle ---


        public void Show(UIBattleSettingEditPanel parentPanel)
        {
            if (parentPanel == null)
                return;
            Show(parentPanel.transform);
        }

        public void Show(Transform parentRoot)
        {
            ShowInternal(parentRoot, null);
        }

        /// <summary>
        /// Invitation-time floor pick: choosing a floor does NOT start battle yet;
        /// the callback stores the floor and continues invitation send.
        /// </summary>
        public void ShowForInvitationPick(Transform parentRoot, Action<SephirahType> onFloorPicked)
        {
            ShowInternal(parentRoot, onFloorPicked);
        }

        private void ShowInternal(Transform parentRoot, Action<SephirahType> onFloorPicked)
        {
            if (parentRoot == null)
                return;
            ForceDestroyUi();
            _onFloorPicked = onFloorPicked;
            _isVisible = true;
            try
            {
                LogLikeMod.InvalidateTmpFontCache();
                var _ = LogLikeMod.DefFont_TMP;
            }
            catch { }

            // Keep host under the UI parent so scene unload + Hide can destroy everything.
            try
            {
                transform.SetParent(parentRoot, false);
                transform.SetAsLastSibling();
                var hostRt = gameObject.GetComponent<RectTransform>();
                if (hostRt == null)
                    hostRt = gameObject.AddComponent<RectTransform>();
                hostRt.anchorMin = Vector2.zero;
                hostRt.anchorMax = Vector2.one;
                hostRt.offsetMin = Vector2.zero;
                hostRt.offsetMax = Vector2.zero;
                hostRt.localScale = Vector3.one;
            }
            catch { }

            BuildPanel(transform);
            if (_rootObject != null)
                _rootObject.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            ForceDestroyUi();
        }
        #endregion

        #region --- Other helpers ---


        public void ForceDestroyUi()
        {
            _isVisible = false;
            _onFloorPicked = null;
            try
            {
                if (_rootObject != null)
                {
                    Destroy(_rootObject);
                    _rootObject = null;
                }
            }
            catch { _rootObject = null; }

            try
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    var child = transform.GetChild(i);
                    if (child != null)
                        Destroy(child.gameObject);
                }
            }
            catch { }
        }
        #endregion

        #region --- UI lifecycle ---


        public static void ForceCloseStatic()
        {
            try
            {
                if (Instance != null)
                {
                    Instance.ForceDestroyUi();
                    try
                    {
                        if (Instance.transform != null && Instance.transform.parent != null)
                            Instance.transform.SetParent(null, false);
                    }
                    catch { }
                }
                try { RMRRealizationLaunchHost.DestroyOverlayCompletely(); } catch { }
            }
            catch { }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            _rootObject = null;
            _isVisible = false;
        }
        #endregion

        #region --- Other helpers ---


        public bool TryHandleBack()
        {
            if (!_isVisible && (_rootObject == null && transform.childCount == 0))
                return false;
            bool invitationPickMode = _onFloorPicked != null;
            ForceDestroyUi();
            try { RMRRealizationLaunchHost.DestroyOverlayIfEmpty(); } catch { }
            if (invitationPickMode)
            {
                RMRRealizationManager.ClearPendingRealizationFloor();
                try
                {
                    if (RMRStartHubPanel.Instance != null)
                        RMRStartHubPanel.Instance.RestoreHubAfterFloorPickCancel();
                }
                catch { }
                return true;
            }
            if (RMRRealizationManager.AwaitingRealizationFloorPick
                && !RMRRealizationManager.PendingRealizationBattle
                && !RMRRealizationManager.InRealizationBattle)
            {
                RMRRealizationManager.CancelRealizationFloorPick();
            }
            return true;
        }

        private static string GetPanelText(string key, string zh, string en, string jp, string kr)
        {
            try
            {
                string text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(key);
                if (!string.IsNullOrEmpty(text) && text != key)
                    return text;
            }
            catch { }
            string lang = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.CurrentLanguage.ToLowerInvariant();
            if (lang.Contains("en")) return en;
            if (lang.Contains("kr") || lang.Contains("ko")) return kr;
            if (lang.Contains("jp") || lang.Contains("ja")) return jp;
            return zh;
        }

        // ── Scheme C palette ────────────────────────────────────────────────────
        private static readonly Color ColPageBlack = new Color(0.02f, 0.015f, 0.01f, 1f);
        private static readonly Color ColVignette = new Color(0.05f, 0.035f, 0.02f, 0.50f);
        private static readonly Color ColOuterFrame = new Color(0.38f, 0.28f, 0.14f, 1f);
        private static readonly Color ColMidFrame = new Color(0.14f, 0.11f, 0.07f, 1f);
        private static readonly Color ColInnerFrame = new Color(0.78f, 0.60f, 0.32f, 1f);
        private static readonly Color ColPanel = new Color(0.085f, 0.065f, 0.05f, 1f);
        private static readonly Color ColPanelLift = new Color(0.125f, 0.095f, 0.065f, 1f);
        private static readonly Color ColGold = new Color(0.93f, 0.76f, 0.42f, 1f);
        private static readonly Color ColGoldDim = new Color(0.62f, 0.48f, 0.26f, 1f);
        private static readonly Color ColCream = new Color(0.93f, 0.88f, 0.78f, 1f);
        private static readonly Color ColMuted = new Color(0.68f, 0.62f, 0.50f, 1f);
        private static readonly Color ColTicketFaceOpen = new Color(0.13f, 0.10f, 0.07f, 1f);
        private static readonly Color ColTicketFaceCleared = new Color(0.20f, 0.16f, 0.09f, 1f);
        private static readonly Color ColTicketEdgeOpen = new Color(0.50f, 0.40f, 0.22f, 0.95f);
        private static readonly Color ColTicketEdgeCleared = new Color(0.90f, 0.72f, 0.36f, 1f);
        private static readonly Color ColStubFace = new Color(0.07f, 0.055f, 0.035f, 1f);
        private static readonly Color ColBadgeCleared = new Color(0.48f, 0.36f, 0.14f, 1f);
        private static readonly Color ColBadgeOpen = new Color(0.22f, 0.17f, 0.12f, 1f);
        private static readonly Color ColCloseBtn = new Color(0.20f, 0.15f, 0.10f, 1f);
        private static readonly Color ColProgressTrack = new Color(0.14f, 0.11f, 0.07f, 1f);
        private static readonly Color ColProgressFill = new Color(0.82f, 0.62f, 0.28f, 1f);
        private static readonly Color ColPerf = new Color(0.55f, 0.42f, 0.22f, 0.55f);
        #endregion

        #region --- UI lifecycle ---


        private void BuildPanel(Transform parentRoot)
        {
            RMRAbnormalityUnlockManager.RefreshRealizationProgress();

            _rootObject = new GameObject("RealizationPanel", typeof(RectTransform));
            _rootObject.transform.SetParent(parentRoot, false);
            _rootObject.transform.SetAsLastSibling();

            RectTransform rootRt = _rootObject.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.localScale = Vector3.one;
            rootRt.anchoredPosition = Vector2.zero;

            var blackImg = _rootObject.AddComponent<Image>();
            blackImg.color = ColPageBlack;
            blackImg.raycastTarget = true;

            AddSolid(_rootObject.transform, "Vignette", Vector2.zero, new Vector2(1480f, 1020f), ColVignette, raycast: false);

            // Triple frame: bronze → ink → gold.
            const float cardW = 1120f;
            const float cardH = 900f;

            var outer = AddSolid(_rootObject.transform, "OuterFrame", Vector2.zero, new Vector2(cardW + 20f, cardH + 20f), ColOuterFrame, raycast: false);
            var mid = AddSolid(outer.transform, "MidFrame", Vector2.zero, new Vector2(cardW + 10f, cardH + 10f), ColMidFrame, raycast: false);
            var frame = AddSolid(mid.transform, "GoldFrame", Vector2.zero, new Vector2(cardW, cardH), ColInnerFrame, raycast: false);

            var card = AddSolid(frame.transform, "Card", Vector2.zero, new Vector2(cardW - 8f, cardH - 8f), ColPanel, raycast: true);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = Vector2.zero;
            cardRt.anchorMax = Vector2.one;
            cardRt.offsetMin = new Vector2(4f, 4f);
            cardRt.offsetMax = new Vector2(-4f, -4f);
            cardRt.sizeDelta = Vector2.zero;

            AddAnchoredSolid(card.transform, "TopSheen",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -72f), new Vector2(cardW - 48f, 148f),
                new Color(ColPanelLift.r, ColPanelLift.g, ColPanelLift.b, 0.9f), raycast: false);

            AddCornerOrnaments(frame.transform, cardW, cardH);

            // Header.
            AddOrnamentRule(card.transform, new Vector2(0f, 372f), 860f);
            AddDiamond(card.transform, new Vector2(0f, 372f), 11f, ColGold);

            AddLabel(card.transform,
                GetPanelText("ui_RMR_RealizationTitle", "\u6311\u6218\u89e3\u653e\u6218", "Challenge Realization", "\u89e3\u653e\u6226\u306b\u6311\u3080", "\ud574\ubc29\uc804 \ub3c4\uc804"),
                new Vector2(0f, 330f), 40, new Vector2(960f, 52f), ColGold, true);

            AddLabel(card.transform,
                GetPanelText("ui_RMR_RealizationSubtitle",
                    "LIBRARY OF RUINA  \u00b7  FLOOR REALIZATION PASS",
                    "LIBRARY OF RUINA  \u00b7  FLOOR REALIZATION PASS",
                    "LIBRARY OF RUINA  \u00b7  FLOOR REALIZATION PASS",
                    "LIBRARY OF RUINA  \u00b7  FLOOR REALIZATION PASS"),
                new Vector2(0f, 292f), 13, new Vector2(960f, 22f), ColGoldDim, false);

            AddLabel(card.transform,
                GetPanelText("ui_RMR_RealizationDesc",
                    "\u9009\u62e9\u697c\u5c42\u901a\u884c\u5238\uff0c\u7f16\u961f\u540e\u76f4\u63a5\u6311\u6218\u539f\u7248\u6700\u7ec8\u89e3\u653e\u6218\u3002\u9996\u901a\u6c38\u4e45\u89e3\u9501\uff1b\u5df2\u901a\u5173\u53ef\u518d\u6218\u3002",
                    "Choose a floor pass, form a team, then fight the vanilla final Floor Realization. First clear unlocks permanently; re-clears give no rewards.",
                    "\u968e\u306e\u901a\u884c\u5238\u3092\u9078\u3073\u3001\u7de8\u6210\u5f8c\u306b\u672c\u7de8\u6700\u7d42\u89e3\u653e\u6226\u3078\u3002",
                    "\uce35 \ud1b5\ud589\uad8c\uc744 \uc120\ud0dd\ud55c \ub4a4 \ud3b8\uc131\ud558\uace0 \uc6d0\uc791 \ucd5c\uc885 \ud574\ubc29\uc804\uc5d0 \ub3c4\uc804\ud569\ub2c8\ub2e4."),
                new Vector2(0f, 256f), 15, new Vector2(960f, 40f), ColMuted, false);

            var floors = new List<SephirahType>
            {
                SephirahType.Malkuth, SephirahType.Yesod, SephirahType.Hod, SephirahType.Netzach,
                SephirahType.Tiphereth, SephirahType.Gebura, SephirahType.Chesed, SephirahType.Binah,
                SephirahType.Hokma, SephirahType.Keter
            };
            int cleared = 0;
            for (int i = 0; i < floors.Count; i++)
            {
                if (RMRAbnormalityUnlockManager.IsFloorRealizationCompleted(floors[i]))
                    cleared++;
            }
            BuildProgressStrip(card.transform, cleared, floors.Count, new Vector2(0f, 210f));

            AddOrnamentRule(card.transform, new Vector2(0f, 180f), 820f);

            // Dual-column pass tickets (5 rows × 2).
            float startX = -255f;
            float startY = 124f;
            float spacingX = 510f;
            float spacingY = -68f;

            for (int i = 0; i < floors.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                float x = startX + col * spacingX;
                float y = startY + row * spacingY;
                bool completed = RMRAbnormalityUnlockManager.IsFloorRealizationCompleted(floors[i]);
                CreateFloorTicket(card.transform, floors[i], i, x, y, completed);
            }

            AddOrnamentRule(card.transform, new Vector2(0f, -258f), 820f);
            AddDiamond(card.transform, new Vector2(0f, -258f), 8f, ColGoldDim);

            AddLabel(card.transform,
                GetPanelText("ui_RMR_RealizationFooter",
                    "Library of Ruina  \u00b7  Roguelike Mod Reborn",
                    "Library of Ruina  \u00b7  Roguelike Mod Reborn",
                    "Library of Ruina  \u00b7  Roguelike Mod Reborn",
                    "Library of Ruina  \u00b7  Roguelike Mod Reborn"),
                new Vector2(0f, -286f), 13, new Vector2(900f, 22f), ColMuted, false);

            CreateCloseButton(card.transform, new Vector2(0f, -340f));
        }

        private static void BuildProgressStrip(Transform parent, int cleared, int total, Vector2 pos)
        {
            float trackW = 560f;
            float trackH = 11f;

            AddLabel(parent,
                string.Format(
                    GetPanelTextStatic("ui_RMR_RealizationProgress",
                        "\u89e3\u653e\u8fdb\u5ea6  {0} / {1}",
                        "Realization Progress  {0} / {1}",
                        "\u89e3\u653e\u9032\u6357  {0} / {1}",
                        "\ud574\ubc29 \uc9c4\ud589  {0} / {1}"),
                    cleared, total),
                new Vector2(pos.x, pos.y + 18f), 14, new Vector2(640f, 22f), ColCream, false);

            // Outer gold rim + track.
            AddSolid(parent, "ProgressRim", pos, new Vector2(trackW + 6f, trackH + 6f), ColGoldDim, raycast: false);
            var track = AddSolid(parent, "ProgressTrack", pos, new Vector2(trackW, trackH), ColProgressTrack, raycast: false);

            float ratio = total <= 0 ? 0f : Mathf.Clamp01(cleared / (float)total);
            float fillW = Mathf.Max(ratio > 0.001f ? 8f : 0f, trackW * ratio);
            if (fillW > 0.5f)
            {
                var fill = new GameObject("ProgressFill", typeof(RectTransform));
                fill.transform.SetParent(track.transform, false);
                var fillImg = fill.AddComponent<Image>();
                fillImg.color = ColProgressFill;
                fillImg.raycastTarget = false;
                var fillRt = fill.GetComponent<RectTransform>();
                fillRt.anchorMin = new Vector2(0f, 0.5f);
                fillRt.anchorMax = new Vector2(0f, 0.5f);
                fillRt.pivot = new Vector2(0f, 0.5f);
                fillRt.sizeDelta = new Vector2(fillW, trackH - 2f);
                fillRt.anchoredPosition = new Vector2(1f, 0f);
            }

            // Segment ticks (10 floors).
            if (total > 1)
            {
                for (int i = 1; i < total; i++)
                {
                    float tx = -trackW * 0.5f + trackW * (i / (float)total);
                    AddSolid(track.transform, "Tick", new Vector2(tx, 0f), new Vector2(1.5f, trackH),
                        new Color(0.05f, 0.04f, 0.02f, 0.65f), raycast: false);
                }
            }
        }
        #endregion

        #region --- Other helpers ---


        private static string GetPanelTextStatic(string key, string zh, string en, string jp, string kr)
        {
            try
            {
                string text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(key);
                if (!string.IsNullOrEmpty(text) && text != key)
                    return text;
            }
            catch { }
            string lang = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.CurrentLanguage.ToLowerInvariant();
            if (lang.Contains("en")) return en;
            if (lang.Contains("kr") || lang.Contains("ko")) return kr;
            if (lang.Contains("jp") || lang.Contains("ja")) return jp;
            return zh;
        }

        private static void AddCornerOrnaments(Transform frame, float w, float h)
        {
            float hx = w * 0.5f - 7f;
            float hy = h * 0.5f - 7f;
            AddDiamond(frame, new Vector2(-hx, hy), 15f, ColGold);
            AddDiamond(frame, new Vector2(hx, hy), 15f, ColGold);
            AddDiamond(frame, new Vector2(-hx, -hy), 15f, ColGold);
            AddDiamond(frame, new Vector2(hx, -hy), 15f, ColGold);
            float hx2 = w * 0.5f - 30f;
            float hy2 = h * 0.5f - 30f;
            AddDiamond(frame, new Vector2(-hx2, hy2), 7f, ColGoldDim);
            AddDiamond(frame, new Vector2(hx2, hy2), 7f, ColGoldDim);
            AddDiamond(frame, new Vector2(-hx2, -hy2), 7f, ColGoldDim);
            AddDiamond(frame, new Vector2(hx2, -hy2), 7f, ColGoldDim);
        }

        private static void AddDiamond(Transform parent, Vector2 pos, float size, Color color)
        {
            var go = new GameObject("Diamond", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            rt.localEulerAngles = new Vector3(0f, 0f, 45f);
        }

        private static void AddOrnamentRule(Transform parent, Vector2 pos, float width)
        {
            AddSolid(parent, "Rule", pos, new Vector2(width, 1.5f),
                new Color(ColGoldDim.r, ColGoldDim.g, ColGoldDim.b, 0.8f), raycast: false);
            AddSolid(parent, "RuleTipL", new Vector2(pos.x - width * 0.5f - 8f, pos.y),
                new Vector2(20f, 1.5f), new Color(ColGold.r, ColGold.g, ColGold.b, 0.5f), raycast: false);
            AddSolid(parent, "RuleTipR", new Vector2(pos.x + width * 0.5f + 8f, pos.y),
                new Vector2(20f, 1.5f), new Color(ColGold.r, ColGold.g, ColGold.b, 0.5f), raycast: false);
        }

        private static GameObject AddSolid(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return go;
        }

        private static GameObject AddAnchoredSolid(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return go;
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string text, Vector2 pos, int size, Vector2 box, Color color, bool bold,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = box;
            rt.anchoredPosition = pos;
            // Same as StartHub: Noto CJK SDF has no real bold; FontStyles.Bold + outline = mushy CN.
            LogLikeMod.ApplyTmpFontPreservingSharpMaterial(tmp, LogLikeMod.DefFont_TMP);
            tmp.fontSize = bold ? size + 2 : size;
            tmp.color = color;
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
        #endregion

        #region --- UI lifecycle ---


        private static Image CreateLoRButtonShell(Transform parent, string name, Vector2 pos, Vector2 size,
            Color faceColor, Color edgeColor, out Button button, out GameObject btnGo)
        {
            btnGo = new GameObject(name, typeof(RectTransform));
            btnGo.transform.SetParent(parent, false);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = size;
            btnRect.anchoredPosition = pos;

            var edge = new GameObject("Edge", typeof(RectTransform));
            edge.transform.SetParent(btnGo.transform, false);
            var eImg = edge.AddComponent<Image>();
            eImg.color = edgeColor;
            eImg.raycastTarget = false;
            var eRt = edge.GetComponent<RectTransform>();
            eRt.anchorMin = Vector2.zero;
            eRt.anchorMax = Vector2.one;
            eRt.offsetMin = Vector2.zero;
            eRt.offsetMax = Vector2.zero;

            // Inner double-line (ticket face inset).
            var inner = new GameObject("InnerRim", typeof(RectTransform));
            inner.transform.SetParent(btnGo.transform, false);
            var iImg = inner.AddComponent<Image>();
            iImg.color = new Color(edgeColor.r * 0.55f, edgeColor.g * 0.55f, edgeColor.b * 0.55f, 0.85f);
            iImg.raycastTarget = false;
            var iRt = inner.GetComponent<RectTransform>();
            iRt.anchorMin = Vector2.zero;
            iRt.anchorMax = Vector2.one;
            iRt.offsetMin = new Vector2(2f, 2f);
            iRt.offsetMax = new Vector2(-2f, -2f);

            var face = new GameObject("Face", typeof(RectTransform));
            face.transform.SetParent(btnGo.transform, false);
            var faceImg = face.AddComponent<Image>();
            faceImg.color = faceColor;
            faceImg.raycastTarget = true;
            var fRt = face.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(3.5f, 3.5f);
            fRt.offsetMax = new Vector2(-3.5f, -3.5f);

            button = btnGo.AddComponent<Button>();
            button.targetGraphic = faceImg;
            try
            {
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.14f, 1.08f, 0.92f, 1f);
                colors.pressedColor = new Color(0.80f, 0.74f, 0.60f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.fadeDuration = 0.08f;
                button.colors = colors;
            }
            catch { }

            return faceImg;
        }
        #endregion

        #region --- Other helpers ---


        private static Color GetSephirahAccent(SephirahType floor)
        {
            try
            {
                if (UIColorManager.Manager != null)
                {
                    Color c = UIColorManager.Manager.GetSephirahColor(floor);
                    if (c.a > 0.05f && (c.r + c.g + c.b) > 0.05f)
                        return c;
                }
            }
            catch { }

            switch (floor)
            {
                case SephirahType.Malkuth: return new Color(0.72f, 0.78f, 0.28f, 1f);
                case SephirahType.Yesod: return new Color(0.62f, 0.38f, 0.78f, 1f);
                case SephirahType.Hod: return new Color(0.88f, 0.52f, 0.22f, 1f);
                case SephirahType.Netzach: return new Color(0.32f, 0.72f, 0.42f, 1f);
                case SephirahType.Tiphereth: return new Color(0.92f, 0.78f, 0.28f, 1f);
                case SephirahType.Gebura: return new Color(0.82f, 0.22f, 0.22f, 1f);
                case SephirahType.Chesed: return new Color(0.28f, 0.52f, 0.88f, 1f);
                case SephirahType.Binah: return new Color(0.55f, 0.42f, 0.28f, 1f);
                case SephirahType.Hokma: return new Color(0.78f, 0.82f, 0.88f, 1f);
                case SephirahType.Keter: return new Color(0.95f, 0.92f, 0.85f, 1f);
                default: return ColGold;
            }
        }
        #endregion

        #region --- UI lifecycle ---


        private void CreateCloseButton(Transform parent, Vector2 pos)
        {
            CreateLoRButtonShell(parent, "CloseBtn", pos, new Vector2(280f, 54f),
                ColCloseBtn, ColGoldDim, out Button btn, out _);

            AddLabel(btn.transform,
                GetPanelText("ui_RMR_RealizationClose", "\u8fd4\u56de", "Back", "\u623b\u308b", "\ub3cc\uc544\uac00\uae30"),
                Vector2.zero, 22, new Vector2(250f, 48f), ColCream, false);

            btn.onClick.AddListener(() =>
            {
                try { UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click); } catch { }
                bool invitationPickMode = _onFloorPicked != null;
                ForceDestroyUi();
                try { RMRRealizationLaunchHost.DestroyOverlayIfEmpty(); } catch { }
                if (invitationPickMode)
                {
                    RMRRealizationManager.ClearPendingRealizationFloor();
                    try
                    {
                        if (RMRStartHubPanel.Instance != null)
                            RMRStartHubPanel.Instance.RestoreHubAfterFloorPickCancel();
                    }
                    catch { }
                    return;
                }
                if (RMRRealizationManager.AwaitingRealizationFloorPick
                    && !RMRRealizationManager.PendingRealizationBattle
                    && !RMRRealizationManager.InRealizationBattle)
                {
                    RMRRealizationManager.CancelRealizationFloorPick();
                }
            });
        }

        /// <summary>
        /// Scheme C pass ticket: left stub (roman) + perforation + sephirah rail + name + EN code + status seal.
        /// </summary>
        private void CreateFloorTicket(Transform parent, SephirahType floor, int index, float x, float y, bool completed)
        {
            // Floor label only (Chinese). No EN sephirah codes / roman stubs — those felt too loud.
            string displayName = RMRRealizationManager.FloorDisplayNames.TryGetValue(floor, out string name)
                ? name : floor.ToString();
            string statusText = completed
                ? GetPanelText("ui_RMR_RealizationReplay", "\u5df2\u5b8c\u6210 \u00b7 \u53ef\u518d\u6218", "Cleared · Replay", "\u5b8c\u4e86\u30fb\u518d\u6226", "\uc644\ub8cc \u00b7 \uc7ac\uc804")
                : GetPanelText("ui_RMR_RealizationChallenge", "\u6311\u6218", "Challenge", "\u6311\u6226", "\ub3c4\uc804");
            Color accent = GetSephirahAccent(floor);

            CreateLoRButtonShell(parent, "FloorTicket_" + floor, new Vector2(x, y), new Vector2(478f, 60f),
                completed ? ColTicketFaceCleared : ColTicketFaceOpen,
                completed ? ColTicketEdgeCleared : ColTicketEdgeOpen,
                out Button btn, out GameObject btnGo);

            // Left sephirah color rail only (identity without extra name labels).
            var rail = new GameObject("AccentRail", typeof(RectTransform));
            rail.transform.SetParent(btnGo.transform, false);
            var railImg = rail.AddComponent<Image>();
            railImg.color = accent;
            railImg.raycastTarget = false;
            var railRt = rail.GetComponent<RectTransform>();
            railRt.anchorMin = new Vector2(0f, 0f);
            railRt.anchorMax = new Vector2(0f, 1f);
            railRt.pivot = new Vector2(0f, 0.5f);
            railRt.sizeDelta = new Vector2(5f, -10f);
            railRt.anchoredPosition = new Vector2(6f, 0f);

            // Centered Chinese floor name.
            AddLabel(btnGo.transform, displayName, new Vector2(-20f, 0f), 22, new Vector2(260f, 40f),
                completed ? ColGold : ColCream, completed, TextAlignmentOptions.Center);

            // Right status seal.
            Color badgeFace = completed ? ColBadgeCleared : ColBadgeOpen;
            Color badgeEdge = completed ? ColTicketEdgeCleared : ColTicketEdgeOpen;
            var badge = AddSolid(btnGo.transform, "StatusSeal", new Vector2(168f, 0f), new Vector2(118f, 34f),
                badgeFace, raycast: false);
            var badgeRim = AddSolid(badge.transform, "SealRim", Vector2.zero, new Vector2(122f, 38f),
                badgeEdge, raycast: false);
            badgeRim.transform.SetAsFirstSibling();

            AddLabel(badge.transform, statusText, Vector2.zero, 14, new Vector2(112f, 30f),
                completed ? ColGold : ColCream, completed);

            if (completed)
                AddDiamond(btnGo.transform, new Vector2(210f, 0f), 6f, ColGold);

            SephirahType capturedFloor = floor;
            btn.onClick.AddListener(() =>
            {
                try { UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click); } catch { }
                Action<SephirahType> inviteCb = _onFloorPicked;
                ForceDestroyUi();
                try { RMRRealizationLaunchHost.DestroyOverlayCompletely(); } catch { }
                if (inviteCb != null)
                {
                    Debug.Log($"[RMR] Realization floor picked (invitation path): {capturedFloor}");
                    inviteCb(capturedFloor);
                    return;
                }
                Debug.Log($"[RMR] Realization floor picked (direct battle path): {capturedFloor}");
                RMRRealizationManager.StartRealizationBattle(capturedFloor);
            });
        }
        #endregion

    }
}
