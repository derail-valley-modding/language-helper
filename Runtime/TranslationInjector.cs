using DVLangHelper.Data;
using HarmonyLib;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public static int ReloadTranslationFiles()
        {
            foreach (var instance in _instances)
            {
                instance.Reload();
            }
            return _instances.Count;
        }

        public readonly string Id;
        private readonly GameObject _sourceHolder;
        private readonly LanguageSource _source;
        private readonly LanguageSourceData _langData;

        public IEnumerable<LanguageData> Languages => _langData.mLanguages;
        public IEnumerable<TermData> Terms => _langData.mTerms;

        private readonly Dictionary<string, TranslationData> _originalData = new Dictionary<string, TranslationData>();
        private readonly List<CsvFileInfo> _csvFiles = new List<CsvFileInfo>(1);

        private readonly List<string> _pendingWebRequests = new List<string>();
        public bool PendingWebRequests => _pendingWebRequests.Count > 0;

        public TranslationInjector(string sourceId)
        {
            Id = sourceId;
            _sourceHolder = new GameObject($"{Id}_Translations");
            UnityEngine.Object.DontDestroyOnLoad(_sourceHolder);

            _source = _sourceHolder.AddComponent<LanguageSource>();
            _langData = _source.SourceData;

            ResetData();

            _instances.Add(this);

            if (InjectionStarted)
            {
                PerformInjection();
            }
        }

        public void ResetData()
        {
            _langData.ClearAllData();

            foreach (DVLanguage language in Enum.GetValues(typeof(DVLanguage)))
            {
                _langData.AddLanguage(language.Name(), language.Code());
                _langData.EnableLanguage(language.Name(), true);
            }
        }

        public void Reload()
        {
            ResetData();

            foreach (var file in _csvFiles)
            {
                switch (file.Type)
                {
                    case CsvFileInfo.SourceType.Local:
                        AddTranslationsFromCsv(file.Path);
                        break;
                    case CsvFileInfo.SourceType.URL:
                        AddTranslationsFromWebCsv(file.Path);
                        break;
                }
            }

            foreach (var kvp in _originalData)
            {
                AddTranslations(kvp.Key, kvp.Value, true);
            }
        }

        public void AddTranslationsFromCsv(string csvPath, bool isOverride = false)
        {
            try
            {
                string csvText = LocalizationReader.ReadCSVfile(csvPath, Encoding.UTF8);
                _langData.Import_CSV(string.Empty, csvText, eSpreadsheetUpdateMode.Merge);

                if (!isOverride && !_csvFiles.Any(f => f.Path == csvPath))
                {
                    _csvFiles.Add(new CsvFileInfo(CsvFileInfo.SourceType.Local, csvPath));
                }
            }
            catch (Exception ex)
            {
                LangHelperMain.Error($"Failed to load csv translations @ {csvPath}", ex);
            }
        }

        public void AddTranslationsFromWebCsv(string url)
        {
            _pendingWebRequests.Add(url);
            _source.StartCoroutine(AddWebCsvCoro(url));
        }

        private IEnumerator AddWebCsvCoro(string url)
        {
            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            string cachePath = Path.Combine(LangHelperMain.CacheDirPath, $"{Id}.csv");

            if (request.isNetworkError || request.responseCode != 200)
            {
                if (LangHelperMain.Settings.UseCache && File.Exists(cachePath))
                {
                    LangHelperMain.Warning($"Failed to fetch web csv translations @ {url}, using cached");
                    _pendingWebRequests.Remove(url);
                    AddTranslationsFromCsv(cachePath);
                    yield break;
                }

                LangHelperMain.Error($"Failed to fetch web csv translations @ {url}");
                _pendingWebRequests.Remove(url);
                yield break;
            }

            string downloaded = request.downloadHandler.text.Replace("\r", string.Empty);
            _langData.Import_CSV(string.Empty, downloaded, eSpreadsheetUpdateMode.Merge);

            if (!_csvFiles.Any(f => f.Path == url))
            {
                _csvFiles.Add(new CsvFileInfo(CsvFileInfo.SourceType.URL, url));
            }

            _pendingWebRequests.Remove(url);
            LangHelperMain.Log($"Successfully fetched web csv translations from {url}");

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

        public void AddTranslations(string key, TranslationData data, bool isOverride = false)
        {
            AddTranslations(key, data.Items, isOverride);
        }

        public void AddTranslations(string key, IEnumerable<TranslationItem> items, bool isOverride = false)
        {
            if (!isOverride) RegisterOriginalData(key, items.ToArray());

            var term = _langData.AddTerm(key);
            
            foreach (var item in items)
            {
                int idx = _langData.GetLanguageIndexFromCode(item.Language.Code());
                term.SetTranslation(idx, item.Value);
            }
        }

        public void AddTranslation(string key, DVLanguage language, string value, bool isOverride = false)
        {
            if (!isOverride) RegisterOriginalData(key, new TranslationItem(language, value));

            var term = _langData.AddTerm(key);

            int idx = _langData.GetLanguageIndexFromCode(language.Code());
            term.SetTranslation(idx, value);
        }

        private void RegisterOriginalData(string key, params TranslationItem[] items)
        {
            if (!_originalData.TryGetValue(key, out var data))
            {
                data = new TranslationData()
                {
                    Items = items.ToList(),
                };
                _originalData.Add(key, data);
                return;
            }

            data.Items.AddRange(items);
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

        private readonly struct CsvFileInfo
        {
            public readonly SourceType Type;
            public readonly string Path;
            
            public CsvFileInfo(SourceType type, string path)
            {
                Type = type;
                Path = path;
            }

            public enum SourceType
            {
                Local,
                URL,
            }
        }
    }
}
