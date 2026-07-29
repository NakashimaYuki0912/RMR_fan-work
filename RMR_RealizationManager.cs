// -----------------------------------------------------------------------------
// RogueLike Mod Reborn (RMR): RMR_RealizationManager
// Namespace/file: ruina-roguelike-reborn-main\RMR_RealizationManager.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using abcdcode_LOGLIKE_MOD;
using GameSave;
using HarmonyLib;
using LOR_DiceSystem;
using UnityEngine;

namespace RogueLike_Mod_Reborn
{
    /// <summary>RMR type: RMRRealizationManager</summary>
    public static class RMRRealizationManager
    {
        /// <summary>
        /// Vanilla Floor Realization (最终解放战) stage id per floor.
        /// These are Angela/Roland multi-phase E.G.O fights (EnemyUnitInfo_creature_final),
        /// NOT the earlier abnormality suppression stages (201001–201004 etc.).
        /// Phase changes are handled in-battle by vanilla scripts (e.g. Corrosion).
        /// </summary>
        public static readonly Dictionary<SephirahType, int> RealizationBossStageIds = new Dictionary<SephirahType, int>
        {
            { SephirahType.Malkuth,   201005 },
            { SephirahType.Yesod,     202005 },
            { SephirahType.Hod,       203005 },
            { SephirahType.Netzach,   204005 },
            { SephirahType.Tiphereth, 205005 },
            { SephirahType.Gebura,    206005 },
            { SephirahType.Chesed,    207005 },
            { SephirahType.Binah,     208004 },
            { SephirahType.Hokma,     209004 },
            { SephirahType.Keter,     210009 },
        };

        /// <summary>True while our code is invoking OpenBattlePrepare (re-entrancy guard).</summary>
        private static bool _inOpenBattlePrepare;

        public static readonly Dictionary<SephirahType, string> FloorDisplayNames = new Dictionary<SephirahType, string>
        {
            { SephirahType.Malkuth,   "\u5386\u53f2\u5c42" },
            { SephirahType.Yesod,     "\u79d1\u6280\u5c42" },
            { SephirahType.Hod,       "\u6587\u5b66\u5c42" },
            { SephirahType.Netzach,   "\u827a\u672f\u5c42" },
            { SephirahType.Tiphereth, "\u81ea\u7136\u5c42" },
            { SephirahType.Gebura,    "\u8bed\u8a00\u5c42" },
            { SephirahType.Chesed,    "\u793e\u4f1a\u5c42" },
            { SephirahType.Binah,     "\u54f2\u5b66\u5c42" },
            { SephirahType.Hokma,     "\u5b97\u6559\u5c42" },
            { SephirahType.Keter,     "\u603b\u7c7b\u5c42" },
        };

        public static bool InRealizationBattle { get; private set; }

        /// <summary>
        /// Set when a realization battle is queued but the current event hasn't finished
        /// transitioning yet. Prevents the EndBattle hook from treating the event end
        /// as a realization battle end.
        /// </summary>
        public static bool PendingRealizationBattle { get; private set; }
        public static bool ForceReturnAsDefeatPending { get; private set; }
        /// <summary>After realization ends, reopen the start hub (when still in hub session).</summary>
        public static bool PendingReturnToHub { get; private set; }

        /// <summary>
        /// True only after the first combat round of a realization fight has started.
        /// EndBattle before this is a transition glitch and must NOT open the start hub.
        /// </summary>
        public static bool RealizationCombatLive { get; private set; }

        public static bool IsRealizationPreparationActive => PendingRealizationBattle && !InRealizationBattle;

        /// <summary>
        /// True while the StageController_StartBattle hook is on the stack.
        /// Used so opening prepare mid-hook aborts the shell StartBattle only,
        /// without blocking the later player-confirmed StartBattle.
        /// </summary>
        private static int _startBattleHookDepth;
        private static bool _abortCurrentStartBattleAfterPrepare;
        #region --- Battle bootstrap ---


        public static void EnterStartBattleHook()
        {
            _startBattleHookDepth++;
        }

        public static void ExitStartBattleHook()
        {
            if (_startBattleHookDepth > 0)
                _startBattleHookDepth--;
            if (_startBattleHookDepth == 0)
                _abortCurrentStartBattleAfterPrepare = false;
        }

        /// <summary>
        /// If prepare was opened during the current StartBattle hook, consume the abort
        /// and skip orig(StartBattle) so dummy/shell combat does not auto-start.
        /// </summary>
        public static bool ConsumeAbortCurrentStartBattle()
        {
            if (!_abortCurrentStartBattleAfterPrepare)
                return false;
            _abortCurrentStartBattleAfterPrepare = false;
            return true;
        }

        private static void RequestAbortCurrentStartBattleIfInHook()
        {
            if (_startBattleHookDepth > 0)
                _abortCurrentStartBattleAfterPrepare = true;
        }
        #endregion

        #region --- State & floors ---


        /// <summary>
        /// Clears the realization battle flag. Called from StageController_EndBattle
        /// AFTER the vanilla EndBattle completes, so that postfix hooks still see
        /// the guard during cleanup.
        /// </summary>
        public static void ClearRealizationFlag()
        {
            InRealizationBattle = false;
            RealizationCombatLive = false;
        }
        #endregion

        #region --- Other helpers ---


        public static void ConsumeForceReturnAsDefeat()
        {
            ForceReturnAsDefeatPending = false;
        }
        #endregion

        #region --- State & floors ---


