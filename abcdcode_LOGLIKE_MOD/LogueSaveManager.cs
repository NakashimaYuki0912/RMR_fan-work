// -----------------------------------------------------------------------------
// LOGLIKE core UI/data: LogueSaveManager
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\LogueSaveManager.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>LOGLIKE type: LogueSaveManager</summary>

    public class LogueSaveManager : Singleton<LogueSaveManager>
    {
        public void RemoveData(string savename)
        {
            if (string.IsNullOrEmpty(savename))
                return;
            if (!Directory.Exists(LogueSaveManager.Saveroot))
                Directory.CreateDirectory(LogueSaveManager.Saveroot);
            string path = Path.Combine(LogueSaveManager.Saveroot, savename);
            if (!File.Exists(path))
                return;
            try { File.Delete(path); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RMR Save] RemoveData('{savename}') failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Persist via temp file + replace. Never open the destination with File.Create first —
        /// that truncates immediately and leaves an empty/all-zero file if the process dies
        /// mid-serialize (root cause of RMR_ItemCatalog BinaryHeader softlocks).
        /// </summary>
        public void SaveData(GameSave.SaveData data, string savename)
        {
            if (data == null || string.IsNullOrEmpty(savename))
                return;
            string path = Path.Combine(LogueSaveManager.Saveroot, savename);
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak_replace";
            try
            {
                if (!Directory.Exists(LogueSaveManager.Saveroot))
                    Directory.CreateDirectory(LogueSaveManager.Saveroot);

                object serializedData = data.GetSerializedData();
                using (FileStream serializationStream = File.Create(tempPath))
                    new BinaryFormatter().Serialize((Stream)serializationStream, serializedData);

                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length <= 0)
                {
                    Debug.LogError($"[RMR Save] Refusing to publish empty temp for '{savename}'.");
                    TryDelete(tempPath);
                    return;
                }

                if (File.Exists(path))
                {
                    try
                    {
                        // Atomic-ish on NTFS: destination never left truncated mid-write.
                        File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
                        TryDelete(backupPath);
                    }
                    catch (Exception replaceEx)
                    {
                        Debug.LogWarning($"[RMR Save] File.Replace failed for '{savename}' ({replaceEx.Message}); falling back to delete+move.");
                        TryDelete(path);
                        File.Move(tempPath, path);
                    }
                }
                else
                {
                    if (File.Exists(tempPath))
                        File.Move(tempPath, path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RMR Save] Failed to save '{savename}': {ex.GetType().Name}: {ex.Message}");
                TryDelete(tempPath);
            }
        }

        public GameSave.SaveData LoadData(string savename)
        {
            if (string.IsNullOrEmpty(savename))
                return null;
            if (!Directory.Exists(LogueSaveManager.Saveroot))
            {
                Directory.CreateDirectory(LogueSaveManager.Saveroot);
                return null;
            }
            string path = Path.Combine(LogueSaveManager.Saveroot, savename);
            if (!File.Exists(path))
                return null;

            try
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                object serialized;
                using (FileStream serializationStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (serializationStream.Length <= 0)
                    {
                        Debug.LogWarning($"[RMR Save] Empty save '{savename}' — treating as missing.");
                        QuarantineCorrupt(path, savename);
                        return null;
                    }
                    serialized = binaryFormatter.Deserialize((Stream)serializationStream);
                }
                if (serialized == null)
                    return null;
                GameSave.SaveData saveData = new GameSave.SaveData();
                saveData.LoadFromSerializedData(serialized);
                return saveData;
            }
            catch (Exception ex)
            {
                // Corrupt catalog/snapshot must never abort combat UI (SelectOne emotion pick
                // used to soft-lock because AddToObtainCount threw here mid OnClickTargetUnit).
                Debug.LogError($"[RMR Save] Failed to load '{savename}' ({ex.GetType().Name}: {ex.Message}). Quarantining corrupt file.");
                QuarantineCorrupt(path, savename);
                return null;
            }
        }

        public static string Saveroot => SaveManager.savePath + "/LogueSave";

        public void AddToObtainCount(object item, int count = 1)
        {
            if (item == null)
                return;
            try
            {
                GameSave.SaveData saveData = Singleton<LogueSaveManager>.Instance.LoadData("RMR_ItemCatalog");
                if (saveData == null)
                    saveData = new GameSave.SaveData();
                string key = "ObtainCount_" + item.GetType().Name;
                int current = 0;
                try { current = saveData.GetInt(key); } catch { current = 0; }
                saveData.SetData(key, new GameSave.SaveData(current + count));
                Singleton<LogueSaveManager>.Instance.SaveData(saveData, "RMR_ItemCatalog");
            }
            catch (Exception ex)
            {
                // Catalog stats are non-critical — never block emotion/shop/reward completion.
                Debug.LogWarning("[RMR Save] AddToObtainCount skipped: " + ex.Message);
            }
        }

        private static void QuarantineCorrupt(string path, string savename)
        {
            try
            {
                string quarantine = path + ".corrupt_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (File.Exists(quarantine))
                    File.Delete(quarantine);
                if (File.Exists(path))
                    File.Move(path, quarantine);
            }
            catch (Exception moveEx)
            {
                Debug.LogWarning($"[RMR Save] Quarantine failed for '{savename}', deleting: " + moveEx.Message);
                TryDelete(path);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch { /* best-effort */ }
        }
    }
}
