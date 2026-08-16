// -----------------------------------------------------------------------------
// LoguePlayDataSaver — run snapshot I/O for Roguelike Mod Reborn (RMR).
//
// Disk key "Lastest" is a historical typo used by Continue Run. Do NOT rename it
// or existing continue saves break. Version gate uses LoguePlayDataSaver.version.
//
// Related: LogueBookModels (party/inventory), LogLikeMod (chapter/money),
//          RMRStartHubPanel (Continue button visibility via CheckPlayerData).
// See also: docs/localization/CODE_TERMINOLOGY.md
// -----------------------------------------------------------------------------

using GameSave;
using Mod;
using RogueLike_Mod_Reborn;
using System;
using UnityEngine;


namespace abcdcode_LOGLIKE_MOD
{
    /// <summary>
    /// Save / load helpers for a single roguelike run.
    /// Primary continue snapshot file name: <c>Lastest</c> (kept for compatibility).
    /// </summary>
    public class LoguePlayDataSaver
    {
        /// <summary>Save format version. Mismatch → CheckPlayerData returns false.</summary>
        public static string version = "4.8";
        public static bool RunDefeatPending { get; private set; }
        /// <summary>
        /// ESC / return-to-title abort. Preserves Lastest; next ClearBattle must not
        /// overwrite it with mid-fight HP/isDead or treat teardown as a party wipe.
        /// </summary>
        public static bool RunAbortWithoutDefeat { get; private set; }

        #region --- Debug / chapter bootstrap ---

        /// <summary>Build a fresh chapter save and load it (debug / chapter jump).</summary>
        public static void LoadChDebugData(ChapterGrade grade)
        {
            SaveData data = new SaveData();
            data.AddData("version", new SaveData(LoguePlayDataSaver.version));
            data.AddData("LogueBookModel", LogueBookModels.CreateChSaveData(grade));
            data.AddData("LogLikeMod", LogLikeMod.CreateChSaveData(grade));
            data.AddData("GlobalEffect", Singleton<GlobalLogueEffectManager>.Instance.GetSaveData());
            LoguePlayDataSaver.LoadPlayData(data);
        }

        #endregion

        #region --- Transient shop / mystery (not the continue snapshot) ---

        /// <summary>Clear shop + mystery temporary files after the node is left.</summary>
        public static void RemoveFlashData()
        {
            LoguePlayDataSaver.RemoveShop();
            LoguePlayDataSaver.RemoveMystery();
        }

        public static void RemoveShop() => Singleton<LogueSaveManager>.Instance.RemoveData("LastShop");

        public static void SaveShop(ShopBase shop)
        {
            Singleton<LogueSaveManager>.Instance.SaveData(shop.GetSaveData(), "LastShop");
        }

        public static bool LoadShop(ShopBase shop)
        {
            SaveData data = Singleton<LogueSaveManager>.Instance.LoadData("LastShop");
            if (data == null)
                return false;
            shop.LoadFromSaveData(data);
            return true;
        }

        public static void RemoveMystery()
        {
            Singleton<LogueSaveManager>.Instance.RemoveData("LastMystery");
        }

        public static void SaveMystery(MysteryBase mystery)
        {
            SaveData saveData = mystery.GetSaveData();
            if (saveData == null)
                return;
            Singleton<LogueSaveManager>.Instance.SaveData(saveData, "LastMystery");
        }

        public static bool LoadMystery(MysteryBase mystery)
        {
            SaveData savedata = Singleton<LogueSaveManager>.Instance.LoadData("LastMystery");
            if (savedata == null)
                return false;
            mystery.LoadFromSaveData(savedata);
            return true;
        }

        #endregion

        #region --- Continue snapshot: Lastest (full run) ---

