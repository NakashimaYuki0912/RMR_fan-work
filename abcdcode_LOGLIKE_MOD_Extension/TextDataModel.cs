// -----------------------------------------------------------------------------
// LOGLIKE XML extension model: TextDataModel
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD_Extension\TextDataModel.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

 
namespace abcdcode_LOGLIKE_MOD_Extension
{
    /// <summary>TextDataModel</summary>
    public static class TextDataModel
    {
      public const string ErrorText = "<color=#FF5544>ERROR</color>";
      public static Dictionary<string, string> _dic = new Dictionary<string, string>();
      public static string _currentLanguage = "kr";
      public static bool _isLoaded = false;
      public static string[] _supported = new string[5]
      {
        "kr",
        "en",
        "jp",
        "cn",
        "trcn"
      };
      public static bool _yame = false;

      public static string CurrentLanguage => TextDataModel._currentLanguage;

      public static Dictionary<string, string> textDic => TextDataModel._dic;

      public static void InitTextData(string currentLanguage)
      {
        TextDataModel._dic.Clear();
        TextDataModel._isLoaded = false;
        if (!TextDataModel._supported.Contains<string>(currentLanguage))
        {
          Debug.LogError( "not supported Language");
          currentLanguage = "en";
        }
        // Publish the new language BEFORE loading. Load() ends by calling LoadOthers, and RMR's
        // LoadOthers postfix reads CurrentLanguage to decide which language to re-stamp vanilla
        // tables with. Assigning afterwards meant that postfix still saw the PREVIOUS language, so
        // the tables were rebuilt one switch behind -- English UI showing Chinese text and vice
        // versa. It also starts out as the "kr" placeholder, which is not any real selection.
        TextDataModel._currentLanguage = currentLanguage;
        if (!TextDataModel._isLoaded)
        {
          Singleton<LocalizedTextLoader>.Instance.Load(currentLanguage, ref TextDataModel._dic);
          TextDataModel._isLoaded = true;
        }
      }

      public static string GetText(string id, params object[] args)
      {
        // Lazy load, but never from inside a text lookup that ran too early.
        //
        // LocalizedTextLoader.Load ends by calling LoadOthers, so this one line re-ran the game's
        // ENTIRE localization just to resolve a single mod UI key. Worse, _currentLanguage starts as
        // "kr", so an early GetText reloaded every vanilla table in Korean and RMR only re-stamped
        // some of them back -- that is how reception and key-page group titles ended up Korean in an
        // English game. Instance was also dereferenced unguarded, which throws during early init.
        if (!TextDataModel._isLoaded && !TextDataModel._yame)
        {
          TextDataModel._yame = true;
          try
          {
            LocalizedTextLoader loader = Singleton<LocalizedTextLoader>.Instance;
            if (loader != null)
            {
              string lang = TextDataModel._currentLanguage;
              if (string.IsNullOrEmpty(lang) || lang == "kr")
              {
                // Do not reload the world in the placeholder language; wait for LoadTextData.
                lang = null;
              }
              if (lang != null)
                loader.Load(lang, ref TextDataModel._dic);
            }
          }
          catch
          {
            // A failed lazy load must not take down every caller of GetText.
          }
        }
        string format;
        if (TextDataModel._dic == null || !TextDataModel._dic.TryGetValue(id, out format))
          return string.Empty;
        format = format.Replace("\\n", "\n");
        if (format.Contains("[[") && format.Contains("]]"))
        {
          format = format.Replace("[[", "<sprite=");
          format = format.Replace("]]", ">");
        }
        string text;
        try
        {
          text = string.Format(format, args);
        }
        catch
        {
          text = format;
        }
        return text;
      }

      public static List<string> GetSupportedLangs()
      {
        List<string> supportedLangs = new List<string>();
        string[] supported = TextDataModel._supported;
        if (supported != null && supported.Length != 0)
        {
          for (int index = 0; index < TextDataModel._supported.Length; ++index)
            supportedLangs.Add(TextDataModel._supported[index]);
        }
        return supportedLangs;
      }
    }
}
