// -----------------------------------------------------------------------------
// RogueLike Mod Reborn (RMR): RMR_Core
// Namespace/file: ruina-roguelike-reborn-main\RMR_Core.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using LOR_DiceSystem;
using UnityEngine.UI;
using TMPro;
using UI;
using UnityEngine;
using abcdcode_LOGLIKE_MOD;
using HarmonyLib;
using System.Reflection;
using UnityEngine.EventSystems;
using System.Linq;
using BattleCharacterProfile;
using GameSave;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Mod;
using System.Xml;
using System.Xml.Serialization;
using LOR_XML;
using System.Reflection.Emit;
using CustomMapUtility;
using Workshop;
using Sound;
using UnityEngine.SceneManagement;
using EnumExtenderV2;
using KeywordUtil;
using Unity.Mathematics;
using BattleCardEnhancedView;

namespace RogueLike_Mod_Reborn
{
    // =============================================================================
    // RMRCore - mod entry (ModInitializer): package id, invitation books, launch gate,
    // Grade6 special pages, Compendium role-book rules, satellite assets, keywords.
    // Continue Run is a Start Hub action (not invitation recipe -855).
    // See also: RMRStartHubPanel, LoguePlayDataSaver, RMRAbnormalityUnlockManager.
    // =============================================================================

    /// <summary>Main RMR bootstrap and cross-cutting helpers.</summary>
    public class RMRCore : ModInitializer
    {
        #region --- Invitation books & package identity ---

        public static LorId[] booksToAddToInventory 
        {
            get {
                // Do not put -855 (Continue) in the invitation book list: continue is a hub
                // menu action after RMR entry, not a free-standing invitation recipe icon.
                var books = new List<LorId>()
                {
                    new LorId(LogLikeMod.ModId, -2854),
                    new LorId(LogLikeMod.ModId, -3854),
                    new LorId(LogLikeMod.ModId, -4854),
                    new LorId(LogLikeMod.ModId, -5854),
                    new LorId(LogLikeMod.ModId, -6854)
                };
                books.AddRange(Singleton<RoguelikeGamemodeController>.Instance.gamemodeList.Select(x => x.StageStart));
                books.RemoveAll(x => x == null || x.id == -855);
                return books.ToArray();
            }
        }
        public static bool provideAdditionalLogging = false;
        /// <summary>
        /// A dictionary which links Assembly.FullName's to packageIds.<br></br>
        /// Should be filled with any assemblies passed up within Roguedlls.<br></br>
        /// Allows for assigning packageId to classes via RMRCorre.ClassIds[this.GetType().Assembly.FullName].
        /// </summary>
        public static Dictionary<string, string> ClassIds = new Dictionary<string, string>();
        public static RoguelikeGamemodeBase CurrentGamemode;
        public const string packageId = "abcdcodecalmmagma.LogueLikeReborn";
        public static CustomMapHandler RMRMapHandler;

        public const string BuildTimestamp = "2026-07-26Tperf-artwork-index-thumb-cache+08:00";

        #endregion

        #region --- Post-invitation launch gate (Play / Continue / Realization) ---

        /// <summary>
        /// After invitation is sent: run the intent chosen on the invitation-time hub.
        /// Never re-opens mode select mid-run.
        /// Realization: no initial mystery; floor pick on battle-prepare after -853 shell boots.
        /// </summary>
        /// <summary>Guards against PassiveAbility_ChStart firing OnWaveStart more than once per reception.</summary>
        private static bool _postInvitationLaunchConsumed;

        public static void ResetPostInvitationLaunchGate()
        {
            _postInvitationLaunchConsumed = false;
        }

        public static bool IsPostInvitationLaunchConsumed => _postInvitationLaunchConsumed;

        /// <summary>
        /// True only during realization bootstrap (hub choice through floor pick / prepare).
        /// Does NOT include live combat - use for skipping intro story / SparklingMirror, not for CheckStage.
        /// </summary>
        public static bool IsRealizationBootstrapPending()
        {
            return RMRRealizationManager.IsRealizationBootstrapPending();
        }

        /// <summary>Legacy alias of <see cref="IsRealizationBootstrapPending"/>.</summary>
        public static bool IsRealizationLaunchPending() => IsRealizationBootstrapPending();

        public static void HandlePostInvitationLaunch()
        {
            // Prefer durable intent on manager (hub Hide must not lose it).
            RMRLaunchIntent intent = RMRRealizationManager.PendingLaunchIntent;
            if (intent == RMRLaunchIntent.None)
                intent = RMRStartHubPanel.LaunchIntent;

            SephirahType? preselectedFloor = RMRRealizationManager.PendingRealizationFloor;
            Debug.Log($"[RMR] HandlePostInvitationLaunch intent={intent} consumed={_postInvitationLaunchConsumed} awaitFloor={RMRRealizationManager.AwaitingRealizationFloorPick} preFloor={(preselectedFloor.HasValue ? preselectedFloor.Value.ToString() : "none")} reception={RMRRealizationManager.RealizationReceptionActive}");

            // PassiveAbility_ChStart can fire OnWaveStart multiple times (wave restart / StartBattle loop).
            // Second call used to have intent=None and incorrectly StartMystery --NRE + broken start UI.
            if (_postInvitationLaunchConsumed
                || RMRRealizationManager.AwaitingRealizationFloorPick
                || RMRRealizationManager.PendingRealizationBattle
                || RMRRealizationManager.InRealizationBattle)
            {
                Debug.LogWarning("[RMR] HandlePostInvitationLaunch ignored (already launched / realization in progress).");
                if (RMRRealizationManager.AwaitingRealizationFloorPick)
                {
                    try { RMRRealizationLaunchHost.EnsureFloorPanelVisible(); } catch { }
                    LogLikeMod.PauseBool = true;
                }
                return;
            }

            // Consume once for this reception. Keep preselected floor until realization starts.
            _postInvitationLaunchConsumed = true;
            RMRRealizationManager.ClearLaunchIntentOnly();

            if (intent == RMRLaunchIntent.Realization)
            {
                RMRRealizationManager.BeginHubSession();
                // Never open initial relic mystery (-100).
                // Preferred path: floor already chosen on dedicated overlay before invite.

                // Early OpenBattlePrepare redirect may already have bound multi-wave prepare.
                if (RMRRealizationManager.PendingRealizationBattle
                    && !RMRRealizationManager.AwaitingRealizationFloorPick
                    && !RMRRealizationManager.IsOnRoguelikeShellStage())
                {
                    Debug.Log("[RMR] HandlePost: realization multi-wave prepare already bound (shell redirect); skip re-start.");
                    return;
                }

                if (preselectedFloor.HasValue)
                {
                    SephirahType floor = preselectedFloor.Value;
                    RMRRealizationManager.ClearPendingRealizationFloor();
                    RMRRealizationManager.AwaitingRealizationFloorPick = false;
                    LogLikeMod.PauseBool = false;
                    try
                    {
                        try { Singleton<MysteryManager>.Instance.EndMystery(); } catch { }
                        try { LogRealizationPanel.ForceCloseStatic(); } catch { }
                        RMRRealizationManager.StartRealizationBattle(floor);
                        Debug.Log($"[RMR] Realization launch direct --floor {floor} (dedicated UI pick, no mystery).");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[RMR] Direct realization start failed: " + ex);
                        RMRRealizationManager.RecoverRealizationToFloorPick("direct start exception: " + ex.Message);
                    }
                    return;
                }

                // Fallback: no preselect --open dedicated overlay floor UI (never vanilla prepare alone).
                RMRRealizationManager.EnterRealizationFloorPickMode();
                try
                {
                    RMRRealizationLaunchHost.EnsureFloorPanelVisible();
                    Debug.Log("[RMR] Realization launch --dedicated overlay floor panel (fallback, no mystery).");
                }
                catch (Exception ex)
                {
                    Debug.LogError("[RMR] Failed to schedule realization floor panel: " + ex);
                }
                return;
            }

            if (intent != RMRLaunchIntent.NormalPlay)
            {
                // No hub choice (e.g. unexpected path) --do not invent a broken mystery start.
                Debug.LogWarning($"[RMR] HandlePostInvitationLaunch: unexpected intent={intent}, no auto mystery.");
                return;
            }

            // Normal play only: initial relic mystery (mark normal only after invite actually started).
            RMRRealizationManager.ClearPendingRealizationFloor();
            RMRRealizationManager.StartNormalPlayFromHub();
            try
            {
                LogLikeMod.InvalidateTmpFontCache();
                var _ = LogLikeMod.DefFont_TMP;

                MysteryXmlList list = Singleton<MysteryXmlList>.Instance;
                if (list == null)
                {
                    Debug.LogError("[RMR] MysteryXmlList missing on launch.");
                    return;
                }
                MysteryXmlInfo info = list.GetData(new LorId(LogLikeMod.ModId, -100))
                    ?? list.GetData(new LorId(LogLikeMod.ModId, -1))
                    ?? list.GetData(new LorId(string.Empty, -100))
                    ?? list.GetData(new LorId(RMRCore.packageId, -100));
                if (info != null)
                    Singleton<MysteryManager>.Instance.StartMystery(info);
                else
                    Debug.LogError("[RMR] Initial mystery -100/-1 not found.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[RMR] Start initial mystery failed: " + ex);
            }
        }

        #endregion

        #region --- Mod init / satellite assets / config ---

        public override void OnInitializeMod()
        {
            base.OnInitializeMod();
            RMRCore.RMRMapHandler = CustomMapHandler.GetCMU(packageId);

            Debug.Log($"[RMR] RogueLike Mod Reborn initializing. Build: {BuildTimestamp}. DLL version check: this log confirms the NEW DLL is loaded.");

            MakeDirectoriesAndLoadConfig();
            Harmony.CreateAndPatchAll(typeof(RMR_Patches), packageId);
            try { RMRSessionHygiene.EnsureHost(); } catch (Exception hyEx) { Debug.LogWarning("[RMR] SessionHygiene host: " + hyEx.Message); }

            RegisterAllKeyword();

            foreach (Assembly ass in LogLikeMod.GetAssemList().Distinct())
            {
                var package = ModContentManager.Instance.GetAllMods().Find(x => ass.CodeBase.Contains(x.GetAssemPath()) || ass.Location.Contains(x.GetAssemPath()));
                if (package != null)
                {
                    ClassIds[ass.FullName] = package.invInfo.workshopInfo.uniqueId;
                }
            }
            ClassIds[this.GetType().Assembly.FullName] = RMRCore.packageId;

            LogLikeMod.ModdedArtWorks = new LogLikeMod.CacheDic<(string, string), Sprite>(new LogLikeMod.CacheDic<(string, string), Sprite>.getdele(LoadSatelliteArtwork));
            LogueEffectXmlList.Instance.Init(TextDataModel.CurrentLanguage);
            LoadSatelliteBattleTexts(TextDataModel.CurrentLanguage);
            LoadSatelliteBattleDialog(TextDataModel.CurrentLanguage);
            try { LoadVanillaCardArt(); }
            catch (Exception ex) { Debug.LogError($"[RMRCore] LoadVanillaCardArt failed: {ex.Message}. Continuing initialization without vanilla card artwork."); }
            RogueMysteryXmlList.Instance.Init(TextDataModel.CurrentLanguage);
            SceneManager.sceneLoaded += FindGamemodes;
            CurrentGamemode = new RoguelikeGamemode_RMR_Default();
            foreach (ModContentInfo logMod in LogLikeMod.GetLogMods())
                LogLikeMod.ModLoader.OnInitializeMod(logMod.GetAssemPath(), logMod.invInfo.workshopInfo.uniqueId);
        }

        // This essentially guarantees that the gamemodes are only collected far after all mods are loaded
        private void FindGamemodes(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Stage_Hod_New")
            {
                Singleton<RoguelikeGamemodeController>.Instance.AddGamemodesToList();
                SceneManager.sceneLoaded -= FindGamemodes;
            }
        }