        /// <summary>
        /// Deletes the continue-run snapshot file named <c>Lastest</c>
        /// (historical typo; do not rename on disk).
        /// Call only when the run truly ends (party wipe or G7 boss complete).
        /// </summary>
        public static void RemovePlayerData()
        {
            // A fresh run is starting, so there is no longer a good snapshot to protect.
            LastLoadFailed = false;
            RunDefeatPending = false;
            RunAbortWithoutDefeat = false;
            Singleton<LogueSaveManager>.Instance.RemoveData("Lastest");
            Debug.Log("[RMR SaveLifecycle] RemovePlayerData Lastest wiped\n" + TruncateStack(Environment.StackTrace));
        }

        public static void MarkRunDefeated(string reason)
        {
            RunDefeatPending = true;
            RunAbortWithoutDefeat = false;
            LastLoadFailed = false;
            Singleton<LogueSaveManager>.Instance.RemoveData("Lastest");
            Debug.Log($"[RMR SaveLifecycle] MarkRunDefeated ({reason})\n" + TruncateStack(Environment.StackTrace));
        }

        /// <summary>
        /// Player left via ESC / title / give-up UI (<c>isbackbutton</c>). Keep Lastest.
        /// </summary>
        public static void MarkRunAbortWithoutDefeat(string reason)
        {
            RunAbortWithoutDefeat = true;
            Debug.Log($"[RMR SaveLifecycle] MarkRunAbortWithoutDefeat ({reason})\n" + TruncateStack(Environment.StackTrace));
        }

        private static string TruncateStack(string stack)
        {
            if (string.IsNullOrEmpty(stack))
                return string.Empty;
            const int max = 800;
            return stack.Length <= max ? stack : stack.Substring(0, max) + "...";
        }

        /// <summary>Consume the abort latch once (ClearBattle). Returns true if abort was pending.</summary>
        public static bool ConsumeRunAbortWithoutDefeat()
        {
            if (!RunAbortWithoutDefeat)
                return false;
            RunAbortWithoutDefeat = false;
            return true;
        }

        /// <summary>
        /// Snapshot writes must never persist a run state that was never fully built. A partial
        /// load leaves cardlist/booklist empty; before this guard the next autosave wrote that
        /// emptiness over the good file, turning one load error into permanent corruption.
        /// </summary>
        private static bool ShouldRefuseSnapshotWrite(string reason)
        {
            if (RunDefeatPending)
            {
                Debug.LogWarning($"[RMR Save] Refusing to write Lastest ({reason}): this run has already ended in defeat.");
                return true;
            }
            if (LastLoadFailed)
            {
                Debug.LogError($"[RMR Save] Refusing to write Lastest ({reason}): the last LoadPlayData failed, so the in-memory run is incomplete. The existing save file is left untouched.");
                return true;
            }
            if (LogLikeMod.saveloading)
            {
                Debug.LogWarning($"[RMR Save] Skipping Lastest write ({reason}): a load is still in progress.");
                return true;
            }
            WarnIfInventoryLooksWiped(reason);
            return false;
        }

        /// <summary>
        /// Diagnostic only — never blocks the write. A genuinely fresh run can legitimately have an
        /// empty inventory, so this must not become a refusal; it only leaves a breadcrumb in
        /// Player.log if the wipe pattern ever shows up again through some other path.
        /// </summary>
        private static void WarnIfInventoryLooksWiped(string reason)
        {
            try
            {
                bool memoryEmpty = (LogueBookModels.cardlist == null || LogueBookModels.cardlist.Count == 0)
                    && (LogueBookModels.booklist == null || LogueBookModels.booklist.Count == 0);
                if (!memoryEmpty)
                    return;
                SaveData existing = Singleton<LogueSaveManager>.Instance.LoadData("Lastest");
                SaveData book = existing != null ? existing.GetData("LogueBookModel") : null;
                if (book == null)
                    return;
                int onDisk = CountEntries(book.GetData("cardlist")) + CountEntries(book.GetData("booklist"));
                if (onDisk > 0)
                    Debug.LogWarning($"[RMR Save] Writing an empty inventory over a Lastest that holds {onDisk} entries ({reason}). If this was not a fresh run, the previous load failed silently.");
            }
            catch { /* diagnostics must never break saving */ }
        }

