using System;
using System.Collections.Generic;

namespace DVLangHelper.Data
{
    [Serializable]
    public class TranslationData
    {
        public List<TranslationItem> Items;

        public TranslationData()
        {
            Items = new List<TranslationItem>() { new TranslationItem() };
        }

        public void Validate()
        {
            if (Items == null || Items.Count == 0)
            {
                Items = new List<TranslationItem>() { new TranslationItem() };
            }
        }

        public static TranslationData Default(string englishName = "") => new TranslationData()
        {
            Items = new List<TranslationItem>() 
            { 
                new TranslationItem() { Value = englishName } 
            }
        };

        public void Concat(TranslationData other)
        {
            Items.AddRange(other.Items);
        }
    }

    [Serializable]
    public class TranslationItem
    {
        public DVLanguage Language = DVLanguage.English;
        public string Value = string.Empty;

        public TranslationItem() { }

        public TranslationItem(DVLanguage language, string value)
        {
            Language = language;
            Value = value;
        }
    }
}
