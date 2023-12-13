using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
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
            modEntry.OnHideGUI = HideGUI;

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

        public static string GuiMessage = string.Empty;

        private static void DrawGUI(UnityModManager.ModEntry entry)
        {
            Settings.Draw(entry);

            GUILayout.Space(5);
            GUILayout.Label("NOTE: Changes made to translation overrides may require a game restart");
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reload Overrides", GUILayout.Width(200)))
            {
                OverrideManager.ReloadOverrides(true);
            }

            if (GUILayout.Button("Open Folder", GUILayout.Width(200)))
            {
                if (!Directory.Exists(OverrideManager.OverrideDir))
                {
                    Directory.CreateDirectory(OverrideManager.OverrideDir);
                }
                System.Diagnostics.Process.Start("explorer.exe", OverrideManager.OverrideDir);
            }

            GUILayout.EndHorizontal();

            foreach (var source in TranslationInjector.Instances)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(source.Id);

                if (GUILayout.Button("New Override File", GUILayout.Width(200)))
                {
                    OverrideManager.CreateOverrideFile(source.Id);
                    GuiMessage = $"Created {source.Id} override file";
                }
                
                GUI.enabled = OverrideManager.DoesOverrideExist(source.Id);
                if (GUILayout.Button("Reload", GUILayout.Width(100)))
                {
                    OverrideManager.ReloadOverrideForSource(source.Id);
                    GuiMessage = $"Reloaded overrides for {source.Id}";
                }

                if (GUILayout.Button("Delete", GUILayout.Width(100)))
                {
                    OverrideManager.DeleteOverrideFile(source.Id);
                    GuiMessage = $"Deleted {source.Id} override file";
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            if (!string.IsNullOrEmpty(GuiMessage))
            {
                GUILayout.Label(GuiMessage);
            }
        }

        private static void SaveGUI(UnityModManager.ModEntry entry)
        {
            Settings.Save(entry);
        }

        private static void HideGUI(UnityModManager.ModEntry entry)
        {
            GuiMessage = string.Empty;
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

        [Draw("Reload translations files")]
        public bool Reload = false;

        public void OnChange()
        {
            if (Reload)
            {
                Reload = false;
                int nReloaded = TranslationInjector.ReloadTranslationFiles();
                LangHelperMain.GuiMessage = $"Reloaded {nReloaded} translation files";
            }
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    [HarmonyPatch]
    internal static class UMM_AfterLoadPatch
    {
        [HarmonyTargetMethod]
        public static MethodInfo GetTarget()
        {
            return typeof(UnityModManager).GetNestedType("GameScripts", BindingFlags.NonPublic).GetMethod("OnAfterLoadMods");
        }

        [HarmonyPostfix]
        public static void AfterLoaded()
        {
            OverrideManager.ReloadOverrides(false);
        }
    }
}