        private static int CountEntries(SaveData list)
        {
            if (list == null)
                return 0;
            int count = 0;
            try
            {
                foreach (SaveData _ in list)
                    count++;
            }
            catch { return 0; }
            return count;
        }

        /// <summary>
        /// Writes a full run snapshot to LogueSave/<c>Lastest</c> for Continue Run.
        /// Includes book model, LogLikeMod, global effects, companion mod list, gamemode.
        /// </summary>
        public static void SavePlayData()
        {
            if (ShouldRefuseSnapshotWrite("SavePlayData"))
                return;
            SaveData data1 = new SaveData();
            data1.AddData("version", new SaveData(LoguePlayDataSaver.version));
            data1.AddData("LogueBookModel", LogueBookModels.GetSaveData());
            data1.AddData("LogLikeMod", LogLikeMod.GetSaveData());
            data1.AddData("GlobalEffect", Singleton<GlobalLogueEffectManager>.Instance.GetSaveData());
            SaveData data2 = new SaveData();
            foreach (ModContentInfo logMod in LogLikeMod.GetLogMods())
                data2.AddToList(new SaveData(logMod.invInfo.workshopInfo.uniqueId));
            data1.AddData("modlist", data2);
            RoguelikeGamemodeController.Instance.SaveCurrentGamemodeName(data1);
            RoguelikeGamemodeController.Instance.SaveCurrentGamemodeData();
            Singleton<LogueSaveManager>.Instance.SaveData(data1, "Lastest");
        }

        /// <summary>
        /// Lightweight mid-run update of <c>Lastest</c> (shop / menu / act transitions).
        /// If no file exists yet (or version mismatches), falls back to a full <see cref="SavePlayData"/>.
        /// </summary>
        public static void SavePlayData_Menu()
        {
            if (ShouldRefuseSnapshotWrite("SavePlayData_Menu"))
                return;
            SaveData saveData = Singleton<LogueSaveManager>.Instance.LoadData("Lastest");
            // No continue file yet (or version mismatch): write a full snapshot so shop/mystery
            // still create a resumable run (ClearBattle used to wipe Lastest after every act).
            if (saveData == null || saveData.GetString("version") != LoguePlayDataSaver.version)
            {
                try
                {
                    if (RMRCore.CurrentGamemode != null)
                    {
                        SavePlayData();
                        Debug.Log("[RMR] SavePlayData_Menu: created full Lastest (was missing/mismatched).");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RMR] SavePlayData_Menu full-save fallback failed: " + ex.Message);
                }
                return;
            }
            saveData.SetData("version", new SaveData(LoguePlayDataSaver.version));
            saveData.SetData("LogueBookModel", LogueBookModels.GetSaveData());
            saveData.SetData("GlobalEffect", Singleton<GlobalLogueEffectManager>.Instance.GetSaveData());
            try
            {
                SaveData logLike = saveData.GetData("LogLikeMod");
                if (logLike != null)
                    logLike.SetData("Money", new SaveData(PassiveAbility_MoneyCheck.GetMoney()));
            }
            catch { /* ignore money patch failures */ }
            Singleton<LogueSaveManager>.Instance.SaveData(saveData, "Lastest");
        }

        #endregion

        #region --- Load into live run state ---

        /// <summary>
        /// True when the most recent <see cref="LoadPlayData(SaveData)"/> threw, i.e. the live run
        /// state is only partly deserialized. Snapshot writes are refused while this is set.
        /// </summary>
        public static bool LastLoadFailed { get; private set; }

