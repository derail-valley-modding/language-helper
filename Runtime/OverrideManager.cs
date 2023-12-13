using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVLangHelper.Runtime
{
    internal static class OverrideManager
    {
        public const string OVERRIDE_DIR_NAME = "override";

        public static string OverrideDir => Path.Combine(LangHelperMain.Instance.Path, OVERRIDE_DIR_NAME);
        public static string OverridePath(string sourceId) => Path.Combine(LangHelperMain.Instance.Path, OVERRIDE_DIR_NAME, $"{sourceId}.csv");

        public static bool DoesOverrideExist(string sourceId) => File.Exists(OverridePath(sourceId));

        public static void CreateOverrideFile(string sourceId)
        {
            static string CsvValue(string value)
            {
                return value.Contains(' ') ? $"\"{value}\"" : value;
            }

            var source = TranslationInjector.Instances.First(x => x.Id == sourceId);

            if (!Directory.Exists(OverrideDir))
            {
                Directory.CreateDirectory(OverrideDir);
            }

            var sb = new StringBuilder();

            // header
            sb.Append("Key,Description");

            int langCount = 0;
            foreach (var lang in source.Languages)
            {
                sb.Append(',');
                sb.Append(CsvValue(lang.Name));
                langCount += 1;
            }
            sb.AppendLine();

            // terms
            string termCommas = new string(Enumerable.Repeat(',', langCount).ToArray());
            foreach (var term in source.Terms)
            {
                sb.Append($"{term.Term},{CsvValue(term.Description)}");
                sb.AppendLine(termCommas);
            }

            File.WriteAllText(OverridePath(sourceId), sb.ToString(), Encoding.UTF8);
        }

        public static void DeleteOverrideFile(string sourceId)
        {
            string overridePath = OverridePath(sourceId);
            if (File.Exists(overridePath))
            {
                try
                {
                    File.Delete(overridePath);
                }
                catch (Exception ex)
                {
                    LangHelperMain.Error("Failed to delete override file", ex);
                }
            }
        }

        public static void ReloadOverrideForSource(string sourceId)
        {
            string overrideFilePath = OverridePath(sourceId);
            if (File.Exists(overrideFilePath) && 
                (TranslationInjector.Instances.FirstOrDefault(i => i.Id == sourceId) is TranslationInjector source))
            {
                source.AddTranslationsFromCsv(overrideFilePath, true);
                LangHelperMain.Log($"Loaded override file for {sourceId}");
            }
        }

        public static void ReloadOverrides(bool reloadAll)
        {
            CoroutineManager.Start(ReloadOverridesWorker(reloadAll));
        }

        private static IEnumerator ReloadOverridesWorker(bool reloadAll)
        {
            foreach (string overrideFilePath in Directory.EnumerateFiles(OverrideDir, "*.csv"))
            {
                string sourceId = Path.GetFileNameWithoutExtension(overrideFilePath);

                if (TranslationInjector.Instances.FirstOrDefault(i => i.Id == sourceId) is TranslationInjector source)
                {
                    if (reloadAll)
                    {
                        source.Reload();
                    }

                    while (source.PendingWebRequests)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    source.AddTranslationsFromCsv(overrideFilePath, true);
                    LangHelperMain.Log($"Loaded override file for {sourceId}");
                }
            }
        }
    }
}
