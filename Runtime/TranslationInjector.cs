using DVLangHelper.Data;
using HarmonyLib;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DVLangHelper.Runtime
{
    public class TranslationInjector
    {
        private static readonly List<TranslationInjector> _instances = new List<TranslationInjector>();
        public static IEnumerable<TranslationInjector> Instances => _instances;

        public static bool InjectionStarted = false;

        public readonly string Id;
        private readonly GameObject _sourceHolder;
        private readonly LanguageSource _source;
        private readonly LanguageSourceData _langData;

        public TranslationInjector(string sourceId)
        {
            Id = sourceId;
            _sourceHolder = new GameObject($"{Id}_Translations");
            UnityEngine.Object.DontDestroyOnLoad(_sourceHolder);

            _source = _sourceHolder.AddComponent<LanguageSource>();
            _langData = _source.SourceData;

            foreach (DVLanguage language in Enum.GetValues(typeof(DVLanguage)))
            {
                _langData.AddLanguage(language.Name(), language.Code());
                _langData.EnableLanguage(language.Name(), true);
            }

            _instances.Add(this);

            if (InjectionStarted)
            {
                PerformInjection();
            }
        }

        public void AddTranslationsFromCsv(string csvPath)
        {
            try
            {
                string csvText = LocalizationReader.ReadCSVfile(csvPath, Encoding.UTF8);
                _langData.Import_CSV(string.Empty, csvText, eSpreadsheetUpdateMode.Merge);
            }
            catch (Exception ex)
            {
                LangHelperMain.Error($"Failed to load csv translations @ {csvPath}", ex);
            }
        }

        public void AddTranslationsFromWebCsv(string url, string fallbackPath)
        {
            _source.StartCoroutine(AddWebCsvCoro(url, fallbackPath));
        }

        private static readonly SHA256 _hasher = SHA256.Create();

        private static string GetSha256Hash(string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = _hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private string GetCsvCachePath(string url)
        {
            string hash = GetSha256Hash(url);
            return Path.Combine(LangHelperMain.CacheDirPath, $"{hash}.csv");
        }

        private IEnumerator AddWebCsvCoro(string url, string fallbackPath)
        {
            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            string cachePath = GetCsvCachePath(url);

            if (request.isNetworkError || request.responseCode != 200)
            {
                LangHelperMain.Warning($"Failed to fetch web csv translations @ {url}, using fallback");

                if (LangHelperMain.Settings.UseCache && File.Exists(cachePath))
                {
                    AddTranslationsFromCsv(cachePath);
                    yield break;
                }

                AddTranslationsFromCsv(fallbackPath);
                yield break;
            }

            string downloaded = request.downloadHandler.text;
            _langData.Import_CSV(string.Empty, downloaded, eSpreadsheetUpdateMode.Merge);

            if (LangHelperMain.Settings.UseCache)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                    File.WriteAllText(cachePath, downloaded);
                }
                catch (Exception ex)
                {
                    LangHelperMain.Error($"Failed to cache csv from {url}", ex);
                }
            }
        }

        public void AddTranslations(string key, TranslationData data)
        {
            AddTranslations(key, data.Items);
        }

        public void AddTranslations(string key, IEnumerable<TranslationItem> items)
        {
            var term = _langData.AddTerm(key);
            
            foreach (var item in items)
            {
                int idx = _langData.GetLanguageIndexFromCode(item.Language.Code());
                term.SetTranslation(idx, item.Value);
            }
        }

        public void AddTranslation(string key, DVLanguage language, string value)
        {
            var term = _langData.AddTerm(key);

            int idx = _langData.GetLanguageIndexFromCode(language.Code());
            term.SetTranslation(idx, value);
        }

        public static void StartInjection()
        {
            foreach (var injector in Instances)
            {
                injector.PerformInjection();
            }

            InjectionStarted = true;
        }

        private static readonly MethodInfo _addSourceMethod = AccessTools.Method(typeof(LocalizationManager), "AddSource");

        public void PerformInjection()
        {
            LangHelperMain.Log($"Injecting language source {Id}");
            _addSourceMethod.Invoke(null, new object[] { _langData });
        }
    }
}
