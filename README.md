# DV Language Helper
Common library that allows creators to provide translations for their mods

## Usage:
### Create an injector object for your mod:
```csharp
var translations = new TranslationInjector("cc.foxden.passenger_jobs");
```

### Create your own translation data via code:
```csharp
var data = new TranslationData()
{
  Items = new List<TranslationItem>()
  {
    new TranslationItem(DVLanguage.English, "Example"),
    new TranslationItem(DVLanguage.Spanish, "Ejemplo")
  }
};

translations.AddTranslations("example_key", data);

translations.AddTranslation("another_key", DVLanguage.Swedish, "Exempel");
```

### Add translation items from a local CSV file:
```csharp
string path = Path.Combine(modEntry.Path, "translations.csv");
translations.AddTranslationsFromCsv(path);
```

### Add translation items from an online CSV:
```csharp
string url = "https://docs.google.com/spreadsheets/d/0000000000000000000000000/export?format=csv";
translations.AddTranslationsFromWebCsv(url);
```
You can use a google sheet as your translation source by setting it to "Anyone with link can view", then copying the link and swapping the "/edit" section for "/export?format=csv":

_https://docs.google.com/spreadsheets/d/0000000000000000000000000/edit?usp=sharing_

becomes

_https://docs.google.com/spreadsheets/d/0000000000000000000000000/export?format=csv_

There is a template CSV file in the Data folder of the repository that can be used as a starting point for your own translation file. You should keep the header rows as they are, as these need to match the game languages.