        /// <summary>
        /// UIStoryArchivesPanel.tabcontroller is public in some builds but private/inaccessible from
        /// mod assemblies on current LoR (FieldAccessException). Always use reflection.
        /// </summary>
        public static UICustomTabsController GetStoryArchivesTabController(UIStoryArchivesPanel panel)
        {
            if (panel == null)
                return null;
            try
            {
                FieldInfo field = AccessTools.Field(typeof(UIStoryArchivesPanel), "tabcontroller");
                if (field == null)
                    return null;
                return field.GetValue(panel) as UICustomTabsController;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] GetStoryArchivesTabController: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Close stuck 指定司书剧情 / story archives overlays (often left open after Floor Realization).
        /// </summary>
        public static void ForceDismissStoryArchivesAndReturnMain()
        {
            try
            {
                try
                {
                    if (StoryScene.StoryRoot.Instance != null)
                        StoryScene.StoryRoot.Instance.EndStory(true);
                }
                catch { }

                UI.UIController ui = UI.UIController.Instance;
                if (ui == null)
                    return;

                UIStoryArchivesPanel story = ui.GetUIPanel(UIPanelType.Story) as UIStoryArchivesPanel;
                if (story != null)
                {
                    try { story.sephirahStoryPanel?.Deactivate(); } catch { }
                    try { story.battleStoryPanel?.Deactivate(); } catch { }
                    try { story.bookStoryPanel?.Deactivate(); } catch { }
                    try { story.creatureRebattlePanel?.Deactivate(); } catch { }
                    try
                    {
                        if (story.gameObject != null && story.gameObject.activeSelf)
                            story.gameObject.SetActive(false);
                    }
                    catch { }
                }

                try { Singleton<GlobalLogueItemCatalogPanel>.Instance?.Deactivate(); } catch { }
                try { ui.ClearActivatePanel(); } catch { }

                // Return to main library / invitation surface so 杂质+指定司书剧情 is not stuck on top.
                try { ui.CallUIPhase(UIPhase.Sephirah); } catch { }
                try { ui.CallUIPhase(UIPhase.Invitation); } catch { }

                Debug.Log("[RMR] ForceDismissStoryArchivesAndReturnMain done.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] ForceDismissStoryArchivesAndReturnMain: " + ex.Message);
            }
        }

        /// <summary>
        /// Safely retrieves the BattleDialogXmlList._dictionary via reflection.
        /// Returns null when the field is inaccessible (avoids FieldAccessException).
        /// </summary>
        private static Dictionary<string, BattleDialogRoot> GetBattleDialogDictionary()
        {
            try
            {
                var field = AccessTools.Field(typeof(BattleDialogXmlList), "_dictionary");
                if (field == null)
                {
                    Debug.LogError("[RMRCore] Field BattleDialogXmlList._dictionary not found via AccessTools");
                    return null;
                }
                return field.GetValue(BattleDialogXmlList.Instance) as Dictionary<string, BattleDialogRoot>;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RMRCore] Cannot access BattleDialogXmlList._dictionary: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Safely retrieves the BattleEffectTextsXmlList._dictionary via reflection.
        /// Returns null when the field is inaccessible (avoids FieldAccessException).
        /// </summary>
        private static Dictionary<string, BattleEffectText> GetBattleEffectTextDictionary()
        {
            try
            {
                var field = AccessTools.Field(typeof(BattleEffectTextsXmlList), "_dictionary");
                if (field == null)
                {
                    Debug.LogError("[RMRCore] Field BattleEffectTextsXmlList._dictionary not found via AccessTools");
                    return null;
                }
                return field.GetValue(BattleEffectTextsXmlList.Instance) as Dictionary<string, BattleEffectText>;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RMRCore] Cannot access BattleEffectTextsXmlList._dictionary: {e.Message}");
                return null;
            }
        }

        public static void LoadSatelliteBattleDialog(string language)
        {
            string str = Path.Combine("Localize", language);
            string ogpath = Path.Combine(ModContentManager.Instance.GetModPath(RMRCore.packageId), "Assemblies", "dlls");

            if (Directory.Exists(Path.Combine(ogpath, str, "BattleDialogs")))
            {
                var dialogDict = GetBattleDialogDictionary();
                if (dialogDict != null && dialogDict.TryGetValue("Workshop", out var workshopRoot))
                    workshopRoot.characterList.RemoveAll(x => x.id.packageId == RMRCore.packageId);
                foreach (System.IO.FileInfo fileinfo in LogLikeMod.EnumerateXmlFiles(Path.Combine(ogpath, str, "BattleDialogs")))
                {
                    try
                    {
                        using (var stream = fileinfo.OpenRead())
                        {
                            var root = new XmlSerializer(typeof(BattleDialogRoot)).Deserialize(stream) as BattleDialogRoot;
                            BattleDialogXmlList.Instance.AddDialogByMod(root.characterList);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Error while trying to load XML file " + fileinfo.Name + ": " + e);
                    }
                }

            }

            foreach (ModContentInfo modContentInfo in LogLikeMod.GetLogMods())
            {
                string uniqueId = modContentInfo.invInfo.workshopInfo.uniqueId;
                if (Directory.Exists(Path.Combine(modContentInfo.GetLogDllPath(), str, "BattleDialogs")))
                {
                    var dialogDict = GetBattleDialogDictionary();
                    if (dialogDict != null && dialogDict.TryGetValue("Workshop", out var workshopRoot))
                        workshopRoot.characterList.RemoveAll(x => x.id.packageId == uniqueId);
                    foreach (System.IO.FileInfo fileinfo2 in LogLikeMod.EnumerateXmlFiles(Path.Combine(modContentInfo.GetLogDllPath(), str, "BattleDialogs")))
                    {
                        try
                        {
                            using (var stream = fileinfo2.OpenRead())
                            {
                                var root = new XmlSerializer(typeof(BattleDialogRoot)).Deserialize(stream) as BattleDialogRoot;
                                BattleDialogXmlList.Instance.AddDialogByMod(root.characterList);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Log("Error while trying to load XML file " + fileinfo2.Name + ": " + e);
                        }
                    }
                }

            }

        }

        public static void LoadSatelliteBattleTexts(string language)
        {
            var dict = GetBattleEffectTextDictionary();
            if (dict == null)
            {
                Debug.LogError("[RMRCore] LoadSatelliteBattleTexts aborted --could not access BattleEffectTextsXmlList._dictionary");
                return;
            }

            string str = Path.Combine("Localize", language);
            string ogpath = Path.Combine(ModContentManager.Instance.GetModPath(RMRCore.packageId), "Assemblies", "dlls");

            if (Directory.Exists(Path.Combine(ogpath, str, "EffectTexts")))
            {
                foreach (System.IO.FileInfo fileinfo in LogLikeMod.EnumerateXmlFiles(Path.Combine(ogpath, str, "EffectTexts")))
                {
                    try
                    {
                        using (var stream = fileinfo.OpenRead())
                        {
                            var root = new XmlSerializer(typeof(BattleEffectTextRoot)).Deserialize(stream) as BattleEffectTextRoot;
                            foreach (var info in root.effectTextList)
                            {
                                if (!dict.ContainsKey(info.ID))
                                    dict.Add(info.ID, info);
                                else
                                    dict[info.ID] = info;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Error while trying to load XML file " + fileinfo.Name + ": " + e);
                    }
                }

            }

            foreach (ModContentInfo modContentInfo in LogLikeMod.GetLogMods())
            {
                string uniqueId = modContentInfo.invInfo.workshopInfo.uniqueId;
                if (Directory.Exists(Path.Combine(modContentInfo.GetLogDllPath(), str, "EffectTexts")))
                {
                    foreach (System.IO.FileInfo fileinfo2 in LogLikeMod.EnumerateXmlFiles(Path.Combine(modContentInfo.GetLogDllPath(), str, "EffectTexts")))
                    {
                        try
                        {
                            using (var stream = fileinfo2.OpenRead())
                            {
                                var root = new XmlSerializer(typeof(BattleEffectTextRoot)).Deserialize(stream) as BattleEffectTextRoot;
                                foreach (var info in root.effectTextList)
                                {
                                    if (!dict.ContainsKey(info.ID))
                                        dict.Add(info.ID, info);
                                    else
                                        dict[info.ID] = info;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Log("Error while trying to load XML file " + fileinfo2.Name + ": " + e);
                        }
                    }
                }

            }

        }

        public static void MakeDirectoriesAndLoadConfig()
        {
            if (!Directory.Exists(LogueSaveManager.Saveroot))
                Directory.CreateDirectory(LogueSaveManager.Saveroot);
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "ModConfigs")))
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "ModConfigs"));

            // CLEAR OUT LEGACY CONFIGS
            if (File.Exists(LogueSaveManager.Saveroot + "/RMR_Config.xml"))
            {
                RMRConfigRoot config;
                using (var file = File.OpenRead(LogueSaveManager.Saveroot + "/RMR_Config.xml"))
                {
                    config = (RMRConfigRoot)(new XmlSerializer(typeof(RMRConfigRoot)).Deserialize(file));
                    RMRCore.provideAdditionalLogging = config.EnableAdditionalLogging;
                    GlobalLogueItemCatalogPanel.Instance.debugMode = config.ShowAllItemCatalog;
                }

                using (var file = File.OpenWrite(Path.Combine(Application.persistentDataPath, "ModConfigs", "RMR_Config.xml")))
                {
                    new XmlSerializer(typeof(RMRConfigRoot)).Serialize(file, config);
                }

                File.Delete(LogueSaveManager.Saveroot + "/RMR_Config.xml");
            }
            else
            { 
                if (File.Exists(Path.Combine(Application.persistentDataPath, "ModConfigs", "RMR_Config.xml")))
                {
                    using (var file = File.OpenRead(Path.Combine(Application.persistentDataPath, "ModConfigs", "RMR_Config.xml")))
                    {
                        RMRConfigRoot config = (RMRConfigRoot)(new XmlSerializer(typeof(RMRConfigRoot)).Deserialize(file));
                        RMRCore.provideAdditionalLogging = config.EnableAdditionalLogging;
                        GlobalLogueItemCatalogPanel.Instance.debugMode = config.ShowAllItemCatalog;
                    }
                } else
                {
                    using (var file = File.Create(Path.Combine(Application.persistentDataPath, "ModConfigs", "RMR_Config.xml")))
                    {
                        new XmlSerializer(typeof(RMRConfigRoot)).Serialize(file, new RMRConfigRoot());
                    }
                }
            }

            // CREATE ITEM CATALOG IF IT DOES NOT EXIST
            if (!File.Exists(LogueSaveManager.Saveroot + "/RMR_ItemCatalog"))
            {
                SaveData data = new SaveData(SaveDataType.Dictionary);
                using (FileStream fileStream = File.Create(LogueSaveManager.Saveroot + "/RMR_ItemCatalog"))
                {
                    new BinaryFormatter().Serialize(fileStream, data.GetSerializedData());
                }
            }
        }

        /// <summary>
        /// Safely retrieves the AssetBundleManagerRemake._cardAssetBundle via reflection.
        /// Calls LoadCardAssetBundle() first to ensure the bundle is loaded.
        /// Returns null when the field is inaccessible (avoids FieldAccessException).
        /// </summary>
        private static AssetBundle GetCardAssetBundle()
        {
            try
            {
                Singleton<AssetBundleManagerRemake>.Instance.LoadCardAssetBundle();
                var field = AccessTools.Field(typeof(AssetBundleManagerRemake), "_cardAssetBundle");
                if (field == null)
                {
                    Debug.LogError("[RMRCore] Field AssetBundleManagerRemake._cardAssetBundle not found via AccessTools");
                    return null;
                }
                var bundle = field.GetValue(Singleton<AssetBundleManagerRemake>.Instance) as AssetBundle;
                if (bundle == null)
                {
                    Debug.LogError("[RMRCore] AssetBundleManagerRemake._cardAssetBundle is null after LoadCardAssetBundle()");
                    return null;
                }
                return bundle;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RMRCore] Cannot access AssetBundleManagerRemake._cardAssetBundle: {e.Message}");
                return null;
            }
        }

        private static FieldInfo _cachedArtworkSpriteField;
        private static FieldInfo ArtworkSpriteField
        {
            get
            {
                if (_cachedArtworkSpriteField == null)
                {
                    _cachedArtworkSpriteField = typeof(ArtworkCustomizeData).GetField("_sprite", AccessTools.all);
                    if (_cachedArtworkSpriteField == null)
                        Debug.LogError("[RMRCore] Cannot find field ArtworkCustomizeData._sprite --vanilla card artwork will not be loaded.");
                }
                return _cachedArtworkSpriteField;
            }
        }

        public static void LoadVanillaCardArt()
        {
            FieldInfo spriteField = ArtworkSpriteField;
            if (spriteField == null)
                return;

            List<ArtworkCustomizeData> list = Singleton<CustomizingCardArtworkLoader>.Instance.GetWorkshopArtworkData("LoR.StartUp.LocalizationManager");
            if (list == null) // check for Localization Manager vanilla page loading
            {
                AssetBundle cardBundle = GetCardAssetBundle();
                if (cardBundle == null)
                    return;
                Sprite[] array = cardBundle.LoadAllAssets<Sprite>();
                list = Singleton<CustomizingCardArtworkLoader>.Instance.GetWorkshopArtworkData(RMRCore.packageId) ?? new List<ArtworkCustomizeData>();
                foreach (Sprite sprite in array)
                {
                    try
                    {
                        var data = new ArtworkCustomizeData { name = sprite.name };
                        spriteField.SetValue(data, sprite);
                        list.Add(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RMRCore] Failed to set _sprite for {sprite?.name ?? "?"}: {ex.Message}. Skipping this sprite.");
                    }
                }
                Singleton<CustomizingCardArtworkLoader>.Instance.AddArtworkData(RMRCore.packageId, list);
            }
            else
            {
                var listLog = Singleton<CustomizingCardArtworkLoader>.Instance.GetWorkshopArtworkData(RMRCore.packageId);
                if (listLog == null)
                {
                    listLog = new List<ArtworkCustomizeData>();
                    Singleton<CustomizingCardArtworkLoader>.Instance.AddArtworkData(RMRCore.packageId, listLog);
                }
                listLog.AddRange(list);
            }
            foreach (ModContentInfo modContentInfo in LogLikeMod.GetLogMods())
            {
                string uniqueId = modContentInfo.invInfo.workshopInfo.uniqueId;
                List<ArtworkCustomizeData> list2 = Singleton<CustomizingCardArtworkLoader>.Instance.GetWorkshopArtworkData(uniqueId);
                if (list2 == null)
                {
                    list2 = new List<ArtworkCustomizeData>();
                    Singleton<CustomizingCardArtworkLoader>.Instance.AddArtworkData(uniqueId, list2);
                }
                list2.AddRange(list);
            }
        }

        public static Sprite LoadSatelliteArtwork((string, string) ids)
        {
            try
            {
                if (ids.Item1 == RMRCore.packageId)
                {
                    if (!Environment.StackTrace.Contains("abcdcode_LOGLIKE_MOD.LogLikeMod+CacheDic`2[Tkey,TValue].ContainsKey (Tkey key)")) 
                        "".Log("Unnecessary custom artwork call for vanilla artwork! Coming from " + Environment.StackTrace);
                    if (LogLikeMod.ArtWorks.ContainsKey(ids.Item2))
                        return LogLikeMod.ArtWorks[ids.Item2];
                    else "".Log("Failed to obtain sprite altogether!! REAL SHIT!!!");
                }
                else
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(ModContentManager.Instance.GetModPath(ids.Item1) + "/Resource/CustomArtwork");
                    if (directoryInfo.GetDirectories().Length != 0)
                    {
                        foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                        {
                            Sprite artWorks = LogLikeMod.GetArtWorks(dir, ids.Item2);
                            if (artWorks != null)
                            {
                                return artWorks;
                            }
                        }
                    }
                    foreach (System.IO.FileInfo fileInfo in directoryInfo.GetFiles())
                    {
                        if (Path.GetFileNameWithoutExtension(fileInfo.FullName) == ids.Item2)
                        {
                            Texture2D texture2D = new Texture2D(2, 2);
                            texture2D.LoadImage(File.ReadAllBytes(fileInfo.FullName));
                            return Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0f, 0f), 100f, 0U, SpriteMeshType.FullRect);
                        }
                    }
                }
            } catch (Exception e)
            {
                "".Log("Failed to obtain custom artwork: " + e);
            }
            return null;
        }

        public static bool AddNewKeywordBufToList(string keyword, ref KeywordBuf buf)
        {
            if (EnumExtender.TryGetValueOf<KeywordBuf>(keyword, out var newKeyword) || (EnumExtender.TryFindUnnamedValue(default(KeywordBuf), null, false, out newKeyword) && EnumExtender.TryAddName(keyword, newKeyword)))
            {
                buf = newKeyword;
                return true;
            }
            return false;
        }

        public static void RegisterAllKeyword()
        {
            if (AddNewKeywordBufToList("CalmMagma_RMR_CritChance", ref RoguelikeBufs.CritChance))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_CritChance>();
            if (AddNewKeywordBufToList("Lux_RMR_Shield", ref RoguelikeBufs.RMRShield))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_Shield>();
            if (AddNewKeywordBufToList("Lux_RMR_StaggerShield", ref RoguelikeBufs.RMRStaggerShield))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_StaggerShield>();
            if (AddNewKeywordBufToList("Lux_RMR_BurnProtection", ref RoguelikeBufs.BurnProtection))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_BurnProtection>();
            if (AddNewKeywordBufToList("Lux_RMR_BleedProtection", ref RoguelikeBufs.BleedProtection))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_BleedProtection>();
            if (AddNewKeywordBufToList("Lux_RMR_ClashPower", ref RoguelikeBufs.ClashPower))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_ClashPower>();
            if (AddNewKeywordBufToList("Lux_RMR_SlashClashPower", ref RoguelikeBufs.SlashClashPower))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_SlashClashPower>();
            if (AddNewKeywordBufToList("Lux_RMR_PierceClashPower", ref RoguelikeBufs.PierceClashPower))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_PierceClashPower>();
            if (AddNewKeywordBufToList("Lux_RMR_BluntClashPower", ref RoguelikeBufs.BluntClashPower))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_BluntClashPower>();
            if (AddNewKeywordBufToList("Lux_RMR_ReworkPersistence", ref RoguelikeBufs.RMRPersistence))
                KeywordUtils.RegisterKeywordBuf<SweeperBuf>();
            if (AddNewKeywordBufToList("Lux_RMR_ReworkSmoke", ref RoguelikeBufs.RMRSmoke))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_Smoke>();
            if (AddNewKeywordBufToList("Lux_RMR_LuckBuf", ref RoguelikeBufs.RMRLuck))
                KeywordUtils.RegisterKeywordBuf<BattleUnitBuf_RMR_Luck>();
        }

        #endregion

        #region literally do not use this ever
        /*
        public static string GetPackageIdFromAssembly(Assembly ass)
        {
            if (ass == typeof(RMRCore).Assembly || ass == typeof(LogLikeMod).Assembly)
                return RMRCore.packageId;
            string ass2 = ass.Location;
            var mod = LogLikeMod.GetLogMods().Find(x => ass2.Contains(x.GetLogDllPath()));
            if (mod != null)
                return mod.invInfo.workshopInfo.uniqueId;
            return null;
        }
        */
        #endregion

        #region --- Grade6 special core pages / Red Mist / archive reset ---

        private const string Grade6SpecialCorePagesGrantedSaveName = "RMR_Grade6SpecialCorePagesGranted";
        private const string BlackSilenceStageClearedSaveName = "RMR_BlackSilenceStageCleared";
        private const string DistortedEnsembleStageClearedSaveName = "RMR_DistortedEnsembleStageCleared";
        private const int RedMistChallengeStageId = 60020;
        /// <summary>
        /// 250013 is the shared localization TextId of Argalia's page -- NOT a book id.
        /// No vanilla book carries id 250013, so `new LorId(250013)` never resolved and the whole
        /// Urban Star unlock aborted with "cannot resolve Blue Reverberation core page LorId(:250013)".
        /// Vanilla ships three books that all use TextId 250013:
        ///   260005   EquipPage_ch7.txt             Chapter 7, Episode 610000  &lt;- player-obtainable
        ///   150013   EquipPage_enemy_ch6.txt       ch6 enemy Blue Reverberation
        ///   1310011  EquipPage_enemy_ch7_Elena.txt Distorted Ensemble enemy
        /// Resolve by TextId, same shape as TryResolveBlackSilenceCorePage.
        /// </summary>
        private const int BlueReverberationCoreTextId = 250013;

        /// <summary>Player-obtainable Argalia page (EquipPage_ch7). Preferred resolution target.</summary>
        private const int BlueReverberationPlayerCorePageId = 260005;

        /// <summary>Old RMR-package page id; still recognised so existing saves migrate.</summary>
        private const int BlueReverberationCorePageId = 250013;

        private static LorId _resolvedBlueReverberationCorePageId;

        private static readonly int[] BlueReverberationBattlePageIds =
        {
            704001,   // Trails of Blue
            704011,   // Allegro
            704012,   // Largo
            704013,   // Resonant Scythe
            704014,   // Tempestuous Danza
            705010,   // Resonance Control (no player-facing localization in vanilla)
            705011    // Argalia Danza, wide (no player-facing localization in vanilla)
        };

        public static LorId GetBlueReverberationCorePageLorId()
        {
            if (_resolvedBlueReverberationCorePageId != null)
                return _resolvedBlueReverberationCorePageId;
            try
            {
                List<BookXmlInfo> candidates = GetVanillaCorePageCandidates(out string _)
                    .Where(x => x != null && x.TextId == BlueReverberationCoreTextId)
                    .ToList();
                // Prefer the ch7 player page; any TextId match still beats an id that cannot resolve.
                BookXmlInfo pick = candidates.Find(x => x.id != null && x.id.id == BlueReverberationPlayerCorePageId)
                    ?? candidates.FirstOrDefault();
                if (pick != null && pick.id != null)
                {
                    _resolvedBlueReverberationCorePageId = pick.id;
                    Debug.Log($"[RMR] Blue Reverberation core page resolved to {pick.id} (TextId {BlueReverberationCoreTextId}, {candidates.Count} candidates).");
                    return _resolvedBlueReverberationCorePageId;
                }
                Debug.LogWarning($"[RMR] No vanilla book with TextId {BlueReverberationCoreTextId}; falling back to {BlueReverberationPlayerCorePageId}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] GetBlueReverberationCorePageLorId: " + ex.Message);
            }
            // Do not cache the fallback: BookXmlList may simply not be loaded yet.
            return new LorId(BlueReverberationPlayerCorePageId);
        }

        public static bool IsLegacyBlueReverberationCorePageId(LorId id)
        {
            if (id == null || id.id != BlueReverberationCorePageId)
                return false;
            // Both the old RMR-package page and the bare vanilla 250013 are legacy: 250013 is a
            // TextId, so a vanilla LorId(250013) resolves to no book at all and must be migrated
            // to the real Argalia page id the same way.
            return id.packageId == LogLikeMod.ModId || string.IsNullOrEmpty(id.packageId);
        }

        public static LorId NormalizeLegacyBlueReverberationCorePageId(LorId id)
        {
            return IsLegacyBlueReverberationCorePageId(id) ? GetBlueReverberationCorePageLorId() : id;
        }

        public static bool PruneLegacyBlueReverberationCorePageUnlocks()
        {
            bool changed = false;
            LorId legacyId = new LorId(LogLikeMod.ModId, BlueReverberationCorePageId);
            if (LogueBookModels.CompendiumUnlockedRoleBooks != null)
            {
                if (LogueBookModels.CompendiumUnlockedRoleBooks.Remove(legacyId))
                {
                    LogueBookModels.CompendiumUnlockedRoleBooks.Add(GetBlueReverberationCorePageLorId());
                    changed = true;
                }
            }
            if (LogueBookModels.booklist != null)
                changed |= LogueBookModels.booklist.RemoveAll(book => book?.ClassInfo?.id == legacyId) > 0;
            return changed;
        }

        private static List<BookXmlInfo> GetVanillaCorePageCandidates(out string reason)
        {
            reason = null;

            var allBooks = Singleton<BookXmlList>.Instance.GetList();
            if (allBooks == null || allBooks.Count == 0)
            {
                reason = "BookXmlList.GetList() returned empty --BookXmlList may not be loaded yet.";
                return new List<BookXmlInfo>();
            }

            return allBooks.Where(x => x != null && !x.id.IsWorkshop()).ToList();
        }

        private static bool TryResolveBlackSilenceCorePage(out BookXmlInfo blackSilence, out string reason)
        {
            blackSilence = null;
            var candidates = GetVanillaCorePageCandidates(out reason);
            if (candidates.Count == 0)
                return false;

            // Two vanilla books carry TextId 102: 102 (EquipPage_basic, the player-obtainable
            // "The Black Silence's Page") and 180003 (EquipPage_enemy_ch7, the enemy variant).
            // Pin the player one instead of taking whichever the list enumerates first.
            blackSilence = candidates.Find(x => x.id != null && x.id.id == 102 && x.TextId == 102);
            if (blackSilence == null)
                blackSilence = candidates.Find(x => x.TextId == 102);
            if (blackSilence == null)
                blackSilence = candidates.Find(x => !string.IsNullOrEmpty(x.InnerName)
                    && x.InnerName.IndexOf("BlackSilence", StringComparison.OrdinalIgnoreCase) >= 0);
            if (blackSilence == null)
                blackSilence = candidates.Find(x => !string.IsNullOrEmpty(x._bookIcon)
                    && x._bookIcon.IndexOf("BlackSilence", StringComparison.OrdinalIgnoreCase) >= 0);
            if (blackSilence == null)
                blackSilence = candidates.Find(x => x.CharacterSkin != null
                    && x.CharacterSkin.Any(s => !string.IsNullOrEmpty(s)
                        && s.IndexOf("Black", StringComparison.OrdinalIgnoreCase) >= 0));

            if (blackSilence != null)
            {
                reason = null;
                return true;
            }

            int textIdMatches = candidates.Count(x => x.TextId == 102);
            reason = $"Black Silence core page not found. candidates={candidates.Count}, TextId102={textIdMatches}.";
            return false;
        }

        private static bool TryResolveBinahCorePage(out BookXmlInfo binah, out string reason)
        {
            binah = null;
            var candidates = GetVanillaCorePageCandidates(out reason);
            if (candidates.Count == 0)
                return false;

            binah = candidates.Find(x => x.CharacterSkin != null
                && x.CharacterSkin.Any(s => !string.IsNullOrEmpty(s)
                    && s.IndexOf("Binah", StringComparison.OrdinalIgnoreCase) >= 0));
            if (binah == null)
                binah = candidates.Find(x => !string.IsNullOrEmpty(x._bookIcon)
                    && x._bookIcon.IndexOf("Binah", StringComparison.OrdinalIgnoreCase) >= 0);
            if (binah == null)
                binah = candidates.Find(x => !string.IsNullOrEmpty(x.InnerName)
                    && x.InnerName.IndexOf("Binah", StringComparison.OrdinalIgnoreCase) >= 0);

            if (binah != null)
            {
                reason = null;
                return true;
            }

            int skinMatches = candidates.Count(x => x.CharacterSkin != null
                && x.CharacterSkin.Any(s => !string.IsNullOrEmpty(s)
                    && s.IndexOf("Binah", StringComparison.OrdinalIgnoreCase) >= 0));
            reason = $"Binah core page not found. candidates={candidates.Count}, skinMatches={skinMatches}.";
            return false;
        }

        /// <summary>
        /// Tries to locate Black Silence (Roland) and Binah core pages in BookXmlList.
        /// Keep this legacy combined resolver for older checks, but runtime grant paths
        /// resolve each page independently so one missing page does not block the other.
        /// </summary>
        private static bool TryResolveGrade6SpecialCorePages(out BookXmlInfo blackSilence, out BookXmlInfo binah, out string reason)
        {
            bool foundBlackSilence = TryResolveBlackSilenceCorePage(out blackSilence, out string blackSilenceReason);
            bool foundBinah = TryResolveBinahCorePage(out binah, out string binahReason);
            reason = foundBlackSilence && foundBinah
                ? null
                : $"BlackSilence={(foundBlackSilence ? blackSilence.id.ToString() : blackSilenceReason)}. Binah={(foundBinah ? binah.id.ToString() : binahReason)}.";
            return foundBlackSilence && foundBinah;
        }

        private static bool HasRoleBookInBothCompendiumAndBooklist(BookXmlInfo page)
        {
            if (page == null)
                return false;

            LogueBookModels.EnsureCompendiumUnlocks();
            bool inAtlas = LogueBookModels.CompendiumUnlockedRoleBooks != null
                && LogueBookModels.CompendiumUnlockedRoleBooks.Contains(page.id);
            bool inBooklist = LogueBookModels.booklist != null
                && LogueBookModels.booklist.Any(book => book?.ClassInfo?.id == page.id);
            return inAtlas && inBooklist;
        }

        /// <summary>
        /// Ensures a role book is in the current booklist (route inventory), even if it is already in the permanent atlas.
        /// This is needed because TryAddUniqueRoleBookToInventoryAndCompendium skips adding to booklist when the id is
        /// already in CompendiumUnlockedRoleBooks --but for Grade6 special core pages, we need them usable immediately.
        /// </summary>
        private static bool EnsureRoleBookInCurrentBooklist(LorId id)
        {
            if (id == null || id == LorId.None)
                return false;
            if (LogueBookModels.booklist == null)
                LogueBookModels.booklist = new List<BookModel>();
            if (LogueBookModels.booklist.Any(b => b?.ClassInfo?.id == id))
                return true; // already in booklist
            BookXmlInfo bookXml = RewardingModel.GetBookDataOriginAware(id);
            if (bookXml == null)
                return false;
            BookModel bookModel = new BookModel(bookXml);
            bookModel.instanceId = LogueBookModels.nextinstanceid++;
            bookModel.TryGainUniquePassive();
            LogueBookModels.booklist.Add(bookModel);
            Debug.Log($"[RMR] EnsureRoleBookInCurrentBooklist: added {id.packageId}:{id.id} ({bookXml.InnerName ?? "?"}) to current booklist.");
            return true;
        }

        /// <summary>
        /// Grants Black Silence at Urban Star entry after the Black Silence stage has been cleared.
        /// Grants Blue Reverberation after the Distorted Ensemble stage has been cleared.
        /// Binah is intentionally excluded:
        /// she is available temporarily during the Red Mist challenge and remains
        /// unlocked only for the current route after that challenge is won.
        /// </summary>
        public static void EnsureGrade6SpecialCorePagesUnlocked()
        {
            RMRAbnormalityUnlockManager.PrunePrematureRedMistChallengeRewards();

            bool grantedAny = false;

            if (IsBlackSilenceUnlockedForUrbanStar())
                grantedAny |= EnsureBlackSilenceCorePageForUrbanStar();
            else
                Debug.Log("[RMR] Urban Star entry: Black Silence is still locked until the Black Silence stage is cleared once.");

            if (IsDistortedEnsembleUnlockedForUrbanStar())
                grantedAny |= EnsureBlueReverberationRewardsForUrbanStar();
            else
                Debug.Log("[RMR] Urban Star entry: Blue Reverberation is still locked until the Distorted Ensemble stage is cleared once.");

            if (grantedAny)
            {
                SaveGrantedFlagInternal();
                LogueBookModels.SavePermanentCompendiumData();
            }
        }

        private static bool EnsureBlackSilenceCorePageForUrbanStar()
        {
            if (!TryResolveBlackSilenceCorePage(out BookXmlInfo blackSilence, out string resolveReason))
            {
                Debug.LogError($"[RMR] EnsureBlackSilenceCorePageForUrbanStar: cannot resolve Black Silence --{resolveReason}");
                return false;
            }
            bool bsInAtlasNow = LogueBookModels.TryAddUniqueRoleBookToInventoryAndCompendium(blackSilence.id);
            if (bsInAtlasNow)
                Debug.Log($"[RMR] Grade6 special: granted Black Silence core page {blackSilence.id.packageId}:{blackSilence.id.id} (TextId={blackSilence.TextId}, InnerName={blackSilence.InnerName ?? "?"}).");
            bool bsInBooklist = EnsureRoleBookInCurrentBooklist(blackSilence.id);

            if (HasRoleBookInBothCompendiumAndBooklist(blackSilence))
            {
                Debug.Log("[RMR] Urban Star entry: Black Silence confirmed in atlas and current route. Binah remains gated by the Red Mist challenge.");
                return bsInAtlasNow || bsInBooklist;
            }

            Debug.LogError($"[RMR] EnsureBlackSilenceCorePageForUrbanStar: failed to confirm Black Silence. atlasAdded={bsInAtlasNow}, booklist={bsInBooklist}.");
            return false;
        }

        private static bool EnsureBlueReverberationRewardsForUrbanStar()
        {
            LorId blueBookId = GetBlueReverberationCorePageLorId();
            BookXmlInfo blueBook = RewardingModel.GetBookDataOriginAware(blueBookId);
            if (blueBook == null)
            {
                Debug.LogError($"[RMR] EnsureBlueReverberationRewardsForUrbanStar: cannot resolve Blue Reverberation core page {blueBookId}.");
                return false;
            }

            bool changed = PruneLegacyBlueReverberationCorePageUnlocks();
            changed |= LogueBookModels.TryAddUniqueRoleBookToInventoryAndCompendium(blueBookId);
            bool inBooklist = EnsureRoleBookInCurrentBooklist(blueBookId);
            if (LogueBookModels.cardlist == null)
                LogueBookModels.cardlist = new List<DiceCardItemModel>();
            foreach (int pageId in BlueReverberationBattlePageIds)
            {
                LorId cardId = new LorId(pageId);
                bool alreadyUnlocked = LogueBookModels.CompendiumUnlockedBattleCards != null
                    && LogueBookModels.CompendiumUnlockedBattleCards.Contains(cardId);
                LogueBookModels.AddCard(cardId, 1, false);
                changed |= !alreadyUnlocked;
            }

            LogueBookModels.EnsureCompendiumUnlocks();
            bool confirmedCore = LogueBookModels.CompendiumUnlockedRoleBooks.Contains(blueBookId)
                && LogueBookModels.booklist != null
                && LogueBookModels.booklist.Any(book => book?.ClassInfo?.id == blueBookId);
            bool confirmedCards = BlueReverberationBattlePageIds.All(pageId =>
                LogueBookModels.CompendiumUnlockedBattleCards.Contains(new LorId(pageId)));
            if (confirmedCore && confirmedCards)
            {
                Debug.Log($"[RMR] Urban Star entry: Blue Reverberation confirmed in atlas/current route; battle pages {string.Join(", ", BlueReverberationBattlePageIds.Select(x => x.ToString()).ToArray())} unlocked.");
                return changed || inBooklist;
            }

            Debug.LogError($"[RMR] EnsureBlueReverberationRewardsForUrbanStar: failed to confirm rewards. core={confirmedCore}, cards={confirmedCards}, booklist={inBooklist}.");
            return false;
        }

        public static bool IsBinahRedMistChallengeUnlocked()
        {
            return RMRAbnormalityUnlockManager.IsBinahUnlockedForCurrentRoute();
        }

        public static bool IsBinahCorePage(BookXmlInfo page)
        {
            return page != null
                && ((!string.IsNullOrEmpty(page.InnerName)
                        && page.InnerName.IndexOf("Binah", StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(page._bookIcon)
                        && page._bookIcon.IndexOf("Binah", StringComparison.OrdinalIgnoreCase) >= 0)
                    || (page.CharacterSkin != null
                        && page.CharacterSkin.Any(skin => !string.IsNullOrEmpty(skin)
                            && skin.IndexOf("Binah", StringComparison.OrdinalIgnoreCase) >= 0)));
        }

        public static bool IsBlueReverberationCorePage(BookXmlInfo page)
        {
            return page != null
                && (page.id?.id == BlueReverberationCorePageId
                    || page.TextId == BlueReverberationCorePageId
                    || (!string.IsNullOrEmpty(page.InnerName)
                        && (page.InnerName.IndexOf("Argalia", StringComparison.OrdinalIgnoreCase) >= 0
                            || page.InnerName.IndexOf("BlueReverberation", StringComparison.OrdinalIgnoreCase) >= 0
                            || page.InnerName.IndexOf("Blue Reverberation", StringComparison.OrdinalIgnoreCase) >= 0))
                    || (!string.IsNullOrEmpty(page._bookIcon)
                        && page._bookIcon.IndexOf("BlueReverberation", StringComparison.OrdinalIgnoreCase) >= 0)
                    || (page.CharacterSkin != null
                        && page.CharacterSkin.Any(skin => !string.IsNullOrEmpty(skin)
                            && skin.IndexOf("Argalia", StringComparison.OrdinalIgnoreCase) >= 0)));
        }

        public static bool ShouldRecordRoleBookInPermanentCompendium(BookXmlInfo page)
        {
            return !IsBinahCorePage(page);
        }

        public static void PrepareBinahForRedMistChallenge()
        {
            if (LogLikeMod.curstageid != new LorId(LogLikeMod.ModId, RedMistChallengeStageId))
                return;
            if (!TryResolveBinahCorePage(out BookXmlInfo binah, out string reason))
            {
                Debug.LogError($"[RMR] PrepareBinahForRedMistChallenge: cannot resolve Binah --{reason}");
                return;
            }

            EnsureRoleBookInCurrentBooklist(binah.id);
            Debug.Log(IsBinahRedMistChallengeUnlocked()
                ? $"[RMR] Red Mist challenge: route-unlocked Binah page {binah.id} is available."
                : $"[RMR] Red Mist challenge: Binah page {binah.id} added to the current route temporarily.");
        }

        public static void UnlockBinahAfterRedMistVictory()
        {
            if (!TryResolveBinahCorePage(out BookXmlInfo binah, out string reason))
            {
                Debug.LogError($"[RMR] UnlockBinahAfterRedMistVictory: cannot resolve Binah --{reason}");
                return;
            }

            EnsureRoleBookInCurrentBooklist(binah.id);
            RMRAbnormalityUnlockManager.UnlockBinahForCurrentRoute();
            if (LogueBookModels.CompendiumUnlockedRoleBooks != null && LogueBookModels.CompendiumUnlockedRoleBooks.Remove(binah.id))
                LogueBookModels.SavePermanentCompendiumUnlocks();
            Debug.Log($"[RMR] Red Mist challenge victory: Binah core page {binah.id} unlocked for the current route.");
        }

        public static void ApplyBinahRedMistProgressionState()
        {
            RMRAbnormalityUnlockManager.PrunePrematureRedMistChallengeRewards();

            if (!TryResolveBinahCorePage(out BookXmlInfo binah, out string reason))
            {
                Debug.LogError($"[RMR] ApplyBinahRedMistProgressionState: cannot resolve Binah --{reason}");
                return;
            }

            if (IsBinahRedMistChallengeUnlocked())
            {
                EnsureRoleBookInCurrentBooklist(binah.id);
                if (LogueBookModels.CompendiumUnlockedRoleBooks != null && LogueBookModels.CompendiumUnlockedRoleBooks.Remove(binah.id))
                    LogueBookModels.SavePermanentCompendiumUnlocks();
                return;
            }

            bool atlasChanged = LogueBookModels.CompendiumUnlockedRoleBooks != null
                && LogueBookModels.CompendiumUnlockedRoleBooks.Remove(binah.id);
            if (atlasChanged)
                LogueBookModels.SavePermanentCompendiumUnlocks();

            if (LogLikeMod.curstageid == new LorId(LogLikeMod.ModId, RedMistChallengeStageId))
            {
                PrepareBinahForRedMistChallenge();
                return;
            }

            ResetPrematureBinahLoadouts(binah.id);
            if (LogueBookModels.booklist != null)
                LogueBookModels.booklist.RemoveAll(book => book?.ClassInfo?.id == binah.id);
            Debug.Log("[RMR] Removed legacy early Binah unlock; it will become temporarily available when the Red Mist challenge is selected.");
        }

        private static void ResetPrematureBinahLoadouts(LorId binahId)
        {
            if (LogueBookModels.playerBattleModel == null)
                return;
            for (int index = 0; index < LogueBookModels.playerBattleModel.Count; index++)
            {
                UnitBattleDataModel battleModel = LogueBookModels.playerBattleModel[index];
                if (battleModel?.unitData?.bookItem?.ClassInfo?.id != binahId)
                    continue;
                BookXmlInfo defaultPage = Singleton<BookXmlList>.Instance.GetData(
                    new LorId(LogLikeMod.ModId, -854 - index));
                if (defaultPage != null)
                    LogueBookModels.EquipNewPage(battleModel, defaultPage, false);
            }
        }

        /// <summary>
        /// Legacy entry point --delegates to EnsureGrade6SpecialCorePagesUnlocked.
        /// </summary>
        public static void GrantGrade6SpecialCorePagesIfNeeded()
        {
            EnsureGrade6SpecialCorePagesUnlocked();
            ApplyBinahRedMistProgressionState();
        }

        public static bool IsBlackSilenceUnlockedForUrbanStar()
        {
            return LoadSimpleFlag(BlackSilenceStageClearedSaveName, "Cleared");
        }

        public static void RecordBlackSilenceStageClear()
        {
            SaveSimpleFlag(BlackSilenceStageClearedSaveName, "Cleared");
        }

        public static bool IsDistortedEnsembleUnlockedForUrbanStar()
        {
            return LoadSimpleFlag(DistortedEnsembleStageClearedSaveName, "Cleared");
        }

        public static void RecordDistortedEnsembleStageClear()
        {
            SaveSimpleFlag(DistortedEnsembleStageClearedSaveName, "Cleared");
        }

        public static void ResetAllArchiveProgress()
        {
            try
            {
                RMRAbnormalityUnlockManager.ResetAllPermanentProgress();
                ClearSimpleFlag(BlackSilenceStageClearedSaveName, "Cleared");
                ClearSimpleFlag(DistortedEnsembleStageClearedSaveName, "Cleared");
                ClearSimpleFlag(Grade6SpecialCorePagesGrantedSaveName, "Granted");

                LogueBookModels.EnsureCompendiumUnlocks();
                LogueBookModels.BeginPermanentCompendiumReset();
                try
                {
                    LogueBookModels.CompendiumUnlockedRoleBooks.Clear();
                    LogueBookModels.CompendiumUnlockedBattleCards.Clear();
                    LogueBookModels.CompendiumUnlockedAbnormalityPages.Clear();
                    LogueBookModels.CompendiumUnlockedEgoPages.Clear();
                    LogueBookModels.SavePermanentCompendiumData();
                }
                finally
                {
                    LogueBookModels.EndPermanentCompendiumReset();
                }
                Debug.Log("[RMR] Reset all archive progress: permanent atlas, realization clears, and special clear flags were cleared.");
            }
            catch (Exception e)
            {
                try { LogueBookModels.EndPermanentCompendiumReset(); } catch { }
                Debug.LogError($"[RMR] ResetAllArchiveProgress failed: {e}");
            }
        }

        private static void SaveGrantedFlagInternal()
        {
            SaveSimpleFlag(Grade6SpecialCorePagesGrantedSaveName, "Granted");
        }

        private static void SaveSimpleFlag(string saveName, string key)
        {
            try
            {
                SaveData data = new SaveData(SaveDataType.Dictionary);
                data.AddData(key, new SaveData(1));
                Singleton<LogueSaveManager>.Instance.SaveData(data, saveName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RMR] Failed to save flag {saveName}: {e.Message}");
            }
        }

        private static bool LoadSimpleFlag(string saveName, string key)
        {
            try
            {
                SaveData data = Singleton<LogueSaveManager>.Instance.LoadData(saveName);
                return data != null && data.GetInt(key) > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void ClearSimpleFlag(string saveName, string key)
        {
            try
            {
                SaveData data = new SaveData(SaveDataType.Dictionary);
                data.AddData(key, new SaveData(0));
                Singleton<LogueSaveManager>.Instance.SaveData(data, saveName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RMR] Failed to clear flag {saveName}: {e.Message}");
            }
        }

        #endregion
    }

    #region TECHNICAL INFRASTRUCTURE

    public static class RMRUtilityExtensions
    {
        /// <summary>
        /// Plays a sound. Useful in conjunction with CustomMapUtility.
        /// </summary>
        public static void PlaySound(this AudioClip audio, Transform transform, float VolumnControl = 1.5f, bool loop = false)
        {
            if (audio == null)
                return;
            if (SingletonBehavior<BattleSoundManager>.Instance == null || SingletonBehavior<BattleSoundManager>.Instance.effectSoundPrefab == null)
                return;
            BattleEffectSound battleEffectSound = UnityEngine.Object.Instantiate<BattleEffectSound>(SingletonBehavior<BattleSoundManager>.Instance.effectSoundPrefab, transform);
            float volume = SingletonBehavior<BattleSoundManager>.Instance.VolumeFX * VolumnControl;
            battleEffectSound.Init(audio, volume, loop);
        }

        /// <summary>
        /// Checks if the current hit is a crit. Should be set to true in-between BeforeRollDice calls.<br></br>
        /// Ideally should be called OnSucceedAttack.
        /// </summary>
        /// <returns></returns>
        public static bool isCrit(this BattleUnitModel model)
        {
            BattleUnitBuf_RMR_CritChance buf = (model.bufListDetail.GetActivatedBuf(RoguelikeBufs.CritChance) as BattleUnitBuf_RMR_CritChance);
            if (buf != null)
            {
                return buf.onCrit;
            }
            return false;
        }

        /// <summary>
        /// UI convenient way of quickly giving an unit an Emotion Coin/Point.
        /// </summary>
        /// <param name="posType">Whether it's positive or negative.</param>
        /// <param name="count">How many coins to give (default: 1).</param>
        public static void GiveEmotionCoin(this BattleUnitEmotionDetail detail, EmotionCoinType posType, int count = 1)
        {
            detail.CreateEmotionCoin(posType, count);
            SingletonBehavior<BattleManagerUI>.Instance.ui_battleEmotionCoinUI.OnAcquireCoin(detail._self, posType, count);
        }

        /// <summary>
        /// Set <u><see cref="BattleUnitView.deadEvent"/></u> to this in order to make an unit explode on death.
        /// </summary>
        public static void ExplodeOnDeath(BattleUnitView view)
        {
            if (view.model.history.data1 != 113413411) // magic number yes i know but this should just prevent the method from running twice
            {
                SoundEffectPlayer soundEffectPlayer = SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Creature/MatchGirl_Explosion");
                if (soundEffectPlayer != null)
                {
                    soundEffectPlayer.SetGlobalPosition(view.WorldPosition);
                }
                var effect = SingletonBehavior<DiceEffectManager>.Instance.CreateCreatureEffect("1/MatchGirl_Footfall", 1f, view, null, 2f);
                effect.transform.localScale *= 3.5f;
                effect.AttachEffectLayer();
                view.model.history.data1 = 113413411;
                view.StartDeadEffect(false);
                view.model._deadSceneBlock = true;
            }
        }

        /// <summary>
        /// Returns a random element from a given list.
        /// </summary>
        public static T SelectOneRandom<T>(this IList<T> list)
        {
            return list[Singleton<System.Random>.Instance.Next(list.Count)];
        }

        /// <summary>
        /// Returns several random elements from a given list.
        /// </summary>
        /// <param name="count">The number of random elements to be returned</param>
        /// <returns>A new list containing randomly selected elements from the original list. The same element may be picked multiple times.</returns>
        public static List<T> SelectManyRandom<T>(this IList<T> list, int count)
        {
            List<T> list2 = new List<T>();
            for (int x = 0; x < count; x++)
            {
                list2.Append(list[Singleton<System.Random>.Instance.Next(list.Count)]);
            }
            return list2;
        }

        /// <summary>
        /// Sorts a list of <see cref="BattleDiceCardModel"/>s (Combat Pages) by their current cost, in crescent order.
        /// </summary>
        /// <param name="list">The list to be sorted.</param>
        /// <returns></returns>
        public static void SortByCost(this List<BattleDiceCardModel> list)
        {
            list.Sort((BattleDiceCardModel x, BattleDiceCardModel y) => x.CurCost - y.CurCost);
        }

        /// <summary>
        /// Shuffles a list.
        /// </summary>
        public static List<T> Shuffle<T>(this IList<T> list)
        {
            return list.OrderBy(x => Singleton<System.Random>.Instance.Next()).ToList();
        }

        /// <summary>
        /// Creates a new list from an existing one, sorts it, then returns it.
        /// </summary>
        public static IList<T> SortReturn<T> (this IList<T> list, Comparison<T> comparer)
        {
            var list2 = list.ToList();
            list2.Sort(comparer);
            return list2;
        }

        /// <summary>
        /// Checks to see if a card or one of its dice posses a given keyword.
        /// </summary>
        /// <param name="card">The card to be tested.</param>
        /// <param name="keyword">The keyword to test the card for.</param>
        /// <returns>Returns <see langword="true"/> if the <paramref name="card"/> or any of its dice have the given <paramref name="keyword"/>. Returns <see langword="false"/> otherwise.</returns>
        public static bool CheckForKeyword(this BattleDiceCardModel card, string keyword)
        {
            if (card != null)
            {
                DiceCardXmlInfo xmlData = card.XmlData;
                if (xmlData == null)
                {
                    return false;
                }
                if (xmlData.Keywords.Contains(keyword))
                {
                    return true;
                }
                List<string> abilityKeywords = Singleton<BattleCardAbilityDescXmlList>.Instance.GetAbilityKeywords(xmlData);
                for (int i = 0; i < abilityKeywords.Count; i++)
                {
                    if (abilityKeywords[i] == keyword)
                    {
                        return true;
                    }
                }
                foreach (DiceBehaviour diceBehaviour in card.GetBehaviourList())
                {
                    List<string> abilityKeywords_byScript = Singleton<BattleCardAbilityDescXmlList>.Instance.GetAbilityKeywords_byScript(diceBehaviour.Script);
                    for (int j = 0; j < abilityKeywords_byScript.Count; j++)
                    {
                        if (abilityKeywords_byScript[j] == keyword)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }

        public static void RemoveAllAltMotion(this CharacterAppearance charAppearance)
        {
            charAppearance.RemoveAltMotion(ActionDetail.Default);
            charAppearance.RemoveAltMotion(ActionDetail.Damaged);
            charAppearance.RemoveAltMotion(ActionDetail.Standing);
            charAppearance.RemoveAltMotion(ActionDetail.Guard);
            charAppearance.RemoveAltMotion(ActionDetail.Aim);
            charAppearance.RemoveAltMotion(ActionDetail.Hit);
            charAppearance.RemoveAltMotion(ActionDetail.Slash);
            charAppearance.RemoveAltMotion(ActionDetail.Penetrate);
            charAppearance.RemoveAltMotion(ActionDetail.Move);
            charAppearance.RemoveAltMotion(ActionDetail.Fire);
            charAppearance.RemoveAltMotion(ActionDetail.Evade);
        }

        public static BattleUnitModel GetPatron(this BattleObjectManager manager)
        {
            if (manager == null)
                return null;
            var aliveList = manager.GetAliveList(Faction.Player);
            if (aliveList == null || aliveList.Count == 0)
                return null;
            return aliveList.SortReturn((BattleUnitModel x, BattleUnitModel y) => x.index - y.index)[0];
        }

        /// <summary>
        /// Replaces color shorthands from the XMLs.
        /// </summary>
        public static string ReplaceColorShorthands(this string input)
        {
            string newString = input;
            foreach (KeyValuePair<string, string> pair in colorShorthands)
            {
                newString = newString.Replace(pair.Key, pair.Value);
            }
            return newString;
        }

        public static Dictionary<string, string> colorShorthands = new Dictionary<string, string>
        {
            {"[/color]", "</color>" }, // close color tag
            {"[green]", "<color=#33DD11>"}, // open green color tag 
            {"[red]", "<color=#DD3311>"}, // open red color tag
            {"[mirror]", "<color=#5163D6>" }, // open light blue color tag
            {"[yellow]", "<color=#FFD800>" }, // open yellow color tag
            {"[i]", "<i>"}, // open italicized tag
            {"[/i]", "</i>" }, // close italicized tag

            // rarities
            {"[common]", "<color=#" + ColorUtility.ToHtmlStringRGB(UIColorManager.Manager.GetEquipRarityColor(Rarity.Common)) +">"  },
            {"[uncommon]", "<color=#" + ColorUtility.ToHtmlStringRGB(UIColorManager.Manager.GetEquipRarityColor(Rarity.Uncommon)) +">"  },
            {"[rare]", "<color=#" + ColorUtility.ToHtmlStringRGB(UIColorManager.Manager.GetEquipRarityColor(Rarity.Rare)) +">"  },
            {"[unique]", "<color=#" + ColorUtility.ToHtmlStringRGB(UIColorManager.Manager.GetEquipRarityColor(Rarity.Unique)) +">"  }
        };

        public static GlobalLogueEffectBase GetRandomEffect(this GlobalLogueEffectManager manager, Rarity rarity)
        {
            List<TypeInfo> items = new List<TypeInfo>();
            foreach (var assembly in LogLikeMod.GetAssemList())
            {
                var randomItems = assembly.DefinedTypes.ToList().FindAll(x => x.IsSubclassOf(typeof(GlobalLogueEffectBase))).FindAll(x => x.GetField("IsRandom", BindingFlags.Static | BindingFlags.Public) is var randomy && randomy != null && (bool)randomy.GetValue(null));
                var randomItemsRare = randomItems.FindAll(x => x.GetField("ItemRarity", BindingFlags.Static | BindingFlags.Public) is var rare && rare != null && (Rarity)rare.GetValue(null) == rarity);
                items.AddRange(randomItemsRare);
            }
            return (GlobalLogueEffectBase)Activator.CreateInstance(items.SelectOneRandom().AsType());
        }

        /// <summary>
        /// Quick method to simply check if a card can redirect another.
        /// </summary>
        /// <param name="card">The card to redirect.</param>
        /// <param name="owner">The owner of the card.</param>
        public static bool CanIRedirectPls(this BattlePlayingCardDataInUnitModel card, BattleUnitModel owner)
        {
            if (card.cardAbility != null && !card.cardAbility.IsTargetChangable(owner))
            {
                return false;
            }
            if (card.card.GetSpec().Ranged == CardRange.FarArea || card.card.GetSpec().Ranged == CardRange.FarAreaEach)
            {
                return false;
            }
            if (card.card.GetSpec().affection == CardAffection.TeamNear || card.card.GetSpec().affection == CardAffection.All)
            {
                return false;
            }
            return true;
        }
        
        public static void AddMaxLight(this BattleUnitCostUI costUI, int light)
        {
            costUI.fixedMaxcost += light;
            for (int i = 0; i < light; i++)
            {
                var item = new BattleUnitCostUI.CostBattleUIImage(UnityEngine.Object.Instantiate(costUI.prefab_cost, costUI.lowerLine));
                costUI.costLists.Add(item);
                item.parent.SetActive(false);
            }
        }

        public static void DeselectShitButActuallyWorks(BattleUnitModel owner)
        {
            int cardorder = owner.view.model.cardOrder;
            for (int i = 0; i < owner.cardSlotDetail.cardAry.Count; i++)
            {
                if (owner.cardSlotDetail.cardAry[i] != null)
                {
                    owner.view.model.cardOrder = i;
                    owner.view.speedDiceSetterUI._speedDices[i].DeselectCard();
                    owner.view.speedDiceSetterUI._speedDices[i].HideDicePreview(false);
                }

            }
            owner.view.model.cardOrder = cardorder;
        }

        /// <summary>
        /// Adds speed to a single Speed Die during the card selection phase.
        /// </summary>
        /// <param name="order">Which die to increase the speed of.</param>
        /// <param name="speed">How much speed to increment.</param>
        public static void AddSpeedImmediately(this BattleUnitModel unit, int order, int speed)
        {
            if (order < 0 || order >= unit.speedDiceResult.Count) return;
            int changeValue = speed;
            if (unit.speedDiceResult != null)
            {
                var speedDie = unit.speedDiceResult[order];
                speedDie.value = AddSpeedWithCheck(speedDie.value, changeValue);

                if (unit.view?.speedDiceSetterUI != null)
                {
                    List<(bool breaked, bool locked, bool blockedForcely)> diceStates = new List<(bool, bool, bool)>();
                    foreach (var speedDieUi in unit.view.speedDiceSetterUI._speedDices)
                    {
                        diceStates.Add((speedDieUi._bBreakedDice, speedDieUi._lockDiceRoot.activeSelf, speedDieUi.anim_BlockSelectDice.GetBool("isSelected")));
                    }
                    unit.view.speedDiceSetterUI.SetSpeedDicesBeforeRoll(unit.speedDiceResult, unit.faction);
                    unit.view.speedDiceSetterUI.SetSpeedDicesAfterRoll(unit.speedDiceResult);
                    int i = 0;
                    foreach (var speedDieUi in unit.view.speedDiceSetterUI._speedDices)
                    {
                        if (i < diceStates.Count)
                        {
                            speedDieUi.BreakDice(diceStates[i].breaked, diceStates[i].locked);
                            speedDieUi.BlockDice(diceStates[i].blockedForcely, diceStates[i].blockedForcely);
                            i++;
                        }
                    }
                }
            }
            var card = unit.cardSlotDetail.cardAry[order];
            if (card != null)
            {
                card.speedDiceResultValue = AddSpeedWithCheck(card.speedDiceResultValue, changeValue);
            }



            int AddSpeedWithCheck(int origin, int adder)
            {
                return math.clamp(origin + adder, Math.Min(origin, 1), Math.Max(origin, 999));
            }
        }

        /// <summary>
        /// Adds speed to all Speed Die of an unit during the card selection phase.
        /// </summary>
        /// <param name="stack">How much speed to increment.</param>
        public static void AddSpeedBufImmediatelyAll(this BattleUnitModel unit, int stack)
        {
            int changeValue = stack;
            if (unit.speedDiceResult != null)
            {
                foreach (var speedDie in unit.speedDiceResult)
                {
                    speedDie.value = AddSpeedWithCheck(speedDie.value, changeValue);
                }
                if (unit.view?.speedDiceSetterUI != null)
                {
                    List<(bool breaked, bool locked, bool blockedForcely)> diceStates = new List<(bool, bool, bool)>();
                    foreach (var speedDieUi in unit.view.speedDiceSetterUI._speedDices)
                    {
                        diceStates.Add((speedDieUi._bBreakedDice, speedDieUi._lockDiceRoot.activeSelf, speedDieUi.anim_BlockSelectDice.GetBool("isSelected")));
                    }
                    unit.view.speedDiceSetterUI.SetSpeedDicesBeforeRoll(unit.speedDiceResult, unit.faction);
                    unit.view.speedDiceSetterUI.SetSpeedDicesAfterRoll(unit.speedDiceResult);
                    int i = 0;
                    foreach (var speedDieUi in unit.view.speedDiceSetterUI._speedDices)
                    {
                        if (i < diceStates.Count)
                        {
                            speedDieUi.BreakDice(diceStates[i].breaked, diceStates[i].locked);
                            speedDieUi.BlockDice(diceStates[i].blockedForcely, diceStates[i].blockedForcely);
                            i++;
                        }
                    }
                }
            }
            foreach (var card in unit.cardSlotDetail.cardAry)
            {
                if (card != null)
                {
                    card.speedDiceResultValue = AddSpeedWithCheck(card.speedDiceResultValue, changeValue);
                }
            }

            int AddSpeedWithCheck(int origin, int adder)
            {
                return math.clamp(origin + adder, Math.Min(origin, 1), Math.Max(origin, 999));
            }
        }

        /// <summary>
        /// Methods for quickly making BattleDiceBehaviors.
        /// </summary>
        public static BattleDiceBehavior CreateBattleDiceBehavior(int min, int max, BehaviourDetail detail, BehaviourType type, MotionDetail mdetail = MotionDetail.N, string script = "", string actionscript = "", string effectres = "", DiceStatBonus bonus = null)
        {
            BattleDiceBehavior battleDiceBehavior = new BattleDiceBehavior
            {
                behaviourInCard = new DiceBehaviour
                {
                    Min = min,
                    Dice = max,
                    ActionScript = actionscript,
                    Desc = "",
                    Detail = detail,
                    EffectRes = effectres,
                    MotionDetail = mdetail,
                    MotionDetailDefault = MotionDetail.N,
                    Script = script,
                    Type = type
                }
            };
            if (bonus != null)
            {
                battleDiceBehavior.ApplyDiceStatBonus(bonus);
            }

            if (script != string.Empty)
            {
                DiceCardAbilityBase diceCardAbilityBase = Singleton<AssemblyManager>.Instance.CreateInstance_DiceCardAbility(script);
                if (diceCardAbilityBase != null)
                {
                    battleDiceBehavior.AddAbility(diceCardAbilityBase);
                }
            }

            return battleDiceBehavior;
        }
        /// <summary>
        /// Methods for quickly converting a DiceBehaviour into a BattleDiceBehavior.
        /// </summary>
        public static BattleDiceBehavior CreateBattleDiceBehavior(DiceBehaviour die, DiceStatBonus bonus = null)
        {
            BattleDiceBehavior battleDiceBehavior = new BattleDiceBehavior
            {
                behaviourInCard = die.Copy()
            };
            
            if (!string.IsNullOrEmpty(die.Script))
            {
                DiceCardAbilityBase diceCardAbilityBase = Singleton<AssemblyManager>.Instance.CreateInstance_DiceCardAbility(die.Script);
                if (diceCardAbilityBase != null)
                {
                    battleDiceBehavior.AddAbility(diceCardAbilityBase);
                }
            }

            return battleDiceBehavior;
        }

        /// <summary>
        /// Essentially just a safer and more reliable CopyAbilityAndStat method from vanilla.
        /// </summary>
        public static void BetterCopyAbilityAndStat(this BattleDiceBehavior origin, BattleDiceBehavior receiver, bool copyDesc = true)
        {
            foreach (DiceCardAbilityBase diceCardAbilityBase in origin.abilityList)
            {
                try
                {
                    string script = Singleton<AssemblyManager>.Instance._diceCardAbilityDict._dict.FirstOrDefault(x => x.Value == diceCardAbilityBase.GetType()).Key;
                    DiceCardAbilityBase diceCardAbilityBase2 = Singleton<AssemblyManager>.Instance.CreateInstance_DiceCardAbility(script);
                    receiver.AddAbility(diceCardAbilityBase2);
                }
                catch { }
            }
            if (copyDesc)
            {
                try
                {
                    receiver.behaviourInCard.Desc = origin.behaviourInCard.Desc;
                }
                catch { }
            }
            try
            {
                receiver._statBonus = origin._statBonus.Copy();
            }
            catch { }
            try
            {
                receiver._firstStatBonus = origin._firstStatBonus.Copy();
            }
            catch { }
        }
    }

    [Serializable]
    public class RMRConfigRoot
    {
        [XmlElement("ShowAdditionalLogging")]
        public bool EnableAdditionalLogging = false;

        [XmlElement("ShowAllItemCatalog")]
        public bool ShowAllItemCatalog = false;
    }

    #region UNIT UTIL
    /// <summary>
    /// A simplified BattleUnitModel object for more easily cloning BattleUnitModels.
    /// </summary>
    public class UnitModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Pos { get; set; }

        public SephirahType Sephirah { get; set; }

        public bool LockedEmotion { get; set; }

        public int MaxEmotionLevel { get; set; } = 0;

        public int EmotionLevel { get; set; }

        public bool AddEmotionPassive { get; set; } = true;

        public bool OnWaveStart { get; set; }

        public XmlVector2 CustomPos { get; set; }
    }

    /// <summary>
    /// Initially taken from CWR (with permission from undefined) before being improved upon 
    /// <br></br>by fixing some UI bugs and other unrelated jank and by adding some shortcuts for
    /// <br></br>storing BattleUnitModels easily.
    /// </summary>
    public static class UnitUtil
    {
        /// <summary>enum UnitSpawnMethod</summary>
        public enum UnitSpawnMethod
        {
            Default,
            Summoned = 7,
            Cloned = 8
        }

        /// <summary>
        /// Copies a <see cref="BattleUnitModel"/> into a simplified <see cref="UnitModel"/> for cloning purposes.
        /// <br></br>Useful for adding controllable units with <see cref="AddModdedUnitPlayerSide"/>.
        /// </summary>
        public static UnitModel Copy(this BattleUnitModel model) => new UnitModel()
        {
            Id = model.id,
            Name = model.UnitData.unitData.name,
            Pos = model.index,
            Sephirah = model.UnitData.unitData.OwnerSephirah,
            LockedEmotion = false,
            MaxEmotionLevel = model.emotionDetail.MaximumEmotionLevel,
            EmotionLevel = model.emotionDetail.EmotionLevel,
            OnWaveStart = true,
            CustomPos = new XmlVector2 { x = model.formation.Pos.x, y = model.formation.Pos.y }
        };

        /// <summary>
        /// Clones an existing BattleUnitModel and summons them.
        /// </summary>
        /// <returns>The cloned <see cref="BattleUnitModel"/>.</returns>
        public static BattleUnitModel CopyModdedUnit(this StageController instance, Faction faction, BattleUnitModel cloner, int index = -1, int height = -1, XmlVector2 position = null)
        {
            UnitBattleDataModel unitBattleDataModel = new UnitBattleDataModel(instance.GetStageModel(), cloner.UnitData.unitData);
            if (faction > Faction.Enemy)
            {
                FieldInfo fieldInfo = AccessTools.Field(typeof(UnitDataModel), "_ownerSephirah");
                fieldInfo.SetValue(unitBattleDataModel.unitData, instance.CurrentFloor);
            }
            if (height != -1)
            {
                unitBattleDataModel.unitData.customizeData.height = height;
            }
            BattleUnitModel battleUnitModel = BattleObjectManager.CreateDefaultUnit(faction);
            UnitDataModel unitData = unitBattleDataModel.unitData;
            if (index < 0)
            {
                IEnumerable<int> source = from y in BattleObjectManager.instance.GetAliveList(faction)
                                          select y.index;
                int num = 0;
                while (index < 0)
                {
                    if (!source.Contains(num))
                    {
                        index = num;
                        break;
                    }
                    num++;
                }
            }
            battleUnitModel.index = index;
            battleUnitModel.grade = unitData.grade;
            if (faction == Faction.Enemy)
            {
                StageWaveModel currentWaveModel = instance.GetCurrentWaveModel();
                if (position != null)
                {
                    battleUnitModel.formation = new FormationPosition(new FormationPositionXmlData
                    {
                        vector = position
                    });
                }
                else
                {
                    int num2 = Mathf.Min(battleUnitModel.index, currentWaveModel.GetFormation().PostionList.Count - 1);
                    if (num2 < battleUnitModel.index)
                    {
                        Debug.Log("UnitUtil: Index higher than available formation positions, summoning at highest value possible");
                    }
                    battleUnitModel.formation = new FormationPosition(new FormationPositionXmlData
                    {
                        vector = new XmlVector2
                        {
                            x = currentWaveModel.GetFormationPosition(num2).Pos.x,
                            y = currentWaveModel.GetFormationPosition(num2).Pos.y
                        }
                    });
                }
            }
            else
            {
                StageLibraryFloorModel floor = instance.GetStageModel().GetFloor(instance.CurrentFloor);
                if (position != null)
                {
                    battleUnitModel.formation = new FormationPosition(new FormationPositionXmlData
                    {
                        vector = position
                    });
                }
                else
                {
                    int num3 = Mathf.Min(battleUnitModel.index, floor.GetFormation().PostionList.Count - 1);
                    if (num3 < battleUnitModel.index)
                    {
                        Debug.Log("AftermathCollection: Index higher than available formation positions, summoning at highest value possible");
                    }
                    battleUnitModel.formation = new FormationPosition(new FormationPositionXmlData
                    {
                        vector = new XmlVector2
                        {
                            x = floor.GetFormationPosition(num3).Pos.x,
                            y = floor.GetFormationPosition(num3).Pos.y
                        }
                    });
                }
            }
            BattleUnitModel result;
            if (unitBattleDataModel.isDead)
            {
                result = battleUnitModel;
            }
            else
            {
                battleUnitModel.SetUnitData(unitBattleDataModel);
                battleUnitModel.OnCreated();
                BattleObjectManager.instance.RegisterUnit(battleUnitModel);
                battleUnitModel.passiveDetail.OnUnitCreated();
                UnitUtil.AddEmotionPassives(battleUnitModel);
                battleUnitModel.cardSlotDetail.RecoverPlayPoint(battleUnitModel.cardSlotDetail.GetMaxPlayPoint());
                battleUnitModel.OnWaveStart();
                UnitUtil.LevelUpEmotion(battleUnitModel, 0);
                UnitUtil.InitializeCombatUI(battleUnitModel);
                battleUnitModel.history.data2 = (int)UnitSpawnMethod.Cloned;
                result = battleUnitModel;
            }
            return result;
        }

        /// <summary>
        /// Summons a new unit.
        /// </summary>
        /// <returns>The summoned <see cref="BattleUnitModel"/>.</returns>
        public static BattleUnitModel AddModdedUnit(this StageController instance, Faction faction, LorId enemyUnitId, int index = -1, int height = -1, XmlVector2 position = null)
        {
            UnitBattleDataModel unitBattleDataModel = UnitBattleDataModel.CreateUnitBattleDataByEnemyUnitId(instance.GetStageModel(), enemyUnitId);
            if (faction > Faction.Enemy)
            {
                FieldInfo fieldInfo = AccessTools.Field(typeof(UnitDataModel), "_ownerSephirah");
                fieldInfo.SetValue(unitBattleDataModel.unitData, instance.CurrentFloor);
            }
            if (height != -1)
            {
                unitBattleDataModel.unitData.customizeData.height = height;
            }
            BattleObjectManager.instance.UnregisterUnitByIndex(faction, index);
            BattleUnitModel battleUnitModel = BattleObjectManager.CreateDefaultUnit(faction);
            UnitDataModel unitData = unitBattleDataModel.unitData;
            if (index < 0)
            {
                IEnumerable<int> source = from y in BattleObjectManager.instance.GetAliveList(faction)
                                          select y.index;
                int num = 0;
                while (index < 0)
                {
                    if (!source.Contains(num))
                    {
                        index = num;
                        break;
                    }
                    num++;
                }
            }
            battleUnitModel.index = index;
            battleUnitModel.grade = unitData.grade;
            if (faction == Faction.Enemy)
            {
                StageWaveModel currentWaveModel = instance.GetCurrentWaveModel();
                if (position != null)
                {
                    battleUnitModel.formation = new FormationPosition(new FormationPositionXmlData
                    {
                        vector = position
                    });
                }
                else
                {
                    int num2 = Mathf.Min(battleUnitModel.index, currentWaveModel.GetFormation().PostionList.Count - 1);
                    if (num2 < battleUnitModel.index)
                    {
                        Debug.Log("UnitUtil: Index higher than available formation positions, summoning at highest value possible");
                    }
                    battleUnitModel.formation = new FormationPosition(new FormationPositionXmlData
                    {
                        vector = new XmlVector2
                        {
                            x = currentWaveModel.GetFormationPosition(num2).Pos.x,
                            y = currentWaveModel.GetFormationPosition(num2).Pos.y
                        }
                    });
                }
            }
            else
            {
                StageLibraryFloorModel floor = instance.GetStageModel().GetFloor(instance.CurrentFloor);
                if (position != null)
                {
                    battleUnitModel.formation = new FormationPosition(new FormationPositionXmlData
                    {
                        vector = position
                    });
                }
                else
                {
                    int num3 = Mathf.Min(battleUnitModel.index, floor.GetFormation().PostionList.Count - 1);
                    if (num3 < battleUnitModel.index)
                    {
                        Debug.Log("UnitUtil: Index higher than available formation positions, summoning at highest value possible");
                    }
                    battleUnitModel.formation = new FormationPosition(new FormationPositionXmlData
                    {
                        vector = new XmlVector2
                        {
                            x = floor.GetFormationPosition(num3).Pos.x,
                            y = floor.GetFormationPosition(num3).Pos.y
                        }
                    });
                }
            }
            BattleUnitModel result;
            if (unitBattleDataModel.isDead)
            {
                result = battleUnitModel;
            }
            else
            {
                battleUnitModel.SetUnitData(unitBattleDataModel);
                battleUnitModel.OnCreated();
                BattleObjectManager.instance.RegisterUnit(battleUnitModel);
                battleUnitModel.passiveDetail.OnUnitCreated();
                UnitUtil.AddEmotionPassives(battleUnitModel);
                battleUnitModel.cardSlotDetail.RecoverPlayPoint(battleUnitModel.cardSlotDetail.GetMaxPlayPoint());
                battleUnitModel.OnWaveStart();
                battleUnitModel.allyCardDetail.DrawCards(battleUnitModel.UnitData.unitData.GetStartDraw());
                UnitUtil.LevelUpEmotion(battleUnitModel, 0);
                UnitUtil.InitializeCombatUI(battleUnitModel);
                battleUnitModel.history.data2 = (int)UnitSpawnMethod.Summoned;
                result = battleUnitModel;
            }
            return result;
        }

        /// <summary>
        /// Summons an unit that can be controlled by the player.
        /// </summary>
        /// <returns>The summoned <see cref="BattleUnitModel"/>.</returns>
        public static BattleUnitModel AddModdedUnitPlayerSide(this StageController instance, UnitModel unit, string packageId, bool playerSide = true)
        {
            StageLibraryFloorModel floor = instance.GetStageModel().GetFloor(instance.CurrentFloor);
            UnitDataModel unitDataModel = new UnitDataModel(new LorId(packageId, unit.Id), playerSide ? floor.Sephirah : SephirahType.None, false);
            unitDataModel.SetCustomName(unit.Name);
            BattleUnitModel battleUnitModel = BattleObjectManager.CreateDefaultUnit(playerSide ? Faction.Player : Faction.Enemy);
            battleUnitModel.index = unit.Pos;
            battleUnitModel.grade = unitDataModel.grade;
            battleUnitModel.formation = ((unit.CustomPos != null) ? new FormationPosition(new FormationPositionXmlData
            {
                vector = unit.CustomPos
            }) : floor.GetFormationPosition(battleUnitModel.index));
            UnitBattleDataModel unitBattleDataModel = new UnitBattleDataModel(instance.GetStageModel(), unitDataModel);
            unitBattleDataModel.Init();
            battleUnitModel.SetUnitData(unitBattleDataModel);
            battleUnitModel.OnCreated();
            BattleObjectManager.instance.RegisterUnit(battleUnitModel);
            battleUnitModel.passiveDetail.OnUnitCreated();
            UnitUtil.LevelUpEmotion(battleUnitModel, unit.EmotionLevel);
            if (unit.LockedEmotion)
            {
                battleUnitModel.emotionDetail.SetMaxEmotionLevel(unit.MaxEmotionLevel);
            }
            battleUnitModel.allyCardDetail.DrawCards(battleUnitModel.UnitData.unitData.GetStartDraw());
            battleUnitModel.cardSlotDetail.RecoverPlayPoint(battleUnitModel.cardSlotDetail.GetMaxPlayPoint());
            if (unit.AddEmotionPassive)
            {
                UnitUtil.AddEmotionPassives(battleUnitModel);
            }
            battleUnitModel.OnWaveStart();
            UnitUtil.InitializeCombatUI(battleUnitModel);
            battleUnitModel.history.data2 = (int)UnitSpawnMethod.Summoned;
            return battleUnitModel;
        }

        /// <summary>
        /// Refreshes the UI after summoning <see cref="BattleUnitModel"/>s.
        /// <br></br>(If you're summoning just one or several units at once, call this after you're done summoning units to update the UI.)
        /// </summary>
        /// <param name="forceReturn">Forces the units to return to their assigned position in the formation.</param>
        public static void RefreshCombatUI(bool forceReturn = false)
        {
            foreach (ValueTuple<BattleUnitModel, int> valueTuple in BattleObjectManager.instance.GetList().Select((BattleUnitModel value, int i) => new ValueTuple<BattleUnitModel, int>(value, i)))
            {
                SingletonBehavior<UICharacterRenderer>.Instance.SetCharacter(valueTuple.Item1.UnitData.unitData, valueTuple.Item2, true, false);
                if (forceReturn)
                {
                    valueTuple.Item1.moveDetail.ReturnToFormationByBlink(true);
                }
            }
            try
            {
                BattleObjectManager.instance.InitUI();
            }
            catch (IndexOutOfRangeException)
            {
            }
        }

        /// <summary>
        /// Levels up an unit's emotion level.
        /// </summary>
        /// <param name="unit">Which unit should level up.</param>
        /// <param name="value">How much they should level up.</param>
        public static void LevelUpEmotion(BattleUnitModel unit, int value)
        {
            for (int i = 0; i < value; i++)
            {
                unit.emotionDetail.LevelUp_Forcely(1);
                unit.emotionDetail.CheckLevelUp();
            }
            Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.UpdateCoin();
        }

        /// <summary>
        /// Stores a single unit in Battle Unit Storage.
        /// </summary>
        /// <param name="unit">The unit to be stored.</param>
        /// <param name="packageId">The packageId of the mod.</param>
        public static void StoreBattleUnitModel(BattleUnitModel unit, string packageId)
        {
            Singleton<StageController>.Instance.GetStageModel().GetStageStorageData<List<BattleUnitModel>>(packageId + "_BattleUnitModelStorage", out var output);
            if (output != null && output.Count > 0)
            {
                output.Add(unit);
                Singleton<StageController>.Instance.GetStageModel().SetStageStorgeData(packageId + "_BattleUnitModelStorage", output);
            }
            else
            {
                var list = new List<BattleUnitModel> { unit };
                Singleton<StageController>.Instance.GetStageModel().SetStageStorgeData(packageId + "_BattleUnitModelStorage", list);
            }

        }

        /// <summary>
        /// Stores multiple units in Battle Unit Storage.
        /// </summary>
        /// <param name="units">The units to be stored.</param>
        /// <param name="packageId">The packageId of the mod.</param>
        public static void StoreBattleUnitModel(List<BattleUnitModel> units, string packageId)
        {
            Singleton<StageController>.Instance.GetStageModel().GetStageStorageData<List<BattleUnitModel>>(packageId + "_BattleUnitModelStorage", out var output);
            if (output != null && output.Count > 0)
            {
                output.AddRange(units);
                Singleton<StageController>.Instance.GetStageModel().SetStageStorgeData(packageId + "_BattleUnitModelStorage", output);
            }
            else return;
        }

        /// <param name="packageId">The packageId of the mod.</param>
        /// <returns>A list of <see cref="BattleUnitModel"/>s containing the stored units in Battle Unit Storage.</returns>
        public static List<BattleUnitModel> GetStoredUnitModels(string packageId)
        {
            Singleton<StageController>.Instance.GetStageModel().GetStageStorageData<List<BattleUnitModel>>(packageId + "_BattleUnitModelStorage", out var output);
            if (output != null && output.Count > 0) return output;
            return null;
        }

        /// <summary>
        /// Clears out Battle Unit Storage.
        /// </summary>
        /// <param name="packageId">The packageId of the mod.</param>
        public static void ClearBattleUnitStorage(string packageId)
        {
            Singleton<StageController>.Instance.GetStageModel().SetStageStorgeData(packageId + "_BattleUnitModelStorage", null);
        }

        /// <summary>
        /// Returns the method through which an unit was summoned which can range between <see cref="UnitSpawnMethod.Default"/>, <see cref="UnitSpawnMethod.Summoned"/> or <see cref="UnitSpawnMethod.Cloned"/>.
        /// </summary>
        /// <param name="unit">The unit to check.</param>
        /// <returns>A <see cref="UnitSpawnMethod"/> containing the spawn method of how the unit was spawned.<b><br></br>Only works with units summoned by <see cref="UnitUtil"/>. Any other units will return <see cref="UnitSpawnMethod.Default"/>.</b></returns>
        public static UnitSpawnMethod GetUnitSpawnMethod(BattleUnitModel unit)
        {
            return (UnitSpawnMethod)unit.history.data2;
        }

        private static void InitializeCombatUI(BattleUnitModel battleUnitModel)
        {
            try
            {
                BattleCharacterProfileUI battleUI = SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.GetProfileUI(battleUnitModel);
                if (battleUI == null)
                {
                    battleUI = new BattleCharacterProfileUI();
                    battleUI.gameObject.SetActive(true);
                    battleUI.Initialize();
                    battleUI.SetUnitModel(battleUnitModel);
                    if (battleUnitModel.faction == Faction.Player)
                    {
                        SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.allyProfileArray[battleUnitModel.index] = battleUI;
                    }
                    else
                    {
                        SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.enemyProfileArray[battleUnitModel.index] = battleUI;
                    }

                }
            }
            catch (Exception e)
            {
                if (e.Message == "")
                    Debug.Log("UnitUtil: successfully summoned " + battleUnitModel.UnitData.unitData.name + " at index " + battleUnitModel.index);
                else
                    Debug.Log("UnitUtil - failed to initialize UI of summoned unit: " + e);
            }
        }

        private static void AddEmotionPassives(BattleUnitModel unit)
        {
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(Faction.Player);
            if (aliveList.Any<BattleUnitModel>())
            {
                foreach (BattleEmotionCardModel battleEmotionCardModel in from x in aliveList.FirstOrDefault<BattleUnitModel>().emotionDetail.PassiveList
                                                                          where x.XmlInfo.TargetType == EmotionTargetType.AllIncludingEnemy || x.XmlInfo.TargetType == EmotionTargetType.All
                                                                          select x)
                {
                    bool flag2 = unit.faction != Faction.Enemy || battleEmotionCardModel.XmlInfo.TargetType > EmotionTargetType.All;
                    if (flag2)
                    {
                        unit.emotionDetail.ApplyEmotionCard(battleEmotionCardModel.XmlInfo, false);
                    }
                }
            }
        }
    }
    
	#endregion

    #region SCROLL ABILITY UTIL
    /// <summary>
    /// This utility was made by StartUp for the Da'at Floor Mod in order to create On Scroll effects.<br></br>
    /// It has been used with due permission from both StartUp himself and the current coordinator<br></br>
    /// of the Da'at Floor Mod (at the time of writing, Dememenic).<br></br><br></br>
    /// If you plan on taking this for yourself, please credit StartUp for ScrollAbilityUtil, and keep this summary tag here.
    /// <br></br>Oh yeah, and as a bonus, you're gonna need to reference UnityEngine.InputLegacyModule besides the regular CoreModule
    /// <br></br>in order for this to work.
    /// </summary>
    internal static class ScrollAbilityUtil
    {
        internal static ScrollAbilityManager GetScrollAbilityManager(this BattleUnitModel model)
            => model.view.GetComponent<ScrollAbilityManager>() ?? model.view.gameObject.AddComponent<ScrollAbilityManager>();

        internal static void AddScrollAbility(this BattleUnitModel model, BattleDiceCardModel card, ScrollAbilityBase ability)
            => model.GetScrollAbilityManager().AddAbility(card, ability);

        internal static void AddScrollAbility<T>(this BattleUnitModel model, BattleDiceCardModel card) where T : ScrollAbilityBase, new()
            => model.AddScrollAbility(card, new T());
    }

    /// <summary>ScrollAbilityManager</summary>

    public class ScrollAbilityManager : MonoBehaviour
    {
        private bool scrolled;
        private BattleUnitModel owner;
        private Dictionary<BattleDiceCardModel, ScrollAbilityBase> _dict = new Dictionary<BattleDiceCardModel, ScrollAbilityBase>();

        private void Awake()
            => owner = gameObject.GetComponent<BattleUnitView>().model;

        private void Update()
        {
            if (scrolled) scrolled = !(BattleCamManager.Instance.scrollable = true);

            if (StageController.Instance.Phase != StageController.StagePhase.ApplyLibrarianCardPhase) return;

            var handUI = BattleManagerUI.Instance.ui_unitCardsInHand;
            if (handUI.SelectedModel != owner) return;

            var uiCard = handUI.GetSelectedCard();
            if (uiCard == null) return;

            var card = uiCard.CardModel;
            if (card == null || !_dict.ContainsKey(card)) return;

            scrolled = BattleCamManager.Instance.scrollable;
            BattleCamManager.Instance.scrollable = false;

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < float.Epsilon) return;

            if (scroll > 0)
                _dict[card].OnScrollUp(owner, card);
            else
                _dict[card].OnScrollDown(owner, card);

            uiCard.SetCard(card);
            ((Singleton<StageController>.Instance.AllyFormationDirection == Direction.RIGHT)
                ? SingletonBehavior<BattleManagerUI>.Instance.ui_unitInformationPlayer
                : SingletonBehavior<BattleManagerUI>.Instance.ui_unitInformation).ShowPreviewCard(card, true);
            uiCard.KeywordListUI.Activate();
        }


        public void AddAbility(BattleDiceCardModel card, ScrollAbilityBase ability, bool overrideIfExist = false)
        {
            if (card is null) throw new ArgumentNullException(nameof(card));
            if (ability is null) throw new ArgumentNullException(nameof(ability));
            if (_dict.ContainsKey(card) && !overrideIfExist) return;
            _dict[card] = ability;
        }
    }

    /// <summary>ScrollAbilityBase</summary>

    public class ScrollAbilityBase
    {
        public virtual void OnScrollUp(BattleUnitModel unit, BattleDiceCardModel self) { }
        public virtual void OnScrollDown(BattleUnitModel unit, BattleDiceCardModel self) { }
    }
    #endregion

    #region MYSTERY EVENT LOC.
    public class RogueMysteryXmlList : Singleton<RogueMysteryXmlList>
    {
        public void Init(string language)
        {
            string str = Path.Combine("Localize", language);
            string ogpath = Path.Combine(ModContentManager.Instance.GetModPath(RMRCore.packageId), "Assemblies", "dlls");

            if (Directory.Exists(Path.Combine(ogpath, str, "MysteryEvents")))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(ogpath, str, "MysteryEvents"));
                // Only real .xml --*.bak / *.pre_* backups were being deserialized and overwriting good text.
                foreach (System.IO.FileInfo fileinfo in LogLikeMod.EnumerateXmlFiles(Path.Combine(ogpath, str, "MysteryEvents")))
                {
                    try
                    {
                        using (var stream = fileinfo.OpenRead())
                        {
                            var root = new XmlSerializer(typeof(RogueMysteryXmlRoot)).Deserialize(stream) as RogueMysteryXmlRoot;
                            if (root?.RogueMysteryList == null)
                                continue;
                            foreach (var info in root.RogueMysteryList)
                            {
                                var lorid = new LorId(RMRCore.packageId, info.ID);
                                if (!mysteryDict.ContainsKey(lorid))
                                    this.mysteryDict.Add(lorid, info);
                                else
                                    this.mysteryDict[lorid] = info;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Error while trying to load XML file " + fileinfo.Name + ": " + e);
                    }
                }

            }

            foreach (ModContentInfo modContentInfo in LogLikeMod.GetLogMods())
            {
                string uniqueId = modContentInfo.invInfo.workshopInfo.uniqueId;
                string mysteryDir = Path.Combine(modContentInfo.GetLogDllPath(), str, "MysteryEvents");
                if (Directory.Exists(mysteryDir))
                {
                    foreach (System.IO.FileInfo fileinfo2 in LogLikeMod.EnumerateXmlFiles(mysteryDir))
                    {
                        try
                        {
                            using (var stream = fileinfo2.OpenRead())
                            {
                                var root = new XmlSerializer(typeof(RogueMysteryXmlRoot)).Deserialize(stream) as RogueMysteryXmlRoot;
                                if (root?.RogueMysteryList == null)
                                    continue;
                                foreach (var info in root.RogueMysteryList)
                                {
                                    var lorid = new LorId(uniqueId, info.ID);
                                    if (!mysteryDict.ContainsKey(lorid))
                                        this.mysteryDict.Add(lorid, info);
                                    else
                                        this.mysteryDict[lorid] = info;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Log("Error while trying to load XML file " + fileinfo2.Name + ": " + e);
                        }
                    }
                }

            }
        }

        public RogueMysteryXmlInfo GetLocalizedMystery(LorId id)
        {
            if (mysteryDict.ContainsKey(id))
                return mysteryDict[id];
            return null;
        }

        public Dictionary<LorId, RogueMysteryXmlInfo> mysteryDict = new Dictionary<LorId, RogueMysteryXmlInfo>();
    }

    /// <summary>RogueMysteryXmlRoot</summary>

    public class RogueMysteryXmlRoot
    {
        [XmlElement("Mystery")]
        public List<RogueMysteryXmlInfo> RogueMysteryList = new List<RogueMysteryXmlInfo>();
    }

    /// <summary>RogueMysteryXmlInfo</summary>

    public class RogueMysteryXmlInfo
    {
        public RogueMysteryXmlInfoFrame GetFrameById(int id)
        {
            return FrameList.Find(x => x.ID == id);
        }

        [XmlElement("Frame")]
        public List<RogueMysteryXmlInfoFrame> FrameList = new List<RogueMysteryXmlInfoFrame>();

        [XmlAttribute]
        public int ID = -1;

        [XmlElement]
        public string Title = "";
    }

    /// <summary>RogueMysteryXmlInfoFrame</summary>

    public class RogueMysteryXmlInfoFrame
    {
        public RogueMysteryXmlInfoFrameChoice GetChoiceById(int id)
        {
            return ChoiceList.Find(x => x.ID == id);
        }

        [XmlIgnore]
        public string Dialogs
        {
            get
            {
                string text = "";
                if (Dialog.Count > 0)
                {
                    foreach (var dia in Dialog)
                    {
                        text += dia.ReplaceColorShorthands() + Environment.NewLine;
                    }
                }
                return text;
            }
        }

        [XmlAttribute]
        public int ID = -1;

        [XmlAttribute]
        public FrameType FrameType = FrameType.DEFAULT;

        [XmlElement]
        public List<string> Dialog = new List<string>();

        [XmlElement("Choice")]
        public List<RogueMysteryXmlInfoFrameChoice> ChoiceList = new List<RogueMysteryXmlInfoFrameChoice>();
    }

    /// <summary>RogueMysteryXmlInfoFrameChoice</summary>

    public class RogueMysteryXmlInfoFrameChoice
    {
        [XmlAttribute]
        public int ID = -1;

        [XmlElement]
        public string Desc = "";
    }
    #endregion

    #region GAMEMODE MANAGEMENT

    public class RoguelikeGamemodeController : Singleton<RoguelikeGamemodeController>
    {
        public void AddGamemodesToList()
        {
            gamemodeList = new List<RoguelikeGamemodeBase>();
            Assembly[] assemblies = LogLikeMod.GetAssemList().Distinct().ToArray();
            for (int b = 0; b < assemblies.Length; b++)
            {
                try
                {
                    TypeInfo[] effects = assemblies[b].DefinedTypes.ToList().FindAll(x => x.IsSubclassOf(typeof(RoguelikeGamemodeBase))).ToArray();
                    for (int i = 0; i < effects.Length; i++)
                    {
                        if (effects[i] != null)
                            gamemodeList.Add(Activator.CreateInstance(effects[i].AsType()) as RoguelikeGamemodeBase);
                    }
                }
                catch
                { }
            }
            gamemodeList.Add(new RoguelikeGamemode_RMR_Modded_DebugCh2());
            gamemodeList.Add(new RoguelikeGamemode_RMR_Modded_DebugCh3());
            gamemodeList.Add(new RoguelikeGamemode_RMR_Modded_DebugCh4());
            gamemodeList.Add(new RoguelikeGamemode_RMR_Modded_DebugCh5());
            gamemodeList.Add(new RoguelikeGamemode_RMR_Modded_DebugCh6());
            gamemodeList.Add(new RoguelikeGamemode_RMR_Modded());
            gamemodeList.Add(new RoguelikeGamemode_RMR_Default());
        }

        // Handles save file creation, deletion and initialization
        public bool LoadGamemodeByStageRecipe(LorId stageRecipe, bool isContinue)
        {
            this.isContinue = isContinue;
            if (isContinue)
            {
                try
                {
                    SaveData latest = Singleton<LogueSaveManager>.Instance.LoadData("Lastest");
                    if (latest == null)
                    {
                        Debug.LogError("[RMR] Continue: Lastest save missing.");
                        return false;
                    }
                    string modeName = latest.GetString("CurrentGamemode");
                    if (string.IsNullOrEmpty(modeName))
                    {
                        Debug.LogError("[RMR] Continue: CurrentGamemode key missing in Lastest.");
                        return false;
                    }
                    return LoadGamemodeByName(modeName, true);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[RMR] Continue LoadGamemodeByStageRecipe failed: " + ex);
                    return false;
                }
            }
            var gamemode = gamemodeList.Find(x => stageRecipe == x.StageStart);
            if (gamemode == null) return false;
            RMRAbnormalityUnlockManager.ResetArchiveProgress();
            if (!File.Exists(Path.Combine(LogueSaveManager.Saveroot, gamemode.SaveDataString)))
            {
                SaveData data = new SaveData(SaveDataType.Dictionary);
                using (FileStream fileStream = File.Create(Path.Combine(LogueSaveManager.Saveroot, gamemode.SaveDataString)))
                {
                    new BinaryFormatter().Serialize(fileStream, data.GetSerializedData());
                }
            }
            else
            {
                LogueSaveManager.Instance.RemoveData(gamemode.SaveDataString);
            }
            RMRCore.CurrentGamemode = gamemode;
            return true;
        }

        // Handles save file creation, deletion and initialization
        public bool LoadGamemodeByName(string name, bool isContinue)
        {
            var gamemode = gamemodeList.Find(x => x.GetType().Name == name);
            if (gamemode == null)
            {
                Debug.Log("LoadGamemodeByName failed to load gamemode: " + name);
                return false;
            }
            else if (isContinue)
            {
                this.InitializeGamemode(gamemode);
                return true;
            }
            RMRAbnormalityUnlockManager.ResetArchiveProgress();
            if (!File.Exists(Path.Combine(LogueSaveManager.Saveroot, gamemode.SaveDataString)))
            {
                SaveData data = new SaveData(SaveDataType.Dictionary);
                using (FileStream fileStream = File.Create(Path.Combine(LogueSaveManager.Saveroot, gamemode.SaveDataString)))
                {
                    new BinaryFormatter().Serialize(fileStream, data.GetSerializedData());
                }
            }
            else
                LogueSaveManager.Instance.RemoveData(gamemode.SaveDataString);
            RMRCore.CurrentGamemode = gamemode;
            return true;
        }

        public void SaveCurrentGamemodeData()
        {
            Singleton<LogueSaveManager>.Instance.SaveData(RMRCore.CurrentGamemode.GetSaveData(), RMRCore.CurrentGamemode.SaveDataString);
        }

        public void SaveCurrentGamemodeName(SaveData data)
        {
            data.SetData("CurrentGamemode", new SaveData(RMRCore.CurrentGamemode.GetType().Name));
        }

        public void InitializeGamemode(RoguelikeGamemodeBase gamemode)
        {
            SaveData data = Singleton<LogueSaveManager>.Instance.LoadData(gamemode.SaveDataString);
            RMRCore.CurrentGamemode = gamemode;
            gamemode.LoadFromSaveData(data);
        }

        public bool isContinue = false;

        public List<RoguelikeGamemodeBase> gamemodeList = new List<RoguelikeGamemodeBase>();

    }

    /// <summary>
    /// Determines the origin of the content.
    /// </summary>
    public enum ContentScope
    {
        /// <summary>
        /// All content will be loaded.
        /// </summary>
        ALL,
        /// <summary>
        /// Only Loglike/RMR content will be loaded.
        /// </summary>
        ONLY_LOGLIKE,
        /// <summary>
        /// Only satellite mod content will be loaded.
        /// </summary>
        ONLY_MODDED,
        /// <summary>
        /// Only satellite mod content from a certain package id will be loaded.<br></br>
        /// PackageId must be specified by overriding <see cref="RoguelikeGamemodeBase.GetContentScopePackageId"/>.
        /// </summary>
        ONLY_PACKAGEID
    }

    /// <summary>RoguelikeGamemodeBase</summary>

    public class RoguelikeGamemodeBase
    {
        /// <summary>
        /// Controls how to filter content when initializing the gamemode.
        /// <br></br>Do not mess with this unless you know what you are doing.
        /// </summary>
        public virtual void FilterContent()
        {
            switch (this.GetMysteryScope())
            {
                case ContentScope.ALL:
                    break;
                case ContentScope.ONLY_LOGLIKE:
                    MysteryXmlList.Instance.info.RemoveAll(x => x.StageId.packageId != RMRCore.packageId);
                    break;
                case ContentScope.ONLY_MODDED:
                    MysteryXmlList.Instance.info.RemoveAll(x => x.StageId.packageId == RMRCore.packageId);
                    break;
                case ContentScope.ONLY_PACKAGEID:
                    MysteryXmlList.Instance.info.RemoveAll(x => x.StageId.packageId != this.GetContentScopePackageId);
                    break;
            }
            switch (this.GetStagesScope())
            {
                case ContentScope.ALL:
                    break;
                case ContentScope.ONLY_LOGLIKE:
                    foreach (var stage in StagesXmlList.Instance.infos)
                        stage.Stages.RemoveAll(x => x.Id.packageId != RMRCore.packageId);
                    break;
                case ContentScope.ONLY_MODDED:
                    foreach (var stage in StagesXmlList.Instance.infos)
                        stage.Stages.RemoveAll(x => x.Id.packageId == RMRCore.packageId);
                    break;
                case ContentScope.ONLY_PACKAGEID:
                    foreach (var stage in StagesXmlList.Instance.infos)
                        stage.Stages.RemoveAll(x => x.Id.packageId != this.GetContentScopePackageId);
                    break;
            }
            switch (this.GetRewardsScope())
            {
                case ContentScope.ALL:
                    break;
                case ContentScope.ONLY_LOGLIKE:
                    RewardPassivesList.Instance.infos.RemoveAll(x => x.Id.packageId != RMRCore.packageId);
                    break;
                case ContentScope.ONLY_MODDED:
                    RewardPassivesList.Instance.infos.RemoveAll(x => x.Id.packageId == RMRCore.packageId);
                    break;
                case ContentScope.ONLY_PACKAGEID:
                    RewardPassivesList.Instance.infos.RemoveAll(x => x.Id.packageId != this.GetContentScopePackageId);
                    break;
            }
        }

        /// <summary>
        /// This determines the name of the savefile containing gamemode-specific data.
        /// </summary>
        public virtual string SaveDataString => "RMR_GMSaveData_" + this.GetType().Name;

        /// <summary>
        /// The LorId of the reception used to start a new run in this gamemode.
        /// </summary>
        public virtual LorId StageStart { get; }

        /// <summary>
        /// Use this to <b>load</b> whatever persistent data from save data.
        /// </summary>
        public virtual void LoadFromSaveData(SaveData data)
        {
            
        }
        
        /// <summary>
        /// Use this to <b>save</b> whatever persistent data to the player's save data.
        /// </summary>
        public virtual SaveData GetSaveData()
        {
            SaveData saveData = new SaveData();
            saveData.AddData("GamemodeName", new SaveData(this.GetType().Name));
            return saveData;
        }

        /// <summary>
        /// Runs when initializing a roguelike reception, BEFORE creating the player.<br></br>
        /// Hooked up to <see cref="LogLikeHooks.UIInvitationRightMainPanel_ConfirmSendInvitation"/>.<br></br>
        /// Runs immediately <b>before</b> StageClassInfo.mapInfo.clear().
        /// </summary>
        public virtual void BeforeInitializeGamemode()
        {
            // done
        }

        /// <summary>
        /// Runs when initializing a roguelike reception, AFTER creating the player.<br></br>
        /// Hooked up to <see cref="LogLikeHooks.UIInvitationRightMainPanel_ConfirmSendInvitation"/>.<br></br>
        /// Runs immediately <b>prior</b> to all return calls.
        /// </summary>
        public virtual void AfterInitializeGamemode()
        {
            // done
        }

        /// <summary>
        /// Runs at the start of the Act of the first encounter.<br></br>
        /// Feel free to insert your initial event call here via <see cref="MysteryManager.StartMystery(LorId)"/>.
        /// </summary>
        public virtual void OnWaveStartInitialEvent()
        {
            // done
        }

        /// <summary>
        /// Determines the end-of-stage reward list for common fight encounters.
        /// </summary>
        public virtual List<RewardPassiveInfo> GetCurChapterCommonReward(ChapterGrade grade)
        {
            return Singleton<RewardPassivesList>.Instance.GetChapterData(grade, PassiveRewardListType.CommonReward, LorId.None);
        }

        /// <summary>
        /// Determines the end-of-stage reward list for elite fight encounters.
        /// </summary>
        public virtual List<RewardPassiveInfo> GetCurChapterEliteReward(ChapterGrade grade)
        {
            return Singleton<RewardPassivesList>.Instance.GetChapterData(grade, PassiveRewardListType.EliteReward, LorId.None);
        }

        /// <summary>
        /// Determines the end-of-stage reward list for boss fight encounters.
        /// </summary>
        public virtual List<RewardPassiveInfo> GetCurChapterBossReward(ChapterGrade grade)
        {
            return Singleton<RewardPassivesList>.Instance.GetChapterData(grade, PassiveRewardListType.BossReward, LorId.None);
        }

        /// <summary>
        /// Abnormality-page chapter pool for reception UI / legacy callers.
        /// Final realization exclusives are excluded until that floor is cleared.
        /// </summary>
        public virtual List<RewardPassiveInfo> GetCurChapterCreature(ChapterGrade grade)
        {
            List<RewardPassiveInfo> raw = Singleton<RewardPassivesList>.Instance.GetChapterData(grade, PassiveRewardListType.Creature, LorId.None)
                ?? new List<RewardPassiveInfo>();
            // Also merge Custom lists used by RMR abno rewards, then gate exclusives.
            List<RewardPassiveInfo> custom = Singleton<RewardPassivesList>.Instance.GetChapterData(grade, PassiveRewardListType.Custom, LorId.None)
                ?? new List<RewardPassiveInfo>();
            List<RewardPassiveInfo> gradeAllCustom = Singleton<RewardPassivesList>.Instance.GetChapterData(ChapterGrade.GradeAll, PassiveRewardListType.Custom, LorId.None)
                ?? new List<RewardPassiveInfo>();

            var merged = new List<RewardPassiveInfo>();
            void AddRange(List<RewardPassiveInfo> list)
            {
                if (list == null) return;
                foreach (RewardPassiveInfo info in list)
                {
                    if (info == null || info.rewardtype != RewardType.Creature)
                        continue;
                    if (!RMRAbnormalityUnlockManager.CanAppearInNormalReceptionAbnoPool(info))
                        continue;
                    // GradeAll exclusive rows must still respect current-chapter floor tier
                    // (e.g. Binah exclusives after clear do not flood Grade1–3 pools).
                    if (!RMRAbnormalityUnlockManager.IsRewardTierAvailableForChapter(
                            RMRAbnormalityUnlockManager.GetTierForRewardPage(info), grade))
                        continue;
                    if (merged.Exists(x => x != null && x.id == info.id))
                        continue;
                    merged.Add(info);
                }
            }
            AddRange(raw);
            AddRange(custom);
            AddRange(gradeAllCustom);
            return merged;
        }

        /// <summary>
        /// Initializes the encounter/stage list for each chapter when the run starts.<br></br>
        /// Defaults to <see cref="LogueBookModels.VanillaGamemodeReceptionList"/>.
        /// </summary>
        public virtual List<LogueStageInfo> InitializeChapterStageList(ChapterGrade chapter) => LogueBookModels.VanillaGamemodeReceptionList(chapter);

        /// <summary>
        /// Determines from which mods RMR should take Mystery Events from.
        /// </summary>
        public virtual ContentScope GetMysteryScope()
        {
            return ContentScope.ALL;
        }

        /// <summary>
        /// Determines from which mods RMR should take encounters from.
        /// </summary>
        public virtual ContentScope GetStagesScope()
        {
            return ContentScope.ALL;
        }

        /// <summary>
        /// Determines from which mods RMR should take rewards from.
        /// </summary>
        public virtual ContentScope GetRewardsScope()
        {
            return ContentScope.ALL;
        }

        /// <summary>
        /// Runs whenever the player beats a boss.<br></br>
        /// The base method does the following:<br></br>
        /// - Increment current chapter;<br></br>
        /// - Add a new librarian;<br></br>
        /// - Revives and heals all librarians;<br></br>
        /// - Resets the current stage counter;<br></br>
        /// - Gives the current (already incremented) chapter's CraftEffect;<br></br>
        /// - Displays the current (already incremented) chapter's splash screen.
        /// </summary>
        public virtual void OnClearBossWave()
        {
            ++LogLikeMod.curchaptergrade;
            LogLikeMod.AddPlayer = true;
            LogLikeMod.RecoverPlayers = true;
            LogLikeMod.curChStageStep = 0;
            switch (LogLikeMod.curchaptergrade)
            {
                case ChapterGrade.Grade2:
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter2());
                    break;
                case ChapterGrade.Grade3:
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter3());
                    break;
                case ChapterGrade.Grade4:
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter4());
                    break;
                case ChapterGrade.Grade5:
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter5());
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftExclusiveCardChapter5());
                    break;
                case ChapterGrade.Grade6:
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter6());
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftExclusiveCardChapter6());
                    RMRCore.GrantGrade6SpecialCorePagesIfNeeded();
                    break;
                case ChapterGrade.Grade7:
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter7());
                    Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftExclusiveCardChapter7());
                    break;
            }
            this.LoadCurrentChapterStory();
        }

        public virtual void LoadCurrentChapterStory()
        {
            LogStoryPathList.Instance.LoadStoryFile(new LorId(LogLikeMod.ModId, (int)LogLikeMod.curchaptergrade + 1), null, true);
        }

        /// <summary>
        /// This does NOT automatically control book drops- it is to be used strictly as a shorthand for getting the right book drops.<br></br>
        ///  Defaults to returning RMR's books. You are expected to put the right book drops on the correct unit XMLs.
        /// </summary>
        public virtual DropBookXmlInfo GetCommonEnemyDropBook(ChapterGrade chapter, Rarity rarity)
        {
            return Singleton<DropBookXmlList>.Instance.GetData(new LorId(LogLikeMod.ModId, ((int)chapter + 1) * 1000 + (int)rarity + 1));
        }

        /// <summary>
        /// The packageId to be used if any <see cref="ContentScope"/> methods are set to <see cref="ContentScope.ONLY_PACKAGEID"/>.<br></br>
        /// Defaults to <b>this type's</b> packageId. For further reference, see <see cref="RMRCore.ClassIds"/>.
        /// </summary>
        public virtual string GetContentScopePackageId => RMRCore.ClassIds[this.GetType().Assembly.FullName];

        /// <summary>
        /// Determines whether to override the base deck or not.<br></br>
        /// (Evade, Charge and Cover, Focused Strikes, etc.)<br></br>
        /// Defaults to <see langword="false"/>. The replacement deck is set by <see cref="BaseDeckReplacement"/>.
        /// </summary>
        public virtual bool ReplaceBaseDeck => false;

        /// <summary>
        /// If <see cref="ReplaceBaseDeck"/> is set to <see langword="true"/>, <br></br>
        /// this points to the <see cref="LorId"/> of the deck you are replacing the base deck with. <br></br>
        /// Defaults to RMR's upgraded deck (RMRCore.packageId, -10).
        /// </summary>
        public virtual LorId BaseDeckReplacement => new LorId(RMRCore.packageId, -10);

    }

    /// <summary>RoguelikeGamemode_RMR_Default</summary>

    public class RoguelikeGamemode_RMR_Default : RoguelikeGamemodeBase
    {
        public override LorId StageStart => new LorId(LogLikeMod.ModId, -854);

        public override ContentScope GetRewardsScope()
        {
            return ContentScope.ONLY_LOGLIKE;
        }

        public override ContentScope GetMysteryScope()
        {
            return ContentScope.ONLY_LOGLIKE;
        }

        public override ContentScope GetStagesScope()
        {
            return ContentScope.ONLY_LOGLIKE;
        }

        public override void AfterInitializeGamemode()
        {
            if (!RoguelikeGamemodeController.Instance.isContinue)
            {
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter1());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new PickUpModel_ShopGood46.SupriseBox());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_HiddenUpgradeChanceEffect());
                // Realization bootstrap must never play chapter intro (use bootstrap flag, not full combat suppress).
                if (!RMRCore.IsRealizationBootstrapPending())
                    this.LoadCurrentChapterStory();
                else
                    Debug.Log("[RMR] Skipped chapter intro story (realization bootstrap).");
            }
        }

        public override void OnWaveStartInitialEvent()
        {
            // Continue: resume saved route --no hub launch / no intro mystery.
            if (RoguelikeGamemodeController.Instance != null && RoguelikeGamemodeController.Instance.isContinue)
            {
                Debug.Log("[RMR] OnWaveStartInitialEvent skipped (continue run).");
                return;
            }
            RMRCore.HandlePostInvitationLaunch();
        }

        public override string GetContentScopePackageId => RMRCore.packageId;
    }

    /// <summary>RoguelikeGamemode_RMR_Modded_DebugCh2</summary>

    public class RoguelikeGamemode_RMR_Modded_DebugCh2 : RoguelikeGamemode_RMR_Modded
    {
        public override LorId StageStart => new LorId(LogLikeMod.ModId, -2854);
    }
    /// <summary>RoguelikeGamemode_RMR_Modded_DebugCh3</summary>
    public class RoguelikeGamemode_RMR_Modded_DebugCh3 : RoguelikeGamemode_RMR_Modded
    {
        public override LorId StageStart => new LorId(LogLikeMod.ModId, -3854);
    }
    /// <summary>RoguelikeGamemode_RMR_Modded_DebugCh4</summary>
    public class RoguelikeGamemode_RMR_Modded_DebugCh4 : RoguelikeGamemode_RMR_Modded
    {
        public override LorId StageStart => new LorId(LogLikeMod.ModId, -4854);
    }
    /// <summary>RoguelikeGamemode_RMR_Modded_DebugCh5</summary>
    public class RoguelikeGamemode_RMR_Modded_DebugCh5 : RoguelikeGamemode_RMR_Modded
    {
        public override LorId StageStart => new LorId(LogLikeMod.ModId, -5854);
    }
    /// <summary>RoguelikeGamemode_RMR_Modded_DebugCh6</summary>
    public class RoguelikeGamemode_RMR_Modded_DebugCh6 : RoguelikeGamemode_RMR_Modded
    {
        public override LorId StageStart => new LorId(LogLikeMod.ModId, -6854);

        public override void AfterInitializeGamemode()
        {
            if (RoguelikeGamemodeController.Instance.isContinue)
                return;
            LogLikeMod.curchaptergrade = ChapterGrade.Grade6;
            LogLikeMod.curChStageStep = 0;
            LogLikeMod.curstagetype = abcdcode_LOGLIKE_MOD.StageType.Start;
            LogLikeMod.curstageid = this.StageStart;
            if (!RMRCore.IsRealizationBootstrapPending())
                this.LoadCurrentChapterStory();
            LogLikeMod.ResetNextStage();
            RMRCore.GrantGrade6SpecialCorePagesIfNeeded();
        }

        public override void OnWaveStartInitialEvent()
        {
            if (RoguelikeGamemodeController.Instance != null && RoguelikeGamemodeController.Instance.isContinue)
            {
                Debug.Log("[RMR] DebugCh6 OnWaveStartInitialEvent skipped (continue run).");
                return;
            }
            if (RMRCore.IsRealizationBootstrapPending())
            {
                RMRCore.HandlePostInvitationLaunch();
                return;
            }
            RMRCore.RMRMapHandler.StartActAsCustomMap<SparklingMirrorMapManager>("SparklingMirrorMapManager");
            LogLikeMod.nextlist = LogueBookModels.GetNextList(ChapterGrade.Grade6, true);
            Singleton<StageController>.Instance.GetStageModel().GetWave(Singleton<StageController>.Instance.CurrentWave).Defeat();
            Singleton<StageController>.Instance.EndBattle();
        }
    }

    /// <summary>RoguelikeGamemode_RMR_Modded</summary>

    public class RoguelikeGamemode_RMR_Modded : RoguelikeGamemodeBase
    {
        public override LorId StageStart => new LorId(LogLikeMod.ModId, -853);

        public override void AfterInitializeGamemode()
        {
            if (!RoguelikeGamemodeController.Instance.isContinue)
            {
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter1());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new PickUpModel_ShopGood46.SupriseBox());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_HiddenUpgradeChanceEffect());
                // Realization bootstrap: no chapter intro story.
                if (!RMRCore.IsRealizationBootstrapPending())
                    this.LoadCurrentChapterStory();
                else
                    Debug.Log("[RMR] Skipped chapter intro story (realization bootstrap).");
            }
        }

        public override void OnWaveStartInitialEvent()
        {
            // Continue: pure resume --no SparklingMirror shell, no hub launch handler.
            if (RoguelikeGamemodeController.Instance != null && RoguelikeGamemodeController.Instance.isContinue)
            {
                Debug.Log("[RMR] OnWaveStartInitialEvent skipped (continue run).");
                return;
            }
            // Realization bootstrap: floor pick / direct stage --do not start SparklingMirror dummy map.
            // Note: still boots via stage -853 shell (engine needs a reception); shell combat is blocked
            // while AwaitingRealizationFloorPick / CheckStage suppressed during ReceptionActive.
            if (RMRCore.IsRealizationBootstrapPending())
            {
                RMRCore.HandlePostInvitationLaunch();
                return;
            }
            RMRCore.RMRMapHandler.StartActAsCustomMap<SparklingMirrorMapManager>("SparklingMirrorMapManager");
            RMRCore.HandlePostInvitationLaunch();
        }

        public override bool ReplaceBaseDeck => true;
        public override LorId BaseDeckReplacement => new LorId(RMRCore.packageId, -10);
        public override string GetContentScopePackageId => RMRCore.packageId;
    }

        #endregion

    #endregion


    #region UI INFRASTRUCTURE

    public class GlobalLogueItemCatalogPanel : Singleton<GlobalLogueItemCatalogPanel>
    {
        public bool IsItemShown(GlobalLogueEffectBase item)
        {
            Type type = item.GetType();
            if (type.GetCustomAttribute<HideFromItemCatalog>(false) != null)
                return false;
            else if (item.GetType().GetCustomAttribute<SecretInItemCatalog>(false) != null)
                return item.HasBeenObtained();
            return true;
        }

        public bool IsItemShown(PickUpModelBase item)
        {
            Type type = item.GetType();
            if (type.GetCustomAttribute<HideFromItemCatalog>(false) != null)
                return false;
            else if (type.GetCustomAttribute<SecretInItemCatalog>(false) != null)
                return item.HasBeenObtained();
            return true;
        }

        public void GetLogUIObj()
        {
            UIStoryArchivesPanel goG = UI.UIController.Instance.GetUIPanel(UIPanelType.Story) as UIStoryArchivesPanel;
            UICustomTabsController tabs = RMRCore.GetStoryArchivesTabController(goG);
            if (goG == null || tabs == null || tabs.CustomTabs == null || tabs.CustomTabs.Length < 3)
            {
                Debug.LogError("[RMR ItemCatalog] UIStoryArchivesPanel/tabcontroller unavailable --cannot create catalog tab.");
                return;
            }
            UICustomTabButton gameObject2 = UnityEngine.Object.Instantiate(tabs.CustomTabs[2], tabs.TabsRoot.transform);
            UIBookStoryPanel gameObject3 = UnityEngine.Object.Instantiate(goG.bookStoryPanel, goG.ActiveControl.transform);
            tabs.CustomTabs = tabs.CustomTabs.Append(gameObject2).ToArray();
            gameObject2.Init(4, tabs);
            LayoutRebuilder.MarkLayoutForRebuild(tabs.GetComponentInChildren<HorizontalLayoutGroup>().transform as RectTransform);
            gameObject2.gameObject.SetActive(true);
            button = gameObject2;
            button.GetComponentInChildren<UITextDataLoader>().key = "ui_RMR_ItemCatalog";
            gameObject3.Initialize();
            gameObject3.gameObject.SetActive(true);
            gameObject3.transform.SetAsFirstSibling();
            root = gameObject3;


            Image image = ModdingUtils.CreateImage(this.root.transform, "RogueLikeRebornIconAlt", new Vector2(1.2f, 1.2f), new Vector2(0f, 0f));
            image.transform.localPosition = new Vector3(525f, -25f, 0f);
            image.color = new Color(image.color.r, image.color.g, image.color.b, image.color.a / 5);
            image.transform.SetSiblingIndex(1);

            this.pageText = ModdingUtils.CreateText_TMP(this.root.transform, new Vector2(-1285f, 250f), 64, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), TextAlignmentOptions.BottomRight, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            this.pageText.gameObject.SetActive(true);

            Image image3 = ModdingUtils.CreateImage(this.root.transform, "ItemCatalogForwardButton", new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(65f, 65f));
            this.pageClickForward = image3.gameObject.AddComponent<UIPageSwitchButton>();
            this.pageClickForward.Init(true);
            this.pageClickForward.transform.localPosition = new Vector3(-265f, -245f);
            this.pageClickForward.gameObject.SetActive(true);

            Image image4 = ModdingUtils.CreateImage(this.root.transform, "ItemCatalogBackButton", new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(65f, 65f));
            this.pageClickBackwards = image4.gameObject.AddComponent<UIPageSwitchButton>();
            this.pageClickBackwards.Init(false);
            this.pageClickBackwards.transform.localPosition = new Vector3(-515f, -245f);
            this.pageClickBackwards.gameObject.SetActive(true);
            for (int i = 0; i < rowCount; i++) // 6 rows
            {
                for (int j = 0; j < columnCount; j++) // 9 entries each
                {
                    Image image2 = ModdingUtils.CreateImage(this.root.transform, (Sprite)null, new Vector2(1f, 1f), new Vector2((float)(-785 + j * 100), (float)(350 - i * 100)), new Vector2(85f, 85f));
                    LogueEffectImage_ItemCatalog item = image2.gameObject.AddComponent<LogueEffectImage_ItemCatalog>();
                    this.sprites.Add(item);
                    item.gameObject.SetActive(true);
                }
            }
        }

        public void Activate()
        {
            this.activated = true;
            LogLikeMod.itemCatalogActive = true;
            root.canvasGroup.alpha = 1f;
            root.canvasGroup.interactable = true;
            root.canvasGroup.blocksRaycasts = true;
            this.UpdateSprites();
        }

        public void Deactivate()
        {
            this.activated = false;
            LogLikeMod.itemCatalogActive = false;
            root.canvasGroup.alpha = 0f;
            root.canvasGroup.interactable = false;
            root.canvasGroup.blocksRaycasts = false;
            SingletonBehavior<UIMainOverlayManager>.Instance.Close();
        }

        public void Init()
        {
            
            this.items = new List<object>();
            List<GlobalLogueEffectBase> gamer = new List<GlobalLogueEffectBase>();
            List<PickUpModelBase> gaming = new List<PickUpModelBase>();
            Assembly[] assemblies = LogLikeMod.GetAssemList().Distinct().ToArray();
            for (int b = 0; b < assemblies.Length; b++)
            {  
                TypeInfo[] effects = assemblies[b].DefinedTypes.ToList().FindAll(x => x.IsSubclassOf(typeof(GlobalLogueEffectBase))).ToArray();
                for (int i = 0; i < effects.Length; i++)
                {
                    try
                    {
                        if (effects[i] != null && effects[i].GetCustomAttribute(typeof(HideFromItemCatalog), false) == null)
                            gamer.Add(Activator.CreateInstance(effects[i].AsType()) as GlobalLogueEffectBase);
                    } catch (Exception e)
                    {
                        Debug.Log("Failed to instantiate an effect: " + e);
                    }
                }
                
                TypeInfo[] pickups = assemblies[b].DefinedTypes.ToList().FindAll(x => (x.IsSubclassOf(typeof(PickUpModelBase)) || x.IsSubclassOf(typeof(ShopPickUpModel))) && !x.IsSubclassOf(typeof(CreaturePickUpModel)) && !x.IsSubclassOf(typeof(RestPickUp))).ToArray();
                for (int i = 0; i < pickups.Length; i++)
                {
                    try { 
                    if (pickups[i] != null && pickups[i].GetCustomAttribute(typeof(HideFromItemCatalog), false) == null)
                        gaming.Add(Activator.CreateInstance(pickups[i].AsType()) as PickUpModelBase);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Failed to instantiate a pickup: " + e);
                    }
                }
            }

            foreach (var eff in gamer.FindAll(x => x != null && x.GetSprite() != null && IsItemShown(x)))
            {
                eff.Log("EFFECT");
                items.Add(eff);
            }
            foreach (var pick in gaming.FindAll(x => x != null && x.GetSprite() != null && IsItemShown(x)))
            {
                pick.Log("PICKUP");
                items.Add(pick);
            }

        }

        public void UpdateSprites()
        {
            foreach (LogueEffectImage_ItemCatalog logueEffectImage_Inventory in this.sprites)
            {
                if (logueEffectImage_Inventory != null)
                    logueEffectImage_Inventory.gameObject.SetActive(false);
            }
            this.pageText.text = string.Concat(currentPage + 1, " / ", pageCount + 1);
            int index = 0;
            
            for (int i = currentPage * SpriteCount; i < items.Count; i++)
            {
                this.sprites[index].Init(items[i]);
                index++;
                if (index >= SpriteCount)
                    break;
            }

            
        }

        public bool debugMode = false;

        public bool activated = false;

        public int currentPage = 0;

        public int pageCount {
            get
            {
                return items.Count % SpriteCount == 0 ? items.Count / SpriteCount - 1 : items.Count / SpriteCount;
            }
        }

        public int rowCount = 6;

        public int columnCount = 9;

        public int SpriteCount => rowCount * columnCount;

        public UIPageSwitchButton pageClickBackwards;

        public UIPageSwitchButton pageClickForward;

        public TextMeshProUGUI pageText;

        public UICustomTabButton button;

        public UIBookStoryPanel root;

        public List<object> items;

        public List<LogueEffectImage_ItemCatalog> sprites = new List<LogueEffectImage_ItemCatalog>();
    }

    /// <summary>UIPageSwitchButton</summary>

    public class UIPageSwitchButton : MonoBehaviour
    {
        public void Init(bool forward)
        {
            this.forward = forward;
            this.image = gameObject.GetComponent<Image>();
            if (this.selectable == null)
            {
                this.selectable = base.gameObject.AddComponent<UILogCustomSelectable>();
                this.selectable.targetGraphic = this.image;


                this.selectable.SelectEvent = new UnityEventBasedata();
                this.selectable.SelectEvent.AddListener(delegate (BaseEventData e)
                {
                    this.OnEnterImage();
                });
                this.selectable.onClick = new Button.ButtonClickedEvent();
                this.selectable.onClick.AddListener(delegate
                {
                    this.OnPointerClick();
                });
                this.selectable.DeselectEvent = new UnityEventBasedata();
                this.selectable.DeselectEvent.AddListener(delegate (BaseEventData e)
                {
                    this.OnExitImage();
                });
            }
            this.image.color = UIColorManager.Manager.GetUIColor(UIColor.Default);
            this.gameObject.SetActive(true);
        }

        public void OnPointerClick()
        {
            int curPage = Singleton<GlobalLogueItemCatalogPanel>.Instance.currentPage;
            if (forward)
                Singleton<GlobalLogueItemCatalogPanel>.Instance.currentPage = Math.Min(curPage + 1, Singleton<GlobalLogueItemCatalogPanel>.Instance.pageCount);
            else
                Singleton<GlobalLogueItemCatalogPanel>.Instance.currentPage = Math.Max(curPage - 1, 0);
            if (curPage != Singleton<GlobalLogueItemCatalogPanel>.Instance.currentPage)
            {
                Singleton<GlobalLogueItemCatalogPanel>.Instance.root.SetItemRightPanel(null);
                Singleton<GlobalLogueItemCatalogPanel>.Instance.UpdateSprites();
            }
        }

        public void OnEnterImage()
        {
            this.image.color = UIColorManager.Manager.GetUIColor(UIColor.Highlighted);
        }

        public void OnExitImage()
        {
            this.image.color = UIColorManager.Manager.GetUIColor(UIColor.Default);
        }

        public bool forward;

        public UILogCustomSelectable selectable;

        public Image image;
    }

    /// <summary>LOGLIKE type: LogueEffectImage_ItemCatalog</summary>

    public class LogueEffectImage_ItemCatalog : MonoBehaviour
    {
        public void Init(object item)
        {
            this.item = item;
            if (isEffect)
                this.Init(Effect);
            else
                this.Init(Pickup);
        }

        public void Init(GlobalLogueEffectBase effect)
        {
            if (effect == null)
            {
                base.gameObject.SetActive(false);
            }
            else
            {
                this.item = effect;
                this.image = base.gameObject.GetComponent<Image>();
                this.image.sprite = LogLikeMod.ArtWorks["ItemCatalogRounded"];
                this.image.color = UIColorManager.Manager.GetUIColor(UIColor.Default);
                if (effect.GetSprite() == null)
                {
                    this.Log("effect is null");
                    base.gameObject.SetActive(false);
                }
                else
                {
                    if (this.baseimage == null)
                    {
                        this.baseimage = ModdingUtils.CreateImage(base.transform, "EmptyIcon", new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(70f, 70f));
                    }
                    this.sprite = effect.GetSprite();
                    this.baseimage.sprite = this.sprite;
                    this.baseimage.color = this.isObtained ? new Color(1f, 1f, 1f) : Color.black;
                    if (this.selectable == null)
                    {
                        this.selectable = base.gameObject.AddComponent<UILogCustomSelectable>();
                        this.selectable.targetGraphic = this.image;
                        this.selectable.SelectEvent = new UnityEventBasedata();
                        this.selectable.SelectEvent.AddListener(delegate (BaseEventData e)
                        {
                            this.OnEnterImage();
                        });
                        this.selectable.onClick = new Button.ButtonClickedEvent();
                        this.selectable.onClick.AddListener(delegate
                        {
                            this.OnPointerClick();
                        });
                        this.selectable.DeselectEvent = new UnityEventBasedata();
                        this.selectable.DeselectEvent.AddListener(delegate (BaseEventData e)
                        {
                            this.OnExitImage();
                        });
                    }
                    this.gameObject.SetActive(true);
                    this.update = false;
                    if (SingletonBehavior<UIMainOverlayManager>.Instance != null)
                    {
                        SingletonBehavior<UIMainOverlayManager>.Instance.Close();
                    }
                }
            }
        }

        public void Init(PickUpModelBase pickup)
        {
            if (pickup == null)
            {
                base.gameObject.SetActive(false);
            }
            else
            {
                this.item = pickup;
                this.image = base.gameObject.GetComponent<Image>();
                this.image.sprite = LogLikeMod.ArtWorks["ItemCatalogRounded"];
                this.image.color = UIColorManager.Manager.GetUIColor(UIColor.Default);
                Sprite sproite = pickup.GetSprite();
                
                if (sproite == null)
                {
                    this.Log("effect is null");
                    base.gameObject.SetActive(false);
                }
                else
                {
                    if (this.baseimage == null)
                    {
                        this.baseimage = ModdingUtils.CreateImage(base.transform, "EmptyIcon", new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(70f, 70f));
                    }
                    this.sprite = sproite;
                    this.baseimage.sprite = this.sprite;
                    this.baseimage.color = this.isObtained ? new Color(1f, 1f, 1f) : Color.black;
                    if (this.selectable == null)
                    {
                        this.selectable = base.gameObject.AddComponent<UILogCustomSelectable>();
                        this.selectable.targetGraphic = this.image;
                        this.selectable.SelectEvent = new UnityEventBasedata();
                        this.selectable.SelectEvent.AddListener(delegate (BaseEventData e)
                        {
                            this.OnEnterImage();
                        });
                        this.selectable.onClick = new Button.ButtonClickedEvent();
                        this.selectable.onClick.AddListener(delegate
                        {
                            this.OnPointerClick();
                        });
                        this.selectable.DeselectEvent = new UnityEventBasedata();
                        this.selectable.DeselectEvent.AddListener(delegate (BaseEventData e)
                        {
                            this.OnExitImage();
                        });
                    }
                    this.gameObject.SetActive(true);
                    this.update = false;
                    if (SingletonBehavior<UIMainOverlayManager>.Instance != null)
                    {
                        SingletonBehavior<UIMainOverlayManager>.Instance.Close();
                    }
                }
            }
        }

        public void OnPointerClick()
        {
            Singleton<GlobalLogueItemCatalogPanel>.Instance.root.SetItemRightPanel(this);
        }

        public void OnEnterImage()
        {
            this.image.color = UIColorManager.Manager.GetUIColor(UIColor.Highlighted);
            this.update = true;
            if (this.isEffect)
            {
                if (!string.IsNullOrEmpty(this.Effect.GetEffectDesc()))
                {
                    SingletonBehavior<UIMainOverlayManager>.Instance.SetTooltip(
                        this.isObtained ? this.Effect.GetEffectName() : TextDataModel.GetText("ui_RMR_ItemNotObtained_Name"),
                        this.isObtained ? this.Effect.GetEffectDesc() + "\n" 
                        + "<color=#" + ColorUtility.ToHtmlStringRGB(UIColorManager.Manager.GetEquipRarityColor(this.Effect.GetRarity())) +">" + this.Effect.GetRarity().ToString() + "</color>" + "\n\n" 
                        + TextDataModel.GetText("ui_RMR_ItemObtainCount", this.Effect.GetItemObtainCount()) : TextDataModel.GetText("ui_RMR_ItemNotObtained_Desc"),
                        base.transform as RectTransform,
                        this.Effect.GetRarity(),
                        UIToolTipPanelType.OnlyContent
                    );
                    
                }
            } else
            {
                if (!string.IsNullOrEmpty(this.Pickup.Desc))
                {
                    SingletonBehavior<UIMainOverlayManager>.Instance.SetTooltip(
                        this.isObtained ? this.Pickup.Name : TextDataModel.GetText("ui_RMR_ItemNotObtained_Name"),
                        this.isObtained ? this.Pickup.Desc + "\n" + "\n" + this.Pickup.FlaverText + "\n"
                        + "<color=#" + ColorUtility.ToHtmlStringRGB(UIColorManager.Manager.GetEquipRarityColor(this.Pickup.GetRarity())) + ">" + this.Pickup.GetRarity().ToString() + "</color>" + "\n\n"
                        + TextDataModel.GetText("ui_RMR_ItemObtainCount", this.Pickup.GetItemObtainCount()) : TextDataModel.GetText("ui_RMR_ItemNotObtained_Desc"),
                        base.transform as RectTransform,
                        this.Pickup.GetRarity(),
                        UIToolTipPanelType.OnlyContent
                    );
                }
            }
            
        }

        public void OnExitImage()
        {
            SingletonBehavior<UIMainOverlayManager>.Instance.Close();
            this.image.color = UIColorManager.Manager.GetUIColor(UIColor.Default);
            this.update = false;
        }

        public bool update;

        public PickUpModelBase Pickup
        {
            get
            {
                if (!isEffect)
                    return (PickUpModelBase)item;
                else
                    return null;
            }
        }

        public GlobalLogueEffectBase Effect
        {
            get { 
                if (isEffect) 
                    return (GlobalLogueEffectBase) item;
                else 
                    return null;
            }
        }

        public bool isEffect
        {
            get
            {
                return item is GlobalLogueEffectBase || item.GetType().IsSubclassOf(typeof(GlobalLogueEffectBase));
            }
        }

        public bool isObtained
        {
            get
            {
                if (Singleton<GlobalLogueItemCatalogPanel>.Instance.debugMode)
                    return true;
                if (isEffect)
                {
                    return Effect.HasBeenObtained();
                } else return Pickup.HasBeenObtained();
            }
        }

        public object item;

        public UILogCustomSelectable selectable;

        public Sprite sprite;

        public Image image;

        public Image baseimage;
    }
    
    /// <summary>UIItemCatalogPanel</summary>
    
    public static class UIItemCatalogPanel
    {
        public static Sprite GetSprite(this PickUpModelBase item)
        {
            try
            {
                if (RMRCore.ClassIds[item.GetType().Assembly.FullName] == RMRCore.packageId)
                {
                    return LogLikeMod.ArtWorks[item.ArtWork]; 
                } 
                else if (string.IsNullOrEmpty(item.ArtWork) && item.id != null)
                {
                    return RewardPassivesList.Instance.GetPassiveInfo(item.id).Artwork;
                }
                else return LogLikeMod.ModdedArtWorks[(RMRCore.ClassIds[item.GetType().Assembly.FullName], item.ArtWork)];
            }
            catch 
            { 
            }
            return null;
        }

        public static Rarity GetRarity(this PickUpModelBase item)
        {
            try
            {
                if (item is ShopPickUpModel && ((ShopPickUpModel)item).basepassive != null)
                    return ((ShopPickUpModel)item).basepassive.rare;
                else if (item.rewardinfo == null)
                    return Rarity.Special;
                return item.rewardinfo.passiverarity;
            }
            catch
            {
                return Rarity.Special;
            }
        }

        public static Rarity GetRarity(this GlobalLogueEffectBase item)
        {
            if (item.GetType().GetField("ItemRarity", BindingFlags.Static | BindingFlags.Public) is var rare && rare != null)
                return (Rarity)rare.GetValue(null);
            return Rarity.Special;
        }

        public static string GetItemKeywordId(this GlobalLogueEffectBase item)
        {
            return string.IsNullOrEmpty(item.KeywordId) ? "NO_KEYWORD_GIVEN" : item.KeywordId;
        }
        public static string GetItemKeywordId(this PickUpModelBase item)
        {
            return string.IsNullOrEmpty(item.KeywordId) ? "NO_KEYWORD_GIVEN" : item.KeywordId;
        }

        public static int GetItemObtainCount(this GlobalLogueEffectBase item)
        {
            try { 
                int count = Singleton<LogueSaveManager>.Instance.LoadData("RMR_ItemCatalog").GetInt("ObtainCount_" + item.GetType().Name);
                return count;
            } catch
            { }
            return 0;
        }
        public static int GetItemObtainCount(this PickUpModelBase item)
        {
            try
            {
                int count = Singleton<LogueSaveManager>.Instance.LoadData("RMR_ItemCatalog").GetInt("ObtainCount_" + item.GetType().Name);
                return count;
            }
            catch
            { }
            return 0;
        }

        public static bool HasBeenObtained(this GlobalLogueEffectBase item)
        {
            try
            {
                bool obtain = Singleton<LogueSaveManager>.Instance.LoadData("RMR_ItemCatalog").GetInt("ObtainCount_" + item.GetType().Name) > 0;
                return obtain;
            } catch
            { }
            return false;
        }
        public static bool HasBeenObtained(this PickUpModelBase item)
        {
            try
            {
                bool obtain = Singleton<LogueSaveManager>.Instance.LoadData("RMR_ItemCatalog").GetInt("ObtainCount_" + item.GetType().Name) > 0;
                return obtain;
            }
            catch
            { }
            return false;
        }

        public static void SetItemRightPanel(this UIBookStoryPanel panel, LogueEffectImage_ItemCatalog item)
        {
            if (item == null)
            {
                panel.equipPageName.text = "";
                panel.equipPageStory.text = "";
                panel.portrait.enabled = false;
                panel.SelectablePanel_Text.ChildSelectable.interactable = false;
                return;
            }

            if (item.isEffect)
            {
                if (Singleton<GlobalLogueItemCatalogPanel>.Instance.debugMode)
                {
                    panel.equipPageStory.text = "--- DEBUGGING INFO ---\n" +
                        "Class name: " + item.Effect.GetType().Name + "\n" +
                        "Full class path: " + item.Effect.GetType().FullName + "\n" +
                        "KeywordId: " + item.Effect.GetItemKeywordId() + "\n" +
                        "Assembly.FullName: " + item.Effect.GetType().Assembly.FullName + "\n" +
                        "PackageId in assembly: " + RMRCore.ClassIds[item.Effect.GetType().Assembly.FullName] +
                        "\n--- DEBUGGING INFO ---\n\n" +
                        item.Effect.GetCredenzaEntry();
                }
                else panel.equipPageStory.text = item.isObtained ? item.Effect.GetCredenzaEntry() : TextDataModel.GetText("ui_RMR_ItemNotObtained_Credenza");
                panel.equipPageName.text = item.isObtained ? item.Effect.GetEffectName() : TextDataModel.GetText("ui_RMR_ItemNotObtained_Name");
            }
            else
            {
                if (Singleton<GlobalLogueItemCatalogPanel>.Instance.debugMode)
                {
                    panel.equipPageStory.text = "--- DEBUGGING INFO ---\n" +
                        "Class name: " + item.Pickup.GetType().Name + "\n" +
                        "Full class path: " + item.Pickup.GetType().FullName + "\n" +
                        "KeywordId: " + item.Pickup.GetItemKeywordId() + "\n" +
                        "Assembly.FullName: " + item.Pickup.GetType().Assembly.FullName + "\n" +
                        "PackageId in assembly: " + RMRCore.ClassIds[item.Pickup.GetType().Assembly.FullName] +
                        "\n--- DEBUGGING INFO ---\n\n" +
                        item.Pickup.GetCredenzaEntry();
                }
                else panel.equipPageStory.text = item.isObtained ? item.Pickup.GetCredenzaEntry() : TextDataModel.GetText("ui_RMR_ItemNotObtained_Credenza");
                panel.equipPageName.text = item.isObtained ? item.Pickup.Name : TextDataModel.GetText("ui_RMR_ItemNotObtained_Name");
            }
            panel.portrait.sprite = item.isObtained ? item.sprite : LogLikeMod.ArtWorks["ItemNotFoundIcon"];
            panel.portrait.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.equipPageStory.GetComponent<RectTransform>());
            panel.scrollbar.scrollbar.Select();
            panel.scrollbar.scrollbar.value = 1f;
        }

        public static void Initialize(this UIBookStoryPanel panel)
        {
            panel.SetData();
            panel.equipPageName.text = "";
            panel.equipPageStory.text = "";
            if (panel.portrait.GetComponentInParent<Mask>() is var mask && mask != null)
            {
                mask.gameObject.transform.localPosition = new Vector3(170f, 370f, 0f);
                mask.gameObject.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            }
            
            panel.portrait.transform.localPosition = new Vector3(0f, 0f, 0f);
            panel.portrait.transform.Rotate(0f, 0f, 0f);
            panel.portrait.transform.localScale = new Vector3(0.5f, 0.25f, 1f);
            panel.portrait.enabled = false;
            panel.SelectablePanel_Text.ChildSelectable.interactable = false;

            UnityEngine.Object.Destroy(panel.bookListDownButton.gameObject);
            UnityEngine.Object.Destroy(panel.bookListUpButton.gameObject);
            UnityEngine.Object.Destroy(panel.BookListLayout.gameObject);
            for (int i = 0; i < panel.bookSlots.Count; i++)
                if (panel.bookSlots[i] != null)
                    UnityEngine.Object.Destroy(panel.bookSlots[i].gameObject);
            for (int i = 0; i < panel.bookStoryChapterSlots.Count; i++)
                if (panel.bookStoryChapterSlots[i] != null)
                    UnityEngine.Object.Destroy(panel.bookStoryChapterSlots[i].gameObject);
            for (int i = 0; i < panel.downSelectableObjects.Count; i++)
                if (panel.downSelectableObjects[i] != null)
                    UnityEngine.Object.Destroy(panel.downSelectableObjects[i]);
            UnityEngine.Object.Destroy(panel.episodeListScroll.gameObject);
            for (int i = 0; i < panel.upSelectableObjects.Count; i++)
                if (panel.upSelectableObjects[i] != null)
                    UnityEngine.Object.Destroy(panel.upSelectableObjects[i]);
            UnityEngine.Object.Destroy(panel.equipPageSlot.rect);
            UnityEngine.Object.Destroy(panel.equipPageSlot.gameObject);
            UnityEngine.Object.Destroy(panel.SelectablePanel_books.gameObject);
            UnityEngine.Object.Destroy(panel.SelectablePanel_epis.gameObject);
            UnityEngine.Object.Destroy(panel.slotsLayoutGroup.gameObject);
            UnityEngine.Object.Destroy(panel.selectedEpisodeIcon.gameObject);
            UnityEngine.Object.Destroy(panel.selectedEpisodeIconGlow.gameObject);
            UnityEngine.Object.Destroy(panel.selectedEpisodeText.gameObject);
            UnityEngine.Object.Destroy(panel.selectedEpisodeTitleRect.gameObject);
            UnityEngine.Object.Destroy(panel.nullEpisodeRect);
            UnityEngine.Object.Destroy(panel.slotsLayoutGroup.gameObject);
        }

        public static void SetTooltip(this UIMainOverlayManager __instance, string name, string content, RectTransform rectTransform, Rarity rare = (Rarity)69, UIToolTipPanelType panelType = UIToolTipPanelType.OnlyContent)
        {
            try
            {
                // Open() may be non-public under some IL --invoke safely.
                try
                {
                    var open = AccessTools.Method(typeof(UIMainOverlayManager), "Open");
                    if (open != null)
                        open.Invoke(__instance, null);
                    else
                        __instance.Open();
                }
                catch
                {
                    try { __instance.Open(); } catch { return; }
                }

                // All UIMainOverlayManager members via reflection --MonoMod Method/FieldAccess otherwise.
                object tooltipNameObj = AccessTools.Field(typeof(UIMainOverlayManager), "tooltipName")?.GetValue(__instance);
                object tooltipDescObj = AccessTools.Field(typeof(UIMainOverlayManager), "tooltipDesc")?.GetValue(__instance);
                if (tooltipNameObj == null || tooltipDescObj == null)
                    return;

                SetOverlayTextProp(tooltipNameObj, "text", name);
                try
                {
                    var rtProp = tooltipNameObj.GetType().GetProperty("rectTransform", AccessTools.all);
                    if (rtProp?.GetValue(tooltipNameObj, null) is RectTransform nameRt)
                        nameRt.sizeDelta = new Vector2(nameRt.sizeDelta.x, 20f);
                }
                catch { }

                Camera camera = null;
                if (rectTransform != null)
                {
                    Graphic componentInChildren = rectTransform.GetComponentInChildren<Graphic>();
                    if (componentInChildren != null && componentInChildren.canvas != null
                        && componentInChildren.canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        camera = Camera.main;
                    }
                }
                Color toolColor = Color.white;
                if (rare >= Rarity.Common && rare <= Rarity.Unique)
                    toolColor = UIColorManager.Manager.GetEquipRarityColor(rare);
                else if (rare == Rarity.Special)
                    toolColor = UIColorManager.Manager.Error;
                SetOverlayTextProp(tooltipNameObj, "color", toolColor);

                try
                {
                    var setter = AccessTools.Field(typeof(UIMainOverlayManager), "setter_tooltipname")?.GetValue(__instance);
                    if (setter != null)
                    {
                        var underProp = AccessTools.Property(setter.GetType(), "underlayColor");
                        if (underProp != null && underProp.CanWrite)
                            underProp.SetValue(setter, toolColor, null);
                        else
                            AccessTools.Field(setter.GetType(), "underlayColor")?.SetValue(setter, toolColor);
                    }
                }
                catch { }

                SetOverlayTextProp(tooltipDescObj, "text", content);

                // SetTooltipOverlayBoxSize is MethodAccess-restricted from mods.
                try
                {
                    var sizeMethod = AccessTools.Method(typeof(UIMainOverlayManager), "SetTooltipOverlayBoxSize", new[] { typeof(UIToolTipPanelType) });
                    if (sizeMethod != null)
                        sizeMethod.Invoke(__instance, new object[] { panelType });
                    else
                        __instance.SetTooltipOverlayBoxSize(panelType);
                }
                catch (System.Exception sizeEx)
                {
                    UnityEngine.Debug.LogWarning("[RMR] SetTooltipOverlayBoxSize via reflection failed: " + sizeEx.Message);
                }

                if (!LogLikeMod.IsBattleState())
                    __instance.SetFixedTooltipOverlayBoxPosition(camera, rectTransform);
                else
                    __instance.SetMouseTooltipOverlayBoxPosition();

                try
                {
                    var setterObj = AccessTools.Field(typeof(UIMainOverlayManager), "setter_tooltipname")?.GetValue(__instance);
                    if (setterObj is Behaviour b)
                    {
                        b.enabled = false;
                        b.enabled = true;
                    }
                }
                catch { }

                try
                {
                    var enProp = tooltipNameObj.GetType().GetProperty("enabled", AccessTools.all);
                    if (enProp != null && enProp.CanWrite)
                    {
                        enProp.SetValue(tooltipNameObj, false, null);
                        enProp.SetValue(tooltipNameObj, true, null);
                    }
                }
                catch { }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[RMR] SetTooltip failed (non-fatal): " + ex.Message);
            }
        }

        private static void SetOverlayTextProp(object target, string propName, object value)
        {
            if (target == null) return;
            try
            {
                var prop = target.GetType().GetProperty(propName, AccessTools.all);
                if (prop != null && prop.CanWrite)
                    prop.SetValue(target, value, null);
            }
            catch { }
        }

        private static T GetOverlayField<T>(UIMainOverlayManager instance, string fieldName)
        {
            try
            {
                object val = AccessTools.Field(typeof(UIMainOverlayManager), fieldName)?.GetValue(instance);
                if (val is T t)
                    return t;
            }
            catch { }
            return default;
        }

        /// <summary>
        /// Vanilla-like tooltip placement with overflow clamps. All fields via reflection (MonoMod-safe).
        /// </summary>
        public static void SetFixedTooltipOverlayBoxPosition(this UIMainOverlayManager __instance, Camera cam, RectTransform targeTranseForm)
        {
            try
            {
                if (targeTranseForm == null)
                    return;
                var tooltipCanvas = GetOverlayField<Canvas>(__instance, "tooltipCanvas");
                var tooltipPositionPivot = GetOverlayField<RectTransform>(__instance, "tooltipPositionPivot");
                var tooltipSizePivot = GetOverlayField<RectTransform>(__instance, "tooltipSizePivot");
                if (tooltipCanvas == null || tooltipPositionPivot == null)
                    return;

                Vector2 curSize = GetOverlayField<Vector2>(__instance, "_curSize");
                Vector2 rightDownPivot = GetOverlayField<Vector2>(__instance, "_rightDownPivot");

                RectTransform rect = tooltipCanvas.transform as RectTransform;
                Vector3 worldPoint = targeTranseForm.TransformPoint(targeTranseForm.rect.center);
                // cam may be null for Screen Space Overlay; WorldToScreenPoint accepts null camera.
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPoint);
                Vector2 desiredPos;
                Camera uiCam = tooltipCanvas != null ? tooltipCanvas.worldCamera : cam;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, uiCam, out desiredPos);
                Vector2 vector2 = new Vector2(desiredPos.x + curSize.x, desiredPos.y - curSize.y);

                float sizePivotX = tooltipSizePivot != null ? tooltipSizePivot.anchoredPosition.x : 0f;
                if (vector2.x > rightDownPivot.x)
                    desiredPos.x -= curSize.x + sizePivotX * 2f;
                if (vector2.y < rightDownPivot.y)
                    desiredPos.y += curSize.y - sizePivotX * 2f;
                if ((desiredPos.y - curSize.y) < -500)
                    desiredPos.y -= 500 + (desiredPos.y - curSize.y);
                else if (desiredPos.y + curSize.y / 2 > 520)
                    desiredPos.y -= 520 - (desiredPos.y + curSize.y);
                if (desiredPos.y > 520f)
                    desiredPos.y = 520f;
                tooltipPositionPivot.anchoredPosition = desiredPos;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[RMR] SetFixedTooltipOverlayBoxPosition failed: " + ex.Message);
            }
        }

        /// <summary>
        /// In-battle mouse-follow tooltip placement (reflection-safe).
        /// </summary>
        public static void SetMouseTooltipOverlayBoxPosition(this UIMainOverlayManager __instance)
        {
            try
            {
                var tooltipCanvas = GetOverlayField<Canvas>(__instance, "tooltipCanvas");
                var tooltipPositionPivot = GetOverlayField<RectTransform>(__instance, "tooltipPositionPivot");
                var tooltipSizePivot = GetOverlayField<RectTransform>(__instance, "tooltipSizePivot");
                if (tooltipCanvas == null || tooltipPositionPivot == null)
                    return;

                Vector2 curSize = GetOverlayField<Vector2>(__instance, "_curSize");
                Vector2 rightDownPivot = GetOverlayField<Vector2>(__instance, "_rightDownPivot");

                RectTransform rect = tooltipCanvas.transform as RectTransform;
                Vector2 desiredPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, tooltipCanvas.worldCamera, out desiredPos);
                Vector2 vector2 = new Vector2(desiredPos.x + curSize.x, desiredPos.y - curSize.y);
                float sizePivotX = tooltipSizePivot != null ? tooltipSizePivot.anchoredPosition.x : 0f;
                if (vector2.x > rightDownPivot.x)
                    desiredPos.x -= curSize.x + sizePivotX * 2f;
                if (vector2.y < rightDownPivot.y)
                    desiredPos.y += curSize.y - sizePivotX * 2f;
                if ((desiredPos.y - curSize.y) < -500)
                    desiredPos.y -= 500 + (desiredPos.y - curSize.y);
                else if (desiredPos.y + curSize.y / 2 > 520)
                    desiredPos.y -= 520 - (desiredPos.y + curSize.y);
                if (desiredPos.y > 520f)
                    desiredPos.y = 520f;
                tooltipPositionPivot.anchoredPosition = desiredPos;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[RMR] SetMouseTooltipOverlayBoxPosition failed: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Used for the combat page adding VFX.
    /// </summary>
    public class SelfDestructBezierCurve : MonoBehaviour
    {
        float time = 0f;
        float duration = 1.5f;
        Vector3 pointStart;
        Transform target;
        public Vector3 
            point1 = new Vector3(-40f, -1340f), 
            point2 = new Vector3(-820f, -1590f), 
            destination = new Vector3(915f, -720f);
        // SCREEN POINTS THAT MAKE A NICE CURVE TOWARDS TOP-RIGHT CORNER
        // 920, -800 -> -40 -1340
        // 140, 2130 -> -820 1590
        // 1875, -180 -> 915 -720
        public void Start()
        {
            target = this.gameObject.transform;
            pointStart = target.position;
            time = 0f;
            duration = 1.5f;
        }

        public void Update()
        {
            float timeSquared = (float)((double)time * (double)time); // use doubles for extra accuracy

            // four-point bezier interpolation
            Vector3 comb1 = Vector3.Lerp(pointStart, point1, timeSquared);
            Vector3 comb2 = Vector3.Lerp(point1, point2, timeSquared);
            Vector3 comb3 = Vector3.Lerp(point2, destination, timeSquared);
            Vector3 combLast1 = Vector3.Lerp(comb1, comb2, timeSquared);
            Vector3 combLast2 = Vector3.Lerp(comb2, comb3, timeSquared);
            
            target.localPosition = Vector3.Lerp(combLast1, combLast2, timeSquared);
            double rotationAngle = Math.Tan(target.localPosition.x) / Math.Tan(target.localPosition.y);
            target.rotation.eulerAngles.Set(0f, (float)rotationAngle, 0f);
            time += Time.deltaTime / duration;
            if (time >= 1f) // self destruct after duration
            {
                Destroy(this.gameObject);
                return;
            }
        }
    }

    /// <summary>CardAddVfx</summary>

    public static class CardAddVfx
    {
        public static SnapshotCamera Camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = SnapshotCamera.MakeSnapshotCamera(LayerMask.NameToLayer("Default"));
                }
                return _camera;
            }
        }
        private static SnapshotCamera _camera;
        public static void RunCardVfx(LogLikeMod.UILogCardSlot card)
        {
            /*
            var cardCanvas = card.cg_LeftPanel.gameObject;
            int curLayer = cardCanvas.gameObject.layer;
            Camera.gameObject.SetActive(true);
            var texture2D = Camera.TakeObjectSnapshot(cardCanvas.gameObject, (int)card.Pivot.sizeDelta.x, (int)card.Pivot.sizeDelta.y);
            Camera.gameObject.SetActive(false);
            var sprite = Sprite.Create(texture2D, new Rect(0.0f, 0.0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.0f, 0.0f), 100f, 0U, SpriteMeshType.FullRect);

            /*
            var cardImage = ModdingUtils.CreateImage(LogLikeMod.LogUIObjs[90].transform,
                sprite,
                new Vector2(1f, 1f),
                card.transform.localPosition);
            //
            
            var vfx = LogAssetBundleManager.Instance.GetAsset("CalmMagma", "calmmagma_card");
            // GetAsset instantiates the VFX, VFX is being imported properly
            
            vfx.transform.parent = LogLikeMod.LogUIObjs[90].transform;
            vfx.transform.localPosition = card.transform.localPosition;
            vfx.transform.localScale = new Vector3(1f, 1f, 1f);
            vfx.SetLayerAll(curLayer);
            vfx.GetComponent<SpriteRenderer>().sprite = sprite;
            // var curve = cardImage.gameObject.AddComponent<SelfDestructBezierCurve>();
            vfx.SetActive(true);
            */
        }
    }

    #endregion


    #region HARMONY PATCHES
    [HarmonyPatch]
    public class RMR_Patches
    {
        #region PREFIXES

        /// <summary>
        /// Skip the -853 shell prepare (闪光 / 当前舞台1/1). When realization was chosen with a
        /// pre-selected floor, rebind to the multi-wave floor stage before the first prepare paints.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UI.UIController), "OpenBattlePrepare")]
        static bool UIController_OpenBattlePrepare_Prefix()
        {
            try
            {
                if (RMRRealizationManager.TryRedirectShellPrepareToRealization())
                {
                    Debug.Log("[RMR] OpenBattlePrepare: shell redirected to vanilla Floor Realization prepare.");
                    return false; // skip original --redirect already opened prepare
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] OpenBattlePrepare prefix redirect failed: " + ex.Message);
            }
            return true;
        }

        #endregion

        #region POSTFIXES
        /// <summary>
        /// Responsible for making the Item Catalog Credenza tab activate and deactivate other panels
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UIStoryArchivesPanel), nameof(UIStoryArchivesPanel.TabControllerUpdated))]
        static void OpenItemCatalogTab_Post(UIStoryArchivesPanel __instance)
        {
            try
            {
                UICustomTabsController tabs = RMRCore.GetStoryArchivesTabController(__instance);
                if (tabs == null)
                    return;
                if (tabs.GetCurrentIndex() == 4)
                {
                    __instance.sephirahStoryPanel?.Deactivate();
                    __instance.battleStoryPanel?.Deactivate();
                    __instance.bookStoryPanel?.Deactivate();
                    __instance.creatureRebattlePanel?.Deactivate();
                    Singleton<GlobalLogueItemCatalogPanel>.Instance?.Activate();
                }
                else
                {
                    Singleton<GlobalLogueItemCatalogPanel>.Instance?.Deactivate();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] OpenItemCatalogTab_Post: " + ex.Message);
            }
        }

        /// <summary>
        /// Patch that initializes the Item Catalog panel; runs after the credenza is done initializing
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UIStoryArchivesPanel), nameof(UIStoryArchivesPanel.InitData))]
        static void InitItemCatalogTab_Post(UIStoryArchivesPanel __instance)
        {
            try
            {
                if (Singleton<GlobalLogueItemCatalogPanel>.Instance == null)
                    return;
                if (Singleton<GlobalLogueItemCatalogPanel>.Instance.root == null)
                    Singleton<GlobalLogueItemCatalogPanel>.Instance.GetLogUIObj();
                Singleton<GlobalLogueItemCatalogPanel>.Instance.Init();

                UICustomTabsController tabs = RMRCore.GetStoryArchivesTabController(__instance);
                if (tabs == null)
                    return;

                if (tabs.GetCurrentIndex() != 4)
                    Singleton<GlobalLogueItemCatalogPanel>.Instance.Deactivate();

                if (tabs.TabsRoot != null)
                    tabs.TabsRoot.transform.localPosition = new Vector3(-290f, 3.17f, 0f);
                if (tabs.CustomTabs != null)
                {
                    if (tabs.CustomTabs.Length > 0 && tabs.CustomTabs[0] != null)
                        tabs.CustomTabs[0].transform.localPosition = new Vector3(325f, 35f, 0f);
                    if (tabs.CustomTabs.Length > 1 && tabs.CustomTabs[1] != null)
                        tabs.CustomTabs[1].transform.localPosition = new Vector3(570f, 35f, 0f);
                    if (tabs.CustomTabs.Length > 2 && tabs.CustomTabs[2] != null)
                        tabs.CustomTabs[2].transform.localPosition = new Vector3(720f, 35f, 0f);
                    if (tabs.CustomTabs.Length > 4 && tabs.CustomTabs[4] != null)
                        tabs.CustomTabs[4].transform.localPosition = new Vector3(900f, 35f, 0f);
                }
                if (Singleton<GlobalLogueItemCatalogPanel>.Instance.button != null
                    && Singleton<GlobalLogueItemCatalogPanel>.Instance.button.TabName != null)
                {
                    Singleton<GlobalLogueItemCatalogPanel>.Instance.button.TabName.text =
                        TextDataModel.GetText("ui_RMR_ItemCatalog");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] InitItemCatalogTab_Post: " + ex.Message);
            }
        }

        /// <summary>
        /// Patch that adds books into the book list
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(DropBookInventoryModel), "GetBookList_invitationBookList")]
        static List<LorId> AddInvitationBooks(List<LorId> result)
        {
            foreach (LorId book in RMRCore.booksToAddToInventory)
            {
                int num;
                if ((num = result.Count(x => x == book)) > 1)
                {
                    result.RemoveAll(x => x == book);
                    result.Add(book);
                }
                else if (num == 0) result.Add(book);
            }
            return result;
        }

        /// <summary>
        /// Simple patch that makes exclusive combat pages work within Roguelike and not shit pant
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(BookModel), nameof(BookModel.SetXmlInfo))]
        public static void BookModel_SetXmlInfo_Post(BookModel __instance, BookXmlInfo ____classInfo, ref List<DiceCardXmlInfo> ____onlyCards)
        {
            if (__instance == null || ____onlyCards == null)
                return;
            var mod = LogLikeMod.GetLogMods().Find(x => x.invInfo.workshopInfo.uniqueId == __instance.BookId.packageId);
            if (____classInfo?.EquipEffect?.OnlyCard != null
                && (__instance.BookId.packageId == LogLikeMod.ModId || mod != null))
            {
                ____onlyCards.RemoveAll(x => x == null || x.id.IsBasic());
                foreach (int id in ____classInfo.EquipEffect.OnlyCard)
                {
                    DiceCardXmlInfo cardItem = RewardingModel.GetCardItemOriginAware(new LorId(__instance.BookId.packageId, id))
                        ?? RewardingModel.GetCardItemOriginAware(new LorId(id))
                        ?? ItemXmlDataList.instance.GetCardItem(new LorId(__instance.BookId.packageId, id), false)
                        ?? ItemXmlDataList.instance.GetCardItem(id, false);
                    if (cardItem != null)
                        ____onlyCards.Add(cardItem);
                }
            } 
        }

        #endregion

        #region TRANSPILERS
        /// <summary>
        /// Cyam's front sprite patch, for forcing front sprites to show above everything else<br></br>
        /// Apparently less "forceful" than Hat's patch due to not messing with the layers<br></br>
        /// // CalmMagma: Also, apparently all of this is just to change ONE FUCKING BOOLEAN FROM <see langword="false"/> TO <see langword="true"/>? What the fuck?
        /// </summary>
        [HarmonyPatch(typeof(SdCharacterUtil), nameof(SdCharacterUtil.CreateSkin))]
        [HarmonyPatch(typeof(UICharacterRenderer), nameof(UICharacterRenderer.SetCharacter))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SdCharacterUtil_CreateSkin_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilgen)
        {
            var setDataMethod = AccessTools.Method(typeof(WorkshopSkinDataSetter), nameof(WorkshopSkinDataSetter.SetData), new Type[] { typeof(WorkshopSkinData) });
            bool obtainedFlag = false;
            LocalBuilder local = null;
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].Is(OpCodes.Callvirt, setDataMethod))
                {
                    if (!obtainedFlag)
                    {
                        int j;
                        for (j = i + 1; j < codes.Count; j++)
                        {
                            if (codes[j].Branches(out Label? _))
                            {
                                j = codes.Count;
                            }
                            else
                            {
                                if (codes[j].IsStloc())
                                {
                                    local = codes[j].operand as LocalBuilder;
                                    if (local != null && local.LocalType == typeof(bool))
                                    {
                                        obtainedFlag = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (j == codes.Count)
                        {
                            Debug.Log("Failed to obtain LateInit flag for CreateSkin");
                        }
                    }
                    else
                    {
                        codes.InsertRange(i + 1, new CodeInstruction[]
                        {
                            new CodeInstruction(OpCodes.Ldc_I4_1),
                            new CodeInstruction(OpCodes.Stloc_S, local)
                        });
                        break;
                    }
                }
            }
            return codes;
        }

        /// <summary>
        /// Patch that localizes modded BattleUnitBufs
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(BattleUnitBuf), nameof(BattleUnitBuf.Init))]
        static IEnumerable<CodeInstruction> InitBattleUnitBuf_Trans(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var x in instructions)
            {
                yield return x;
                if (x.opcode == OpCodes.Stfld)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // loads current instance of BattleUnitBuf onto stack
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RMR_Patches), nameof(RMR_Patches.InitBattleUnitBuf_Infix)));
                }
            }
        }
        static void InitBattleUnitBuf_Infix(BattleUnitBuf buf)
        {
            try
            {
                if (buf == null)
                    return;
                string keyword = buf.keywordIconId ?? buf.keywordId;
                string fullName = buf.GetType().Assembly.FullName;
                if (string.IsNullOrEmpty(keyword) || !RMRCore.ClassIds.ContainsKey(fullName))
                    return;

                Sprite sprite = null;
                if (RMRCore.ClassIds[fullName] == RMRCore.packageId && LogLikeMod.ArtWorks != null && LogLikeMod.ArtWorks.ContainsKey(keyword))
                    sprite = LogLikeMod.ArtWorks[keyword];
                else if (LogLikeMod.ModdedArtWorks != null
                    && LogLikeMod.ModdedArtWorks.ContainsKey((RMRCore.ClassIds[fullName], keyword)))
                    sprite = LogLikeMod.ModdedArtWorks[(RMRCore.ClassIds[fullName], keyword)];

                if (sprite == null)
                    return;

                // Private fields --use reflection (FieldAccessException under MonoMod otherwise).
                AccessTools.Field(typeof(BattleUnitBuf), "_bufIcon")?.SetValue(buf, sprite);
                var iconInit = AccessTools.Field(typeof(BattleUnitBuf), "_iconInit");
                if (iconInit != null && iconInit.FieldType == typeof(bool))
                    iconInit.SetValue(buf, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RMR] Unable to set buf icon: " + e.Message);
            }
        }


        /// <summary>
        /// Patch to add in keypage thumbnails automatically (if they exist)<br></br>
        /// TO USE: Simply name the skin name and the skin folder the same thing.<br></br>
        /// Then put a 256x512 image named "Thumb.png" inside the skin's respective ClothCustom folder. That's it.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(BookModel), nameof(BookModel.GetThumbSprite))]
        static IEnumerable<CodeInstruction> BookModel_GetThumbSprite_Trans(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var x in instructions)
            {
                yield return x;
                if (x.opcode == OpCodes.Callvirt && x.Calls(typeof(ClothCustomizeData).GetMethod("get_" + nameof(ClothCustomizeData.sprite))))
                {
                    // patch occurs right after Sprite getter; stack currently has the Default sprite at the top
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // loads current instance of BookModel onto stack
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RMR_Patches), nameof(BookModel_GetThumbSprite_Method)));
                }
            }
        }
        static Sprite BookModel_GetThumbSprite_Method(Sprite sprite, BookModel book)
        {
            try
            {
                Texture2D texture2D = new Texture2D(4, 4);
                string path = Path.Combine(ModContentManager.Instance.GetModPath(book.BookId.packageId), "Resource", "CharacterSkin", book._characterSkin, "ClothCustom", "Thumb.png");
                if (File.Exists(path))
                {
                    texture2D.LoadImage(File.ReadAllBytes(path));
                    sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
                }
            } catch (Exception e)
            {
                Debug.Log("Unable to set keypage thumbnail: " + e);
            }
            return sprite;
        }

        /// <summary>
        /// Second part of the keypage thumbnail patch.<br></br>
        /// Refer to <see cref="BookModel_GetThumbSprite_Trans"/> for the first patch.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(BookXmlInfo), nameof(BookXmlInfo.GetThumbSprite))]
        static IEnumerable<CodeInstruction> BookXmlInfo_GetThumbSprite_Trans(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var x in instructions)
            {
                yield return x;
                if (x.opcode == OpCodes.Callvirt && x.Calls(typeof(ClothCustomizeData).GetMethod("get_" + nameof(ClothCustomizeData.sprite))))
                {
                    // patch occurs right after Sprite getter; stack currently has the Default sprite at the top
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // loads current instance of BookXmlInfo onto stack
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RMR_Patches), nameof(BookXmlInfo_GetThumbSprite_Method)));
                }
            }
        }
        static Sprite BookXmlInfo_GetThumbSprite_Method(Sprite sprite, BookXmlInfo book)
        {
            try
            {
                Texture2D texture2D = new Texture2D(4, 4);
                string path = Path.Combine(ModContentManager.Instance.GetModPath(book.id.packageId), "Resource", "CharacterSkin", book.GetCharacterSkin(), "ClothCustom", "Thumb.png");
                if (File.Exists(path))
                {
                    texture2D.LoadImage(File.ReadAllBytes(path));
                    sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (Exception e)
            {
                Debug.Log("Unable to set keypage thumbnail: " + e);
            }
            return sprite;
        }

        /// <summary>
        /// Makes singleton account for upgraded pages by checking their original IDs instead
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.IsHighlander))]
        static IEnumerable<CodeInstruction> IsHighlander_UpgradePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var x in instructions)
            {
                if (x.opcode == OpCodes.Stloc_2)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExtensionUtils), nameof(ExtensionUtils.GetOriginalId)));
                }
                yield return x;
            }
        }

        #endregion

        #region FINALIZERS

        /// <summary>
        /// Makes BookModel.SortPassive stop bitching
        /// </summary>
        [HarmonyPatch(typeof(BookModel), nameof(BookModel.SortPassive))]
        [HarmonyFinalizer]
        static Exception BookModel_SortPassive_Finalizer(Exception __exception)
        {
            return __exception is ArgumentOutOfRangeException ? null : __exception;
        }


        /// <summary>
        /// Cyam's front sprite patch- this is to prevent the game from shitting itself
        /// </summary>
        [HarmonyPatch(typeof(Workshop.WorkshopSkinDataSetter), nameof(Workshop.WorkshopSkinDataSetter.LateInit))]
        [HarmonyFinalizer]
        static Exception WorkshopSkinDataSetter_LateInit_Finalizer(Exception __exception)
        {
            return __exception is NullReferenceException ? null : __exception;
        }

        /// <summary>
        /// Makes UI.UISelectableIconController.HideIcon stop bitching
        /// </summary>
        [HarmonyPatch(typeof(UI.UISelectableIconController), nameof(UI.UISelectableIconController.HideIcon))]
        [HarmonyFinalizer]
        static Exception UISelectableIconController_HideIcon(Exception __exception)
        {
            return __exception is NullReferenceException ? null : __exception;
        }

        /*
        /// <summary>
        /// Makes BattleEmotionCardModel's constructor stop bitching
        /// </summary>
        [HarmonyPatch(typeof(System.Activator), nameof(System.Activator.CreateInstance), new Type[2]
        {
            typeof(System.Type),
            typeof(System.Boolean)
        })]
        [HarmonyFinalizer]
        static Exception BattleEmotionCardModel_Constructor(Exception __exception)
        {
            return __exception is ArgumentNullException ? null : __exception;
        }
        */

        #endregion
    }

    #endregion
}