        /// <summary>Apply a SaveData blob into LogueBookModels + LogLikeMod + global effects.</summary>
        public static void LoadPlayData(SaveData data)
        {
            // cardlist / booklist are the LAST two things LogueBookModels.LoadFromSaveData reads,
            // and CreatePlayer() has already reset them to empty by then. So anything that throws
            // in between leaves the librarians intact but the inventory empty — exactly the
            // "Keypages and Combat Pages are all removed" report.
            //
            // Without the finally, saveloading also stayed true forever, which makes CheckStage()
            // permanently true and hands the prep-screen inventory UI those empty lists; the next
            // autosave then wrote the emptiness to disk and the run was unrecoverable.
            LogLikeMod.saveloading = true;
            LastLoadFailed = false;
            try
            {
                LogueBookModels.LoadFromSaveData(data.GetData("LogueBookModel"));
                LogLikeMod.LoadFromSaveData(data.GetData("LogLikeMod"));
                if (LogLikeMod.curchaptergrade >= ChapterGrade.Grade6)
                    RMRCore.EnsureGrade6SpecialCorePagesUnlocked();
                RMRCore.ApplyBinahRedMistProgressionState();
                Singleton<GlobalLogueEffectManager>.Instance.LoadFromSaveData(data.GetData("GlobalEffect"));
            }
            catch (Exception)
            {
                // Latch before rethrowing so the snapshot guard below can refuse to persist the
                // half-built state. Callers keep their existing failure handling.
                LastLoadFailed = true;
                throw;
            }
            finally
            {
                LogLikeMod.saveloading = false;
            }
        }

        /// <summary>Load the on-disk <c>Lastest</c> continue snapshot.</summary>
        public static void LoadPlayData()
        {
            LoguePlayDataSaver.LoadPlayData(Singleton<LogueSaveManager>.Instance.LoadData("Lastest"));
        }

        #endregion

        #region --- Continue eligibility (Start Hub) ---

        private static bool SavedPartyHasLivingMember(SaveData save)
        {
            SaveData bookData = save?.GetData("LogueBookModel");
            SaveData partyData = bookData?.GetData("playerBattleModel");
            if (partyData == null)
                return false;

            foreach (SaveData unit in partyData)
            {
                if (unit == null)
                    continue;
                if (unit.GetInt("isDead") == 0 && unit.GetInt("hp") > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if Continue Run should be offered on the start hub.
        /// Requires a valid <c>Lastest</c> file and matching save version.
        /// Companion modlist is soft-checked (warn only) so optional mods do not hide Continue.
        /// </summary>
        public static bool CheckPlayerData()
        {
            try
            {
                SaveData saveData1 = Singleton<LogueSaveManager>.Instance.LoadData("Lastest");
                if (saveData1 == null)
                    return false;
                if (saveData1.GetString("version") != LoguePlayDataSaver.version)
                {
                    Debug.LogWarning($"[RMR] CheckPlayerData: version mismatch (save={saveData1.GetString("version")}, need={LoguePlayDataSaver.version}).");
                    return false;
                }
                if (!SavedPartyHasLivingMember(saveData1))
                {
                    Debug.LogWarning("[RMR] CheckPlayerData: saved party has no living librarians; deleting failed run.");
                    Singleton<LogueSaveManager>.Instance.RemoveData("Lastest");
                    return false;
                }

                // Soft modlist check: missing companion log-mods must not hide Continue.
                // (Strict match used to hide Continue after enabling/disabling optional mods.)
                SaveData modlist = null;
                try { modlist = saveData1.GetData("modlist"); } catch { modlist = null; }
                if (modlist != null)
                {
                    foreach (SaveData saveData2 in modlist)
                    {
                        if (saveData2 == null)
                            continue;
                        string id = null;
                        try { id = saveData2.GetStringSelf(); } catch { id = null; }
                        if (string.IsNullOrEmpty(id))
                            continue;
                        if (LogLikeMod.GetLogMods().Find(x =>
                                x?.invInfo?.workshopInfo != null
                                && x.invInfo.workshopInfo.uniqueId == id) == null)
                        {
                            Debug.LogWarning($"[RMR] CheckPlayerData: saved log-mod '{id}' not loaded; still offering Continue.");
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] CheckPlayerData failed: " + ex.Message);
                return false;
            }
        }

        #endregion
    }
}
