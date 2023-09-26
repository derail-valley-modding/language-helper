using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityModManagerNet;

namespace DVLangHelper.Runtime
{
    public static class LangHelperMain
    {
        public static UnityModManager.ModEntry Instance = null!;
        public static LangHelperSettings Settings = null!;

        public static string CacheDirPath => Path.Combine(Instance.Path, "cache");

        public static void Load(UnityModManager.ModEntry modEntry)
        {
            Instance = modEntry;

            Settings = UnityModManager.ModSettings.Load<LangHelperSettings>(modEntry);
            modEntry.OnGUI = DrawGUI;
            modEntry.OnSaveGUI = SaveGUI;

            string cacheDir = CacheDirPath;
            if (!Settings.UseCache && Directory.Exists(cacheDir))
            {
                try
                {
                    Directory.Delete(cacheDir, true);
                }
                catch { }
            }

            var harmony = new Harmony("cc.foxden.dv_lang_helper");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void DrawGUI(UnityModManager.ModEntry entry)
        {
            Settings.Draw(entry);
        }

        private static void SaveGUI(UnityModManager.ModEntry entry)
        {
            Settings.Save(entry);
        }

        public static void Log(string message)
        {
            Instance!.Logger.Log(message);
        }

        public static void Warning(string message)
        {
            Instance!.Logger.Warning(message);
        }

        public static void Error(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                Instance!.Logger.LogException(exception);
            }
            Instance!.Logger.Error(message);
        }
    }

    public class LangHelperSettings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Enable Caching of Web CSVs (debug)")]
        public bool UseCache = true;

        public void OnChange()
        {
            
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