        /// <summary>
        /// Called from StageController_StartBattle when the pending realization stage
        /// actually begins loading. Activates InRealizationBattle and clears the pending flag.
        /// </summary>
        public static void ActivatePendingRealization()
        {
            if (PendingRealizationBattle)
            {
                InRealizationBattle = true;
                PendingRealizationBattle = false;
                RealizationCombatLive = false;
                Debug.Log($"[RMRRealizationManager] Activated realization battle: {CurrentRealizationFloor}");
            }
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>Call on first round start while in realization combat.</summary>
        public static void MarkRealizationCombatLive()
        {
            if (!InRealizationBattle)
                return;
            if (!RealizationCombatLive)
            {
                RealizationCombatLive = true;
                Debug.Log($"[RMRRealizationManager] Realization combat is live: {CurrentRealizationFloor}");
                // Vanilla Floor Realization pre-loads floor EGO; make sure the very first
                // card-select round shows normal battle pages (EGO stays on the toggle).
                try { RMRPrepareRestrictions.ForceHandUiToBattleCards(); } catch { }
            }
            // Every round: ensure multiphase boss passives + log immortal/phase state.
            EnsureRealizationMultiPhaseBossState();
        }
        #endregion

        #region --- Battle bootstrap ---


        /// <summary>
        /// True when EndBattle should run full realization cleanup + hub return.
        /// False during prepare / scene transition (StartBattle then immediate EndBattle).
        /// </summary>
        public static bool ShouldHandleRealizationBattleEnd()
        {
            return InRealizationBattle && RealizationCombatLive;
        }
        public static SephirahType CurrentRealizationFloor { get; private set; }

        /// <summary>True while the start-hub session can offer realization (before normal play).</summary>
        public static bool HubSessionActive { get; private set; }

        /// <summary>True after the player chooses normal play from the hub; blocks realization for this run.</summary>
        public static bool NormalPlayStarted { get; private set; }

        /// <summary>
        /// Durable launch intent (survives hub Hide). Set from invitation hub; consumed in HandlePostInvitationLaunch.
        /// Mirrored by <see cref="RMRStartHubPanel.LaunchIntent"/> for legacy call sites.
        /// </summary>
        public static RMRLaunchIntent PendingLaunchIntent { get; set; }

        /// <summary>
        /// Optional pre-selected floor (if set before ConfirmSendInvitation).
        /// Default product path leaves this null and picks floor on battle-prepare after -853 shell.
        /// </summary>
        public static SephirahType? PendingRealizationFloor { get; set; }

        /// <summary>
        /// True while floor panel is up on the -853 bootstrap shell.
        /// Blocks fighting the dummy start-stage unit 854.
        /// </summary>
        public static bool AwaitingRealizationFloorPick { get; set; }

        /// <summary>
        /// From "挑战解放战" until battle ends / cancel / normal play — treat reception as
        /// non-roguelike so CheckStage(-853) hooks cannot hijack the bootstrap shell.
        /// Engine still needs -853 to init LogueBookModels; we never run its dummy combat.
        /// </summary>
        public static bool RealizationReceptionActive { get; private set; }
        #endregion

        #region --- State & floors ---


        public static void ClearPendingRealizationFloor()
        {
            PendingRealizationFloor = null;
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Full wipe when opening a fresh invitation hub (no leftover intent/floor/gate).
        /// </summary>
        public static void PrepareNewHubSession()
        {
            // Defensive: a defeat that bypassed the EndBattle hook (vanilla GameOver path)
            // can leave realization combat flags and the atlas-only loadout stuck ON.
            // A fresh hub session must never start with stale realization state.
            if (InRealizationBattle || RealizationCombatLive || PendingRealizationBattle)
            {
                Debug.LogWarning("[RMRRealizationManager] PrepareNewHubSession found stale realization combat state — force-clearing.");
                InRealizationBattle = false;
                RealizationCombatLive = false;
                PendingRealizationBattle = false;
            }
            if (CompendiumOnlyLoadoutActive)
            {
                Debug.LogWarning("[RMRRealizationManager] PrepareNewHubSession found stale atlas-only loadout — restoring route snapshot.");
                try { RestoreRouteLoadout(); }
                catch (Exception ex) { Debug.LogWarning("[RMRRealizationManager] Stale loadout restore failed: " + ex.Message); }
            }

            PendingLaunchIntent = RMRLaunchIntent.None;
            RMRStartHubPanel.SyncLaunchIntentMirror(RMRLaunchIntent.None);
            PendingRealizationFloor = null;
            AwaitingRealizationFloorPick = false;
            RealizationReceptionActive = false;
            RMRCore.ResetPostInvitationLaunchGate();
            // Kill any leaked 10-floor list / overlay blocking RMR entry clicks.
            try { LogRealizationPanel.ForceCloseStatic(); } catch { }
            try { RMRRealizationLaunchHost.DestroyOverlayCompletely(); } catch { }
            BeginHubSession();
            Debug.Log("[RMRRealizationManager] PrepareNewHubSession — launch state reset.");
        }
        #endregion

        #region --- State & floors ---


        /// <summary>
        /// Clear intent mirrors only. Keeps <see cref="PendingRealizationFloor"/> when invite is in flight.
        /// </summary>
        public static void ClearLaunchIntentOnly()
        {
            PendingLaunchIntent = RMRLaunchIntent.None;
            RMRStartHubPanel.SyncLaunchIntentMirror(RMRLaunchIntent.None);
        }

        /// <summary>
        /// Set durable intent (+ hub mirror). Realization marks reception as non-roguelike bootstrap.
        /// </summary>
        public static void SetLaunchIntent(RMRLaunchIntent intent)
        {
            PendingLaunchIntent = intent;
            RMRStartHubPanel.SyncLaunchIntentMirror(intent);
            if (intent == RMRLaunchIntent.Realization)
                RealizationReceptionActive = true;
            else if (intent == RMRLaunchIntent.NormalPlay)
                RealizationReceptionActive = false;
            Debug.Log($"[RMRRealizationManager] LaunchIntent set to {intent}, realizationReception={RealizationReceptionActive}");
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// While true, <see cref="LogLikeMod.CheckStage"/> must return false (no full roguelike hooks).
        /// Covers realization bootstrap shell + prepare + live combat.
        /// </summary>
        public static bool ShouldSuppressRoguelikeStageChecks()
        {
            if (IsRealizationBootstrapPending())
                return true;
            if (PendingRealizationBattle || InRealizationBattle || IsRealizationPreparationActive)
                return true;
            return false;
        }
        #endregion

        #region --- State & floors ---


        /// <summary>
        /// Pre-combat window only: hub chose Realization, waiting floor / prepare, not yet live combat.
        /// Used to skip chapter intro and SparklingMirror — must NOT stay true for whole fight.
        /// </summary>
        public static bool IsRealizationBootstrapPending()
        {
            // Live combat is past bootstrap.
            if (InRealizationBattle || RealizationCombatLive)
                return false;

            if (PendingLaunchIntent == RMRLaunchIntent.Realization)
                return true;
            if (RMRStartHubPanel.LaunchIntent == RMRLaunchIntent.Realization)
                return true;
            if (PendingRealizationFloor.HasValue)
                return true;
            if (AwaitingRealizationFloorPick)
                return true;
            // Reception active but not yet in combat (includes Pending prepare).
            if (RealizationReceptionActive)
                return true;
            if (PendingRealizationBattle || IsRealizationPreparationActive)
                return true;
            return false;
        }
        #endregion

        #region --- Battle bootstrap ---


        /// <summary>
        /// Only block dummy 854 while the floor panel is up. Do NOT block on RealizationReceptionActive
        /// alone — that aborted StartBattle before OnWaveStart and prevented HandlePost from running.
        /// </summary>
        public static bool ShouldBlockDummyStartBattle()
        {
            if (PendingRealizationBattle || InRealizationBattle)
                return false;
            return AwaitingRealizationFloorPick;
        }
        #endregion

        #region --- State & floors ---


        /// <summary>Enter floor-pick mode on the -853 shell (no dummy combat, no mystery).</summary>
        public static void EnterRealizationFloorPickMode()
        {
            RealizationReceptionActive = true;
            AwaitingRealizationFloorPick = true;
            PendingRealizationBattle = false;
            PendingRealizationFloor = null;
            LogLikeMod.PauseBool = true;
            BeginHubSession();
        }

        /// <summary>
        /// Soft recover after a failed StartRealizationBattle: restore loadout if needed,
        /// re-open floor pick instead of GameOver (avoids "ended without fighting").
        /// </summary>
        public static void RecoverRealizationToFloorPick(string reason)
        {
            Debug.LogWarning("[RMRRealizationManager] RecoverRealizationToFloorPick: " + reason);
            PendingRealizationBattle = false;
            RealizationCombatLive = false;
            PendingRealizationFloor = null;
            LogLikeMod.PauseBool = true;
            RealizationReceptionActive = true;
            AwaitingRealizationFloorPick = true;
            BeginHubSession();
            // Do NOT OpenBattlePrepare here: after a failed stage bind it re-opens broken
            // battle chrome under the floor list (white portraits / emotion-bar NRE spam).
            // Dedicated overlay floor pick is enough to choose again.
            try
            {
                RMRRealizationLaunchHost.DestroyOverlayCompletely();
            }
            catch { }
            try { RMRRealizationLaunchHost.EnsureFloorPanelVisible(); }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] Recover floor panel failed: " + ex.Message);
            }
        }
        #endregion

        #region --- Battle bootstrap ---


        /// <summary>
        /// MonoMod blocks direct OpenBattlePrepare calls (MethodAccessException).
        /// Always invoke via reflection. Returns false if UI cannot open.
        /// </summary>
        public static bool TryOpenBattlePrepare()
        {
            if (_inOpenBattlePrepare)
                return false;
            _inOpenBattlePrepare = true;
            try
            {
                var ui = UI.UIController.Instance;
                if (ui == null)
                {
                    Debug.LogError("[RMRRealizationManager] TryOpenBattlePrepare: UIController.Instance is null.");
                    return false;
                }
                var method = AccessTools.Method(typeof(UI.UIController), "OpenBattlePrepare", Type.EmptyTypes);
                if (method == null)
                    method = AccessTools.Method(typeof(UI.UIController), "OpenBattlePrepare");
                if (method == null)
                {
                    Debug.LogError("[RMRRealizationManager] OpenBattlePrepare method not found.");
                    return false;
                }
                method.Invoke(ui, null);
                return true;
            }
            catch (Exception ex)
            {
                Exception inner = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null
                    ? tie.InnerException
                    : ex;
                Debug.LogError("[RMRRealizationManager] OpenBattlePrepare invoke failed: " + inner);
                return false;
            }
            finally
            {
                _inOpenBattlePrepare = false;
            }
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// True if the active stage looks like the RMR -853 bootstrap shell (闪光 / dummy).
        /// </summary>
        public static bool IsOnRoguelikeShellStage()
        {
            try
            {
                StageModel model = Singleton<StageController>.Instance?.GetStageModel();
                LorId id = model?.ClassInfo?.id ?? LorId.None;
                if (id == LorId.None)
                    return false;
                // Shell stages use mod package + -853 / -855.
                if (id.id == -853 || id.id == -855)
                    return true;
                if (!string.IsNullOrEmpty(id.packageId)
                    && (id.packageId == LogLikeMod.ModId || id.packageId == RMRCore.packageId)
                    && id.id < 0)
                    return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Called from OpenBattlePrepare prefix: if the engine is about to show the -853 shell
        /// prepare (闪光), bind the vanilla Floor Realization stage first so the player
        /// only sees Angela/Roland liberate-battle prepare.
        /// Returns true when this method already opened prepare (caller should skip original).
        /// </summary>
        public static bool TryRedirectShellPrepareToRealization()
        {
            if (_inOpenBattlePrepare)
                return false;
            if (InRealizationBattle || RealizationCombatLive)
                return false;
            if (PendingRealizationBattle && !IsOnRoguelikeShellStage())
                return false;

            SephirahType? floor = PendingRealizationFloor;
            if (!floor.HasValue
                && PendingLaunchIntent == RMRLaunchIntent.Realization
                && CurrentRealizationFloor != default(SephirahType)
                && IsRealizationPreparationActive)
            {
                floor = CurrentRealizationFloor;
            }

            bool wantRealization = RealizationReceptionActive
                || PendingLaunchIntent == RMRLaunchIntent.Realization
                || RMRStartHubPanel.LaunchIntent == RMRLaunchIntent.Realization;
            if (!wantRealization || !floor.HasValue)
                return false;
            if (!IsOnRoguelikeShellStage())
                return false;

            Debug.Log($"[RMRRealizationManager] Redirecting shell prepare → vanilla Floor Realization ({floor.Value})");
            try
            {
                StartRealizationBattle(floor.Value);
                return PendingRealizationBattle || IsRealizationPreparationActive;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RMRRealizationManager] Shell→realization redirect failed: " + ex);
                return false;
            }
        }
        #endregion

        #region --- State & floors ---


        /// <summary>
        /// Invitation send aborted (compat mods / missing panel) — drop launch intent so hub stays clean.
        /// Keeps HubSessionActive so the player can choose again.
        /// </summary>
        public static void RollbackLaunchIntentKeepHub()
        {
            PendingLaunchIntent = RMRLaunchIntent.None;
            RMRStartHubPanel.SyncLaunchIntentMirror(RMRLaunchIntent.None);
            PendingRealizationFloor = null;
            AwaitingRealizationFloorPick = false;
            RealizationReceptionActive = false;
            Debug.Log("[RMRRealizationManager] RollbackLaunchIntentKeepHub — intent cleared, hub session kept.");
        }
        #endregion

        #region --- Battle bootstrap ---


        /// <summary>
        /// Realization start failed. Prefer soft recover to floor pick; only GameOver on hard exit.
        /// </summary>
        /// <param name="restoreLoadout">Restore atlas snapshot if loadout was applied.</param>
        /// <param name="forceExitToLibrary">If true, skip floor-pick recover and leave reception.</param>
        public static void FailRealizationStart(string reason, bool restoreLoadout, bool forceExitToLibrary = false)
        {
            Debug.LogError("[RMRRealizationManager] FailRealizationStart: " + reason);
            PendingRealizationBattle = false;
            RealizationCombatLive = false;
            PendingRealizationFloor = null;
            if (restoreLoadout)
            {
                try { RestoreRouteLoadout(); }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RMRRealizationManager] FailRealizationStart restore failed: " + ex.Message);
                }
            }

            // Soft path: player can pick another floor (still in bootstrap shell).
            if (!forceExitToLibrary && (HubSessionActive || RealizationReceptionActive) && !NormalPlayStarted)
            {
                RecoverRealizationToFloorPick(reason);
                return;
            }

            // Hard exit to library.
            AwaitingRealizationFloorPick = false;
            LogLikeMod.PauseBool = false;
            EndHubSessionToLibrary();
            try
            {
                StageController controller = Singleton<StageController>.Instance;
                if (controller != null)
                    controller.GameOver(false, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] FailRealizationStart GameOver failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Legacy name: realization is allowed only from the start hub before normal play.
        /// Prefer <see cref="CanEnterRealizationFromHub"/> in new code.
        /// </summary>
        public static bool InitialRelicEntryAvailable => CanEnterRealizationFromHub();
        #endregion

        #region --- Other helpers ---


        public static bool CanEnterRealizationFromHub()
        {
            return HubSessionActive && !NormalPlayStarted;
        }

        public static void BeginHubSession()
        {
            HubSessionActive = true;
            NormalPlayStarted = false;
        }
        #endregion

        #region --- Battle bootstrap ---


        public static void StartNormalPlayFromHub()
        {
            NormalPlayStarted = true;
            HubSessionActive = false;
            RealizationReceptionActive = false;
            PendingRealizationFloor = null;
            AwaitingRealizationFloorPick = false;
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Realization finished or cancelled: return to library main. One challenge per invitation
        /// (re-challenge requires a new mod entry / hub). Not a multi-floor hub loop.
        /// </summary>
        public static void EndHubSessionToLibrary()
        {
            HubSessionActive = false;
            NormalPlayStarted = false;
            RealizationReceptionActive = false;
            PendingRealizationFloor = null;
            AwaitingRealizationFloorPick = false;
            ClearLaunchIntentOnly();
            try { LogRealizationPanel.ForceCloseStatic(); } catch { }
        }

        /// <summary>
        /// Legacy bridge: true opens a hub session; false marks normal play (closes realization entry).
        /// </summary>
        public static void SetInitialRelicEntryAvailable(bool available)
        {
            if (available)
                BeginHubSession();
            else
                StartNormalPlayFromHub();
        }

        private static bool CompendiumOnlyLoadoutActive;
        private static List<BookModel> RouteBookSnapshot;
        private static List<DiceCardItemModel> RouteCardSnapshot;
        private static List<UnitDataModel> RoutePlayerModelSnapshot;
        private static List<UnitBattleDataModel> RoutePlayerBattleModelSnapshot;
        private static Dictionary<UnitDataModel, List<LorId>> RoutePlayersPickSnapshot;
        private static Dictionary<UnitDataModel, List<LorId>> RoutePlayersPerPassivesSnapshot;
        private static Dictionary<UnitDataModel, List<LogStatAdder>> RoutePlayersStatAddersSnapshot;
        private static abcdcode_LOGLIKE_MOD.StageType RouteStageTypeSnapshot;
        private static LorId RouteStageIdSnapshot;
        private static List<EmotionCardXmlInfo> RouteNextListSnapshot;
        private static List<DropBookXmlInfo> RouteRewardsSnapshot;
        private static List<RewardInfo> RouteRewardsPassiveSnapshot;
        private static List<RewardInfo> RouteRewardsInStageSnapshot;
        private static ChapterGrade RouteChapterGradeSnapshot;
        private static int RouteChStageStepSnapshot;

        // Deep per-unit state for proper restoration after realization battle.
        // Units are identified by their index in the original playerModel list.
        private sealed class UnitDeepState
        {
            public LorId EquippedBookId;
            public List<LorId> DeckCardIds;
            public string CustomName;
        }
        private static List<UnitDeepState> RouteUnitDeepStates;
        /// <summary>
        /// Resolve the vanilla Floor Realization stage for a floor (Angela/Roland multi-phase).
        /// Only the final stage id (e.g. 201005) — never 201001–201004 abno suppressions.
        /// </summary>
        public static bool TryResolveRealizationStage(
            SephirahType floor,
            out LorId stageId,
            out StageClassInfo stageClassInfo,
            out string reason)
        {
            stageId = LorId.None;
            stageClassInfo = null;
            reason = null;

            if (!RealizationBossStageIds.TryGetValue(floor, out int bossStageIdNum))
            {
                reason = $"No realization stage ID mapping for floor: {floor}";
                return false;
            }

            stageClassInfo = GetVanillaCreatureStage(bossStageIdNum);
            if (stageClassInfo != null && stageClassInfo.waveList != null && stageClassInfo.waveList.Count > 0)
            {
                stageId = stageClassInfo.id != LorId.None
                    ? stageClassInfo.id
                    : new LorId(string.Empty, bossStageIdNum);
                reason = null;
                Debug.Log($"[RMRRealizationManager] Resolved vanilla Floor Realization stage for {floor}: {stageId.packageId}:{stageId.id} (Angela/Roland multi-phase, not abno chain)");
                return true;
            }

            reason = $"Realization stage {bossStageIdNum} for floor {floor} has no valid StageClassInfo in StageClassInfoList.";
            return false;
        }

        private static StageClassInfo GetVanillaCreatureStage(int stageIdNum)
        {
            var list = Singleton<StageClassInfoList>.Instance;
            if (list == null)
                return null;
            foreach (var candidate in new[]
            {
                new LorId(string.Empty, stageIdNum),
                new LorId("@origin", stageIdNum),
                new LorId("BaseGame", stageIdNum),
                new LorId(LogLikeMod.ModId, stageIdNum),
            })
            {
                StageClassInfo info = list.GetData(candidate);
                if (info != null && info.waveList != null && info.waveList.Count > 0)
                    return info;
            }
            try
            {
                StageClassInfo byInt = list.GetData(stageIdNum);
                if (byInt != null && byInt.waveList != null && byInt.waveList.Count > 0)
                    return byInt;
            }
            catch { }
            return null;
        }
        #endregion

        #region --- Battle bootstrap ---


        /// <summary>
        /// Product flow: pick floor → team prepare → one vanilla Floor Realization stage.
        /// Multi-phase (Angela/Roland E.G.O forms) is handled entirely by vanilla combat scripts.
        /// </summary>
        public static void StartRealizationBattle(SephirahType floor)
        {
            if (!CanEnterRealizationFromHub())
            {
                FailRealizationStart(
                    "Realization gate closed (HubSessionActive=false or NormalPlayStarted).",
                    restoreLoadout: false,
                    forceExitToLibrary: true);
                return;
            }

            if (!TryResolveRealizationStage(floor, out LorId stageId, out StageClassInfo stageClassInfo, out string reason))
            {
                FailRealizationStart("Cannot resolve Floor Realization stage: " + reason, restoreLoadout: false, forceExitToLibrary: false);
                return;
            }

            RealizationReceptionActive = true;
            ForceReturnAsDefeatPending = false;
            PendingReturnToHub = false;
            RealizationCombatLive = false;
            AwaitingRealizationFloorPick = false;
            PendingRealizationFloor = null;
            LogLikeMod.PauseBool = false;
            PendingRealizationBattle = true;
            CurrentRealizationFloor = floor;

            try
            {
                if (RMRStartHubPanel.Instance != null && RMRStartHubPanel.Instance.IsVisible)
                    RMRStartHubPanel.Instance.Hide();
                if (LogRealizationPanel.Instance != null && LogRealizationPanel.Instance.IsVisible)
                    LogRealizationPanel.Instance.Hide();
                try { RMRRealizationLaunchHost.DestroyOverlayCompletely(); } catch { }
            }
            catch { }

            if (!ApplyCompendiumOnlyLoadout())
            {
                FailRealizationStart("Atlas-only loadout failed (no core pages?).", restoreLoadout: false, forceExitToLibrary: false);
                return;
            }

            SuppressRoguelikeSelectionUi();

            if (!TryStartVanillaRealizationStage(floor, stageId, stageClassInfo))
            {
                FailRealizationStart(
                    $"Failed to start Floor Realization stage: {stageId.packageId}:{stageId.id}",
                    restoreLoadout: true,
                    forceExitToLibrary: true);
                return;
            }
            Debug.Log($"[RMRRealizationManager] Started vanilla Floor Realization: {floor} stage={stageId.id} (prepare → fight Angela/Roland phases)");
        }

        /// <summary>
        /// Leave battle scene after realization complete / defeat. Avoids dark battle UI freeze.
        /// </summary>
        public static void EnsureExitBattleToLibrary()
        {
            LogLikeMod.PauseBool = false;
            try { LogRealizationPanel.ForceCloseStatic(); } catch { }
            try { RMRRealizationLaunchHost.DestroyOverlayCompletely(); } catch { }

            ForceReturnAsDefeatPending = true;
            try
            {
                StageController controller = Singleton<StageController>.Instance;
                if (controller != null)
                    controller.GameOver(false, true);
                else
                    ConsumeForceReturnAsDefeat();
            }
            catch (Exception ex)
            {
                ConsumeForceReturnAsDefeat();
                Debug.LogError("[RMRRealizationManager] EnsureExitBattleToLibrary GameOver failed: " + ex);
            }

            // Vanilla Floor Realization victory often opens 指定司书剧情 / story archives on top of
            // the library. Our item-catalog postfix used to throw FieldAccessException on
            // tabcontroller and leave that panel stuck. Dismiss it after the return.
            try { RMRCore.ForceDismissStoryArchivesAndReturnMain(); } catch { }
            try
            {
                // Second pass after a short delay: EndBattle/story callbacks may re-open archives after GameOver.
                // Prefer UIController (survives leave-battle); fall back to BattleManagerUI if still alive.
                MonoBehaviour runner = UI.UIController.Instance as MonoBehaviour;
                if (runner == null)
                    runner = SingletonBehavior<BattleManagerUI>.Instance;
                if (runner != null)
                    runner.StartCoroutine(CoDismissStoryArchivesDelayed());
            }
            catch
            {
                try { RMRCore.ForceDismissStoryArchivesAndReturnMain(); } catch { }
            }
        }
        #endregion

        #region --- Other helpers ---


        private static System.Collections.IEnumerator CoDismissStoryArchivesDelayed()
        {
            yield return null;
            yield return null;
            try { RMRCore.ForceDismissStoryArchivesAndReturnMain(); } catch { }
            yield return new WaitForSeconds(0.35f);
            try { RMRCore.ForceDismissStoryArchivesAndReturnMain(); } catch { }
        }
        #endregion

        #region --- UI / floor pick ---


        /// <summary>
        /// Clear next-stage / reward selection state and hide LevelUpUI so realization entry
        /// does not show an extra Roguelike reception picker.
        /// </summary>
        public static void SuppressRoguelikeSelectionUi()
        {
            try
            {
                if (LogLikeMod.nextlist != null)
                    LogLikeMod.nextlist.Clear();
                else
                    LogLikeMod.nextlist = new List<EmotionCardXmlInfo>();
                if (LogLikeMod.rewards != null) LogLikeMod.rewards.Clear();
                if (LogLikeMod.rewards_passive != null) LogLikeMod.rewards_passive.Clear();
                if (LogLikeMod.rewards_InStage != null) LogLikeMod.rewards_InStage.Clear();
                if (LogLikeMod.rewardsMystery != null) LogLikeMod.rewardsMystery.Clear();
                if (LogLikeMod.egoSelectionQueue != null) LogLikeMod.egoSelectionQueue.Clear();
                RewardingModel.rewardFlag = default(RewardingModel.RewardFlag);
                LogLikeRoutines.HideRewardSelectionImmediately(
                    SingletonBehavior<BattleManagerUI>.Instance != null
                        ? SingletonBehavior<BattleManagerUI>.Instance.ui_levelup
                        : null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] SuppressRoguelikeSelectionUi: " + ex.Message);
            }
        }
        #endregion

        #region --- State & floors ---


        /// <summary>
        /// Leave floor-pick mode on -853 shell (close panel / back) → library.
        /// Must clear Awaiting; otherwise StartBattle stays aborted forever.
        /// </summary>
        public static void CancelRealizationFloorPick()
        {
            if (InRealizationBattle || RealizationCombatLive)
                return;
            if (!AwaitingRealizationFloorPick && !RealizationReceptionActive)
            {
                // Still force-kill UI if a ghost panel is on screen.
                try { LogRealizationPanel.ForceCloseStatic(); } catch { }
                return;
            }

            Debug.Log("[RMRRealizationManager] CancelRealizationFloorPick → library.");
            try { LogRealizationPanel.ForceCloseStatic(); } catch { }

            PendingRealizationBattle = false;
            RealizationCombatLive = false;
            AwaitingRealizationFloorPick = false;
            PendingRealizationFloor = null;
            ForceReturnAsDefeatPending = false;
            PendingReturnToHub = false;
            LogLikeMod.PauseBool = false;
            SuppressRoguelikeSelectionUi();
            EndHubSessionToLibrary();
            try
            {
                StageController controller = Singleton<StageController>.Instance;
                if (controller != null)
                    controller.GameOver(false, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] CancelFloorPick GameOver failed: " + ex.Message);
            }
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Back out of battle prepare without starting the fight; restore route → library.
        /// Also covers floor-pick mode (Awaiting) so back/close never leaves a stuck shell.
        /// </summary>
        public static void CancelRealizationPreparation()
        {
            if (InRealizationBattle)
                return;

            // Floor pick only (no atlas loadout yet).
            if (AwaitingRealizationFloorPick && !PendingRealizationBattle && !IsRealizationPreparationActive)
            {
                CancelRealizationFloorPick();
                return;
            }

            if (!PendingRealizationBattle && !IsRealizationPreparationActive)
                return;

            Debug.Log("[RMRRealizationManager] Cancelling realization preparation (back/exit).");
            PendingRealizationBattle = false;
            ForceReturnAsDefeatPending = false;
            PendingReturnToHub = false;
            AwaitingRealizationFloorPick = false;
            try { RestoreRouteLoadout(); }
            catch (Exception ex) { Debug.LogWarning("[RMRRealizationManager] Cancel restore failed: " + ex.Message); }
            SuppressRoguelikeSelectionUi();

            // Cancel prepare → return to library main (no in-run mode menu / no multi-floor hub).
            LogLikeMod.PauseBool = false;
            EndHubSessionToLibrary();
            try
            {
                StageController controller = Singleton<StageController>.Instance;
                if (controller != null)
                    controller.GameOver(false, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] Cancel GameOver failed: " + ex.Message);
            }
        }
        #endregion

        #region --- Battle bootstrap ---


        private static bool TryStartVanillaRealizationStage(
            SephirahType floor,
            LorId stageId,
            StageClassInfo stageClassInfo)
        {
            if (stageClassInfo == null)
                return false;

            // Floor realization uses up to 5 librarians; do not force if stage already sets AvailableUnit.
            if (stageClassInfo.waveList != null)
            {
                foreach (StageWaveInfo w in stageClassInfo.waveList)
                {
                    if (w == null) continue;
                    // Only set if unset / zero so we don't break Keter AvailableUnit=1 etc.
                    if (w.availableNumber <= 0)
                        ForceWaveAvailableUnits(w, 5);
                }
            }

            try
            {
                Singleton<MysteryManager>.Instance.EndMystery();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] Realization transition mystery finish failed: " + ex.Message);
            }

            try
            {
                StageController controller = Singleton<StageController>.Instance;
                if (controller == null)
                    return false;

                // --- Mirror vanilla UICreatureRebattle / OnClickStartCreatureStage ---
                // 1) Sephirah context for floor-only + map managers
                controller.SetCurrentSephirah(floor);

                // 2) Floor temporary level = 5 (FloorLevelXml maps Level 5 → final realization stage)
                //    Vanilla rebattle path always SetTemporaryLevel before InitStageByCreature.
                try
                {
                    LibraryFloorModel libFloor = LibraryModel.Instance?.GetFloor(floor);
                    if (libFloor != null)
                    {
                        libFloor.SetTemporaryLevel(5);
                        Debug.Log($"[RMRRealizationManager] SetTemporaryLevel(5) on {floor}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RMRRealizationManager] SetTemporaryLevel failed: " + ex.Message);
                }

                // 3) isRebattle=true — same as final-battle rebattle slot. Required for proper
                //    creature-final setup; false was used before and multi-phase (Corrosion) broke.
                controller.InitStageByCreature(stageClassInfo, true);

                StageModel stageModel = controller.GetStageModel();
                ForceRealizationStageModelAvailableUnits(stageModel, 5);

                // 4) Vanilla marks floor unit battle data as added — keep for stage floor model.
                try
                {
                    StageLibraryFloorModel stageFloor = controller.GetCurrentStageFloorModel();
                    var units = stageFloor?.GetUnitBattleDataList();
                    if (units != null)
                    {
                        foreach (UnitBattleDataModel ub in units)
                        {
                            if (ub != null)
                                ub.IsAddedBattle = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RMRRealizationManager] Mark IsAddedBattle failed: " + ex.Message);
                }

                LorId currentId = stageModel?.ClassInfo?.id ?? LorId.None;
                if (currentId == LorId.None && (stageModel?.waveList == null || stageModel.waveList.Count == 0))
                {
                    Debug.LogError($"[RMRRealizationManager] Realization StageModel bind failed for {stageId.packageId}:{stageId.id}");
                    return false;
                }

                LogLikeMod.curstageid = stageId;
                LogLikeMod.curstagetype = abcdcode_LOGLIKE_MOD.StageType.Creature;
                Debug.Log($"[RMRRealizationManager] Bound vanilla Floor Realization: floor={floor} stage={stageId.id} isRebattle=true (multi-phase via unit passives/ManagerScript)");

                if (!TryOpenBattlePrepare())
                {
                    Debug.LogError("[RMRRealizationManager] Stage bound but OpenBattlePrepare failed: "
                        + stageId.packageId + ":" + stageId.id);
                    return false;
                }

                RequestAbortCurrentStartBattleIfInHook();
                Debug.Log($"[RMRRealizationManager] Battle prepare opened for Floor Realization {floor} stage {stageId.id}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RMRRealizationManager] Failed to start vanilla realization stage: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Final-form multiphase Angela books (EnemyUnitInfo_creature_final) → passive script id.
        /// Vanilla multiphase is a single wave; phase swaps happen in these passives via isImmortal + OnRoundEndTheLast.
        /// </summary>
        private static readonly Dictionary<int, string> MultiphaseBossBookToPassive = new Dictionary<int, string>
        {
            // Confirmed CreaturePhase multiphase passives (single-unit Angela E.G.O forms).
            { 9010511, "105010" }, // Malkuth Angela
            { 9020511, "205010" }, // Yesod Angela
            // Hod/Gebura/Binah/etc. use ManagerScript + multi-unit phase; guarded via IsStageFinishable / IsImmortal.
        };
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// True while Angela/Roland multi-phase is still mid-fight (immortal boss not on final phase).
        /// EndBattle during this window must not grant floor clear or exit to library.
        /// </summary>
        public static bool IsMidRealizationMultiPhase()
        {
            try
            {
                // ManagerScript floors (GeburaFinal, HodFinalBattle, BinahFinal, …):
                // while !IsStageFinishable, phase transition is still in progress even if unit list is empty briefly.
                try
                {
                    var sc = Singleton<StageController>.Instance;
                    var mgr = sc?.EnemyStageManager;
                    if (mgr != null && !mgr.IsStageFinishable())
                        return true;
                }
                catch { }

                var bom = BattleObjectManager.instance;
                if (bom == null)
                    return false;

                // Scan full enemy list (includes units at 1 HP mid-phase transition).
                var enemies = bom.GetList(Faction.Enemy);
                if (enemies == null)
                    return false;

                foreach (BattleUnitModel unit in enemies)
                {
                    if (unit == null)
                        continue;

                    if (IsUnitInMultiphaseTransition(unit))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] IsMidRealizationMultiPhase: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Whether this enemy is mid multiphase (must not Die / must not EndBattle as victory).
        /// </summary>
        public static bool IsUnitInMultiphaseTransition(BattleUnitModel unit)
        {
            if (unit == null)
                return false;

            // Vanilla IsImmortal() aggregates passiveDetail + bufListDetail.
            try
            {
                if (unit.IsImmortal())
                    return true;
            }
            catch { }

            // Phase passives (Malkuth 105010, Yesod 205010, …). Final phase enum value is 4.
            try
            {
                foreach (object p in EnumeratePassives(unit))
                {
                    if (p == null) continue;
                    if (!IsMultiphasePassiveType(p.GetType()))
                        continue;

                    var phaseField = AccessTools.Field(p.GetType(), "_currentPhase");
                    if (phaseField != null)
                    {
                        int phase = Convert.ToInt32(phaseField.GetValue(p));
                        if (phase < 4)
                            return true;
                    }
                    else if (!unit.IsDead())
                    {
                        return true;
                    }
                }
            }
            catch { }

            // Known multiphase boss book missing phase passive / still before final form.
            // Do NOT treat final phase (or non-phase books) as mid-fight forever.
            try
            {
                if (unit.IsDead())
                    return false;
                int bookId = 0;
                try { bookId = unit.Book != null ? unit.Book.GetBookClassInfoId().id : 0; } catch { }
                if (bookId == 0)
                {
                    try { bookId = unit.UnitData.unitData.bookItem.GetBookClassInfoId().id; } catch { }
                }
                if (bookId != 0 && MultiphaseBossBookToPassive.ContainsKey(bookId))
                {
                    int? phase = TryGetMultiphasePhase(unit);
                    // No readable phase ⇒ assume mid (passive missing or not started).
                    // phase < 4 ⇒ still multiphase.
                    if (!phase.HasValue || phase.Value < 4)
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static int? TryGetMultiphasePhase(BattleUnitModel unit)
        {
            try
            {
                foreach (object p in EnumeratePassives(unit))
                {
                    if (p == null || !IsMultiphasePassiveType(p.GetType()))
                        continue;
                    var phaseField = AccessTools.Field(p.GetType(), "_currentPhase");
                    if (phaseField != null)
                        return Convert.ToInt32(phaseField.GetValue(p));
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Block Die() on multiphase realization bosses until their final form.
        /// Vanilla relies on TakeDamage→IsImmortal clamp; if immortal fails, Die ends the fight after phase 1.
        /// </summary>
        public static bool ShouldBlockRealizationBossDeath(BattleUnitModel unit)
        {
            if (!InRealizationBattle || unit == null)
                return false;
            if (unit.faction != Faction.Enemy)
                return false;
            return IsUnitInMultiphaseTransition(unit);
        }
        #endregion

        #region --- State & floors ---


        /// <summary>
        /// Called when combat goes live / each round: ensure multiphase passives exist and log state.
        /// </summary>
        public static void EnsureRealizationMultiPhaseBossState()
        {
            if (!InRealizationBattle)
                return;
            try
            {
                var bom = BattleObjectManager.instance;
                if (bom == null)
                    return;
                var enemies = bom.GetList(Faction.Enemy);
                if (enemies == null)
                    return;

                foreach (BattleUnitModel unit in enemies)
                {
                    if (unit == null || unit.IsDead())
                        continue;

                    int bookId = 0;
                    try
                    {
                        if (unit.Book != null)
                            bookId = unit.Book.GetBookClassInfoId().id;
                        if (bookId == 0 && unit.UnitData?.unitData?.bookItem != null)
                            bookId = unit.UnitData.unitData.bookItem.GetBookClassInfoId().id;
                    }
                    catch { }

                    bool hasPhasePassive = false;
                    bool immortal = false;
                    int phase = -1;
                    try { immortal = unit.IsImmortal(); } catch { }

                    foreach (object p in EnumeratePassives(unit))
                    {
                        if (p == null) continue;
                        if (!IsMultiphasePassiveType(p.GetType()))
                            continue;
                        hasPhasePassive = true;
                        var phaseField = AccessTools.Field(p.GetType(), "_currentPhase");
                        if (phaseField != null)
                            phase = Convert.ToInt32(phaseField.GetValue(p));
                    }

                    if (!hasPhasePassive && bookId != 0
                        && MultiphaseBossBookToPassive.TryGetValue(bookId, out string passiveScript))
                    {
                        if (TryInjectMultiphasePassive(unit, passiveScript))
                        {
                            hasPhasePassive = true;
                            try { immortal = unit.IsImmortal(); } catch { }
                            Debug.Log($"[RMRRealizationManager] Injected multiphase passive {passiveScript} on book {bookId}");
                        }
                        else
                        {
                            Debug.LogWarning($"[RMRRealizationManager] Failed to inject multiphase passive {passiveScript} on book {bookId}");
                        }
                    }

                    Debug.Log($"[RMRRealizationManager] Multiphase boss state: book={bookId} immortal={immortal} hasPhasePassive={hasPhasePassive} phase={phase} hp={unit.hp:F0}/{unit.MaxHp}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] EnsureRealizationMultiPhaseBossState: " + ex.Message);
            }
        }
        #endregion

        #region --- Other helpers ---


        private static bool TryInjectMultiphasePassive(BattleUnitModel unit, string passiveScriptId)
        {
            try
            {
                if (unit?.passiveDetail == null || string.IsNullOrEmpty(passiveScriptId))
                    return false;

                // Already present?
                foreach (object p in EnumeratePassives(unit))
                {
                    if (p != null && p.GetType().Name.Contains(passiveScriptId))
                        return true;
                }

                PassiveAbilityBase created = null;
                try
                {
                    created = Singleton<AssemblyManager>.Instance.CreateInstance_PassiveAbility(passiveScriptId);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RMRRealizationManager] CreateInstance_PassiveAbility failed: " + ex.Message);
                }

                if (created == null)
                {
                    // Fallback: construct PassiveAbility_XXXXXX via reflection.
                    string typeName = "PassiveAbility_" + passiveScriptId;
                    Type t = AccessTools.TypeByName(typeName)
                        ?? typeof(PassiveAbilityBase).Assembly.GetType(typeName);
                    if (t != null)
                        created = Activator.CreateInstance(t) as PassiveAbilityBase;
                }

                if (created == null)
                    return false;

                try
                {
                    created.id = new LorId(int.Parse(passiveScriptId));
                }
                catch { }

                PassiveAbilityBase added = unit.passiveDetail.AddPassive(created);
                PassiveAbilityBase live = added ?? created;
                try { live.Init(unit); } catch { }
                try { live.OnWaveStart(); } catch { }
                try { live.OnUnitCreated(); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] TryInjectMultiphasePassive: " + ex.Message);
                return false;
            }
        }

        private static bool IsMultiphasePassiveType(Type t)
        {
            if (t == null)
                return false;
            string n = t.Name;
            // Known ids + any passive that owns CreaturePhase / _currentPhase multiphase field.
            if (n == "PassiveAbility_105010" || n == "PassiveAbility_205010"
                || n == "PassiveAbility_305010" || n == "PassiveAbility_405010"
                || n == "PassiveAbility_505010" || n == "PassiveAbility_605010"
                || n == "PassiveAbility_705010" || n == "PassiveAbility_805010"
                || n == "PassiveAbility_905010")
                return true;
            if (AccessTools.Field(t, "_currentPhase") != null
                && (n.StartsWith("PassiveAbility_1") || n.StartsWith("PassiveAbility_2")
                    || n.StartsWith("PassiveAbility_3") || n.StartsWith("PassiveAbility_4")
                    || n.StartsWith("PassiveAbility_5") || n.StartsWith("PassiveAbility_6")
                    || n.StartsWith("PassiveAbility_7") || n.StartsWith("PassiveAbility_8")
                    || n.StartsWith("PassiveAbility_9") || n.StartsWith("PassiveAbility_10")))
                return true;
            // Nested CreaturePhase enum is a strong signal (Malkuth/Yesod style).
            foreach (Type nested in t.GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (nested.Name == "CreaturePhase")
                    return true;
            }
            return false;
        }

        private static IEnumerable<object> EnumeratePassives(BattleUnitModel unit)
        {
            if (unit?.passiveDetail == null)
                yield break;

            System.Collections.IEnumerable plist = null;
            try
            {
                // Prefer public PassiveList if available.
                var prop = AccessTools.Property(unit.passiveDetail.GetType(), "PassiveList")
                    ?? AccessTools.Property(unit.passiveDetail.GetType(), "passiveList");
                plist = prop?.GetValue(unit.passiveDetail, null) as System.Collections.IEnumerable;
            }
            catch { }

            if (plist == null)
            {
                try
                {
                    var listField = AccessTools.Field(unit.passiveDetail.GetType(), "_passiveList")
                        ?? AccessTools.Field(unit.passiveDetail.GetType(), "passiveList");
                    plist = listField?.GetValue(unit.passiveDetail) as System.Collections.IEnumerable;
                }
                catch { }
            }

            if (plist == null)
                yield break;

            foreach (object p in plist)
                yield return p;
        }

        private static void ForceRealizationAvailableUnits(StageClassInfo stageClassInfo, int count)
        {
            if (stageClassInfo?.waveList == null)
                return;
            foreach (StageWaveInfo wave in stageClassInfo.waveList)
                ForceWaveAvailableUnits(wave, count);
        }

        private static void ForceRealizationStageModelAvailableUnits(StageModel stageModel, int count)
        {
            if (stageModel?.waveList == null)
                return;
            foreach (StageWaveModel wave in stageModel.waveList)
                ForceStageWaveModelAvailableUnits(wave, count);
        }
        #endregion

        #region --- Battle bootstrap ---


        private static void ForceWaveAvailableUnits(StageWaveInfo wave, int count)
        {
            if (wave == null)
                return;
            Type type = wave.GetType();
            foreach (string fieldName in new[] { "availableNumber", "availableUnit", "_availableUnit", "AvailableUnit" })
            {
                try
                {
                    var field = type.GetField(fieldName, AccessTools.all);
                    if (field != null && field.FieldType == typeof(int))
                    {
                        field.SetValue(wave, count);
                        return;
                    }
                }
                catch { }
            }
            foreach (string propertyName in new[] { "AvailableUnit", "availableUnit" })
            {
                try
                {
                    var property = type.GetProperty(propertyName, AccessTools.all);
                    if (property != null && property.CanWrite && property.PropertyType == typeof(int))
                    {
                        property.SetValue(wave, count, null);
                        return;
                    }
                }
                catch { }
            }
        }

        private static void ForceStageWaveModelAvailableUnits(StageWaveModel wave, int count)
        {
            if (wave == null)
                return;
            try
            {
                var field = wave.GetType().GetField("_availableUnitNumber", AccessTools.all);
                if (field != null && field.FieldType == typeof(int))
                    field.SetValue(wave, count);
            }
            catch { }
        }

        /// <summary>
        /// Vanilla Floor Realization stage fully finished (all Angela/Roland phases done, or defeat).
        /// In-battle phase swaps must NOT call this — only real Stage EndBattle.
        /// </summary>
        public static void OnRealizationBattleEnded(bool victory)
        {
            if (!InRealizationBattle && !RealizationCombatLive)
                return;

            if (!RealizationCombatLive)
            {
                Debug.LogWarning("[RMRRealizationManager] Ignoring EndBattle before realization combat went live (no hub return).");
                return;
            }

            if (victory)
            {
                RMRAbnormalityUnlockManager.CompleteFloorRealization(CurrentRealizationFloor);
                Debug.Log($"[RMRRealizationManager] Floor Realization victory: {CurrentRealizationFloor}");
            }
            else
            {
                Debug.Log($"[RMRRealizationManager] Floor Realization defeat: {CurrentRealizationFloor}");
            }

            LogLikeMod.rewards?.Clear();
            LogLikeMod.rewards_passive?.Clear();
            LogLikeMod.rewards_InStage?.Clear();
            LogLikeMod.nextlist?.Clear();
            SuppressRoguelikeSelectionUi();

            try { RestoreRouteLoadout(); }
            catch (Exception ex) { Debug.LogWarning("[RMRRealizationManager] Restore after realization failed: " + ex.Message); }

            PendingReturnToHub = false;
            EndHubSessionToLibrary();
            RealizationCombatLive = false;
            CurrentRealizationFloor = SephirahType.Keter;

            Debug.Log($"[RMRRealizationManager] Realization battle ended. Victory={victory}. Returning to library.");
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// After vanilla EndBattle: hub is invitation-time only. No re-open here.
        /// </summary>
        public static void ReturnToMainAfterRealization()
        {
            PendingReturnToHub = false;
            try
            {
                if (RMRStartHubPanel.Instance != null && RMRStartHubPanel.Instance.IsVisible)
                    RMRStartHubPanel.Instance.Hide();
            }
            catch { }
            try { RMRCore.ForceDismissStoryArchivesAndReturnMain(); } catch { }
            Debug.Log("[RMRRealizationManager] ReturnToMainAfterRealization — library main via vanilla flow.");
        }
        #endregion

        #region --- Save / progress ---


        private static void EnsureDefaultRealizationCompendiumUnlocks()
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            bool changed = false;

            for (int id = -854; id >= -858; id--)
            {
                LorId bookId = new LorId(LogLikeMod.ModId, id);
                if (Singleton<BookXmlList>.Instance.GetData(bookId) != null)
                    changed |= LogueBookModels.CompendiumUnlockedRoleBooks.Add(bookId);
            }

            for (int id = -10; id >= -14; id--)
            {
                LorId cardId = new LorId(LogLikeMod.ModId, id);
                if (ItemXmlDataList.instance.GetCardItem(cardId, true) != null)
                    changed |= LogueBookModels.CompendiumUnlockedBattleCards.Add(cardId);
            }

            if (changed)
                // Save only the fallback atlas entries; the broader save path would
                // also sync the current route inventory into the permanent atlas.
                LogueBookModels.SavePermanentCompendiumData();
        }


        private static bool ApplyCompendiumOnlyLoadout()
        {
            if (CompendiumOnlyLoadoutActive)
                return true;
            LogueBookModels.EnsureCompendiumUnlocks();
            if (LogueBookModels.CompendiumUnlockedRoleBooks == null)
                LogueBookModels.CompendiumUnlockedRoleBooks = new HashSet<LorId>();
            if (LogueBookModels.CompendiumUnlockedBattleCards == null)
                LogueBookModels.CompendiumUnlockedBattleCards = new HashSet<LorId>();
            EnsureDefaultRealizationCompendiumUnlocks();
            LogueBookModels.RestoreCompendiumUpgradeDefinitions();

            // --- Save full route state ---
            RouteBookSnapshot = LogueBookModels.booklist == null ? new List<BookModel>() : new List<BookModel>(LogueBookModels.booklist);
            RouteCardSnapshot = LogueBookModels.cardlist == null ? new List<DiceCardItemModel>() : new List<DiceCardItemModel>(LogueBookModels.cardlist);
            RoutePlayerModelSnapshot = LogueBookModels.playerModel == null ? new List<UnitDataModel>() : new List<UnitDataModel>(LogueBookModels.playerModel);
            RoutePlayerBattleModelSnapshot = LogueBookModels.playerBattleModel == null ? new List<UnitBattleDataModel>() : new List<UnitBattleDataModel>(LogueBookModels.playerBattleModel);
            RoutePlayersPickSnapshot = LogueBookModels.playersPick == null ? new Dictionary<UnitDataModel, List<LorId>>() : new Dictionary<UnitDataModel, List<LorId>>(LogueBookModels.playersPick);
            RoutePlayersPerPassivesSnapshot = LogueBookModels.playersperpassives == null ? new Dictionary<UnitDataModel, List<LorId>>() : new Dictionary<UnitDataModel, List<LorId>>(LogueBookModels.playersperpassives);
            RoutePlayersStatAddersSnapshot = LogueBookModels.playersstatadders == null ? new Dictionary<UnitDataModel, List<LogStatAdder>>() : new Dictionary<UnitDataModel, List<LogStatAdder>>(LogueBookModels.playersstatadders);
            RouteStageTypeSnapshot = LogLikeMod.curstagetype;
            RouteStageIdSnapshot = LogLikeMod.curstageid;
            RouteNextListSnapshot = LogLikeMod.nextlist == null ? new List<EmotionCardXmlInfo>() : new List<EmotionCardXmlInfo>(LogLikeMod.nextlist);
            RouteRewardsSnapshot = LogLikeMod.rewards == null ? new List<DropBookXmlInfo>() : new List<DropBookXmlInfo>(LogLikeMod.rewards);
            RouteRewardsPassiveSnapshot = LogLikeMod.rewards_passive == null ? new List<RewardInfo>() : new List<RewardInfo>(LogLikeMod.rewards_passive);
            RouteRewardsInStageSnapshot = LogLikeMod.rewards_InStage == null ? new List<RewardInfo>() : new List<RewardInfo>(LogLikeMod.rewards_InStage);
            RouteChapterGradeSnapshot = LogLikeMod.curchaptergrade;
            RouteChStageStepSnapshot = LogLikeMod.curChStageStep;

            // --- Save deep per-unit state BEFORE mutating UnitDataModel objects ---
            RouteUnitDeepStates = new List<UnitDeepState>();
            if (LogueBookModels.playerModel != null)
            {
                foreach (var unit in LogueBookModels.playerModel)
                {
                    var ds = new UnitDeepState();
                    if (unit != null && unit.bookItem != null)
                    {
                        // Save the equipped book ID (the booklist entry that has owner == unit)
                        var ownedBook = LogueBookModels.booklist?.Find(b => b.owner == unit);
                        ds.EquippedBookId = ownedBook?.ClassInfo?.id ?? LorId.None;
                        // Save current deck card IDs
                        var deck = unit.bookItem.GetCardListFromCurrentDeck();
                        ds.DeckCardIds = deck != null ? new List<LorId>(deck.Select(c => c.id)) : new List<LorId>();
                        ds.CustomName = unit.name;
                    }
                    RouteUnitDeepStates.Add(ds);
                }
            }

            // --- Build atlas-only book/card lists ---
            List<BookModel> atlasBooks = new List<BookModel>();
            foreach (LorId id in LogueBookModels.CompendiumUnlockedRoleBooks)
            {
                BookXmlInfo book = Singleton<BookXmlList>.Instance.GetData(id);
                if (book == null)
                    continue;
                BookModel model = new BookModel(book);
                model.instanceId = LogueBookModels.nextinstanceid++;
                model.TryGainUniquePassive();
                atlasBooks.Add(model);
            }
            if (atlasBooks.Count == 0)
            {
                Debug.LogError("[RMRRealizationManager] Atlas-only realization loadout has no unlocked core pages, including starter fallback.");
                return false;
            }

            List<DiceCardItemModel> atlasCards = new List<DiceCardItemModel>();
            foreach (LorId id in LogueBookModels.CompendiumUnlockedBattleCards)
            {
                DiceCardXmlInfo card = RewardingModel.GetCardItemOriginAware(id);
                // EGO combat pages must never enter the realization deck inventory —
                // they are unusable as normal combat pages and block deck slots.
                if (card == null || !RMRPrepareRestrictions.IsAllowedInCombatDeckInventory(card))
                    continue;
                atlasCards.Add(new DiceCardItemModel(card) { num = LogueBookModels.UNLOCKED_CARD_COUNT });
            }

            LogueBookModels.booklist = atlasBooks;
            LogueBookModels.cardlist = atlasCards;

            // --- Build realization team: ensure 5 librarians ---
            int targetCount = 5;

            // Start with the route player list (will be mutated by EquipNewPage)
            var teamList = new List<UnitDataModel>(RoutePlayerModelSnapshot ?? new List<UnitDataModel>());
            LogueBookModels.playerModel = teamList;
            LogueBookModels.playerBattleModel = new List<UnitBattleDataModel>();
            LogueBookModels.playersPick = new Dictionary<UnitDataModel, List<LorId>>();
            LogueBookModels.playersperpassives = new Dictionary<UnitDataModel, List<LorId>>();
            LogueBookModels.playersstatadders = new Dictionary<UnitDataModel, List<LogStatAdder>>();
            foreach (UnitDataModel unit in teamList)
            {
                if (unit == null)
                    continue;
                LogueBookModels.playersPick[unit] = new List<LorId>();
                LogueBookModels.playersperpassives[unit] = new List<LorId>();
                LogueBookModels.playersstatadders[unit] = new List<LogStatAdder>();
            }

            // If we have fewer than 5, pad using AddSubPlayer
            while (teamList.Count < targetCount)
            {
                try
                {
                    LogueBookModels.AddSubPlayer();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RMRRealizationManager] AddSubPlayer failed: {ex.Message}");
                    break;
                }
                // AddSubPlayer appends to LogueBookModels.playerModel
                teamList = LogueBookModels.playerModel;
            }

            // Trim if somehow we have more than 5
            while (teamList.Count > targetCount)
                teamList.RemoveAt(teamList.Count - 1);

            LogueBookModels.playerModel = teamList;
            LogueBookModels.CreatePlayerBattle();
            foreach (UnitBattleDataModel battleModel in LogueBookModels.playerBattleModel)
            {
                if (battleModel != null)
                    battleModel.IsAddedBattle = true;
            }

            // Equip each librarian with an atlas book
            for (int i = 0; i < teamList.Count; i++)
            {
                var unit = teamList[i];
                // Use atlas book at matching index, or wrap around if fewer books than librarians
                var book = atlasBooks[i % atlasBooks.Count];
                if (unit != null && book != null)
                {
                    LogueBookModels.EquipNewPage(unit, book.ClassInfo, false);
                    // Skip auto-fill only for true built-in decks (e.g. Black Silence, Binah).
                    // OnlyCard pages still need atlas cards filled, otherwise realization
                    // battles can start with empty hands after temporary atlas projection.
                    bool hasBuiltInDeck = false;
                    try
                    {
                        bool editableBlue = LogueBookModels.IsEditableBlueReverberationDeck(unit.bookItem);
                        hasBuiltInDeck = !editableBlue
                            && (unit.bookItem?.IsFixedDeck() == true
                                || unit.bookItem?.IsLockByBluePrimary() == true);
                    }
                    catch { }
                    if (!hasBuiltInDeck)
                    {
                        var deck = unit.bookItem?.GetCardListFromCurrentDeck();
                        if (deck != null)
                        {
                            int deckCount = deck.Count;
                            int needed = 9 - deckCount;
                            int tried = 0;
                            int cardIdx = 0;
                            while (needed > 0 && tried < atlasCards.Count * 2 && cardIdx < atlasCards.Count)
                            {
                                var cardXml = atlasCards[cardIdx].ClassInfo;
                                if (cardXml != null && LogueBookModels.CanAddCardToCurrentDeck(cardXml.id, unit.bookItem))
                                {
                                    unit.AddCardFromInventory(cardXml.id);
                                    needed--;
                                }
                                cardIdx++;
                                if (cardIdx >= atlasCards.Count) cardIdx = 0;
                                tried++;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"[RMRRealizationManager] Skipped atlas auto-fill for built-in-deck book: {book.ClassInfo.id.id}");
                    }
                }
            }

            // Safety net: route snapshot / EquipNewPage may still leave EGO pages inside
            // current decks (e.g. atlas-recorded EGO ids). Move them back to inventory so
            // the realization battle never starts with unusable EGO combat cards equipped.
            try { RMRPrepareRestrictions.StripEgoPagesFromPlayerDecks(); }
            catch (Exception ex) { Debug.LogWarning("[RMRRealizationManager] StripEgoPagesFromPlayerDecks: " + ex.Message); }

            CompendiumOnlyLoadoutActive = true;
            Debug.Log($"[RMRRealizationManager] Applied atlas-only loadout: books={atlasBooks.Count}, cards={atlasCards.Count}, librarians={teamList.Count}");
            return true;
        }

        private static void RestoreRouteLoadout()
        {
            if (!CompendiumOnlyLoadoutActive)
                return;

            // --- Restore book/card lists ---
            LogueBookModels.booklist = RouteBookSnapshot ?? new List<BookModel>();
            LogueBookModels.cardlist = RouteCardSnapshot ?? new List<DiceCardItemModel>();
            LogueBookModels.playerModel = RoutePlayerModelSnapshot ?? new List<UnitDataModel>();
            LogueBookModels.playerBattleModel = RoutePlayerBattleModelSnapshot ?? new List<UnitBattleDataModel>();
            LogueBookModels.playersPick = RoutePlayersPickSnapshot ?? new Dictionary<UnitDataModel, List<LorId>>();
            LogueBookModels.playersperpassives = RoutePlayersPerPassivesSnapshot ?? new Dictionary<UnitDataModel, List<LorId>>();
            LogueBookModels.playersstatadders = RoutePlayersStatAddersSnapshot ?? new Dictionary<UnitDataModel, List<LogStatAdder>>();
            LogLikeMod.curstagetype = RouteStageTypeSnapshot;
            LogLikeMod.curstageid = RouteStageIdSnapshot;
            LogLikeMod.nextlist = RouteNextListSnapshot ?? new List<EmotionCardXmlInfo>();
            LogLikeMod.rewards = RouteRewardsSnapshot ?? new List<DropBookXmlInfo>();
            LogLikeMod.rewards_passive = RouteRewardsPassiveSnapshot ?? new List<RewardInfo>();
            LogLikeMod.rewards_InStage = RouteRewardsInStageSnapshot ?? new List<RewardInfo>();
            LogLikeMod.curchaptergrade = RouteChapterGradeSnapshot;
            LogLikeMod.curChStageStep = RouteChStageStepSnapshot;

            // --- Restore deep per-unit state ---
            if (LogueBookModels.playerModel != null && RouteUnitDeepStates != null)
            {
                for (int i = 0; i < LogueBookModels.playerModel.Count && i < RouteUnitDeepStates.Count; i++)
                {
                    var unit = LogueBookModels.playerModel[i];
                    var ds = RouteUnitDeepStates[i];
                    if (unit == null || ds == null)
                        continue;
                    // Restore equipped book
                    if (ds.EquippedBookId != LorId.None && LogueBookModels.booklist != null)
                    {
                        var book = LogueBookModels.booklist.Find(b => b != null && b.ClassInfo != null && b.ClassInfo.id == ds.EquippedBookId);
                        if (book != null)
                        {
                            LogueBookModels.EquipNewPage(unit, book.ClassInfo, false);
                        }
                    }
                    // Restore deck cards
                    if (ds.DeckCardIds != null && unit.bookItem != null)
                    {
                        unit.bookItem.GetCardListFromCurrentDeck()?.Clear();
                        foreach (var cardId in ds.DeckCardIds)
                        {
                            try { unit.AddCardFromInventory(cardId); } catch { }
                        }
                    }
                    // Restore custom name
                    if (!string.IsNullOrEmpty(ds.CustomName))
                    {
                        unit.SetCustomName(ds.CustomName);
                    }
                }
            }

            CompendiumOnlyLoadoutActive = false;
            Debug.Log("[RMRRealizationManager] Restored route loadout from snapshot.");
        }
        #endregion

    }
}
