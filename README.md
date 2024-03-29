# DV Language Helper
Common library that allows creators to provide translations for their mods

## Adding a Language Source:
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
> [!TIP]
> A Template for making your own translation spreadsheet is available [here](https://docs.google.com/spreadsheets/d/17bsnt45jNpnuvIsjK8WJ0HvYUZ-UTuY7yvxYcPvRe7Y/edit?usp=sharing). Simply click `File > Make a Copy` to duplicate it in your own drive. You can use a google sheet as your translation source by setting it to "Anyone with link can view", then copying the link and swapping the "/edit" section for "/export?format=csv":
>
> _https://docs.google.com/spreadsheets/d/0000000000000000000000000/edit?usp=sharing_
>
> becomes
>
> _https://docs.google.com/spreadsheets/d/0000000000000000000000000/export?format=csv_
>
> The template CSV file is also included in the Data folder of this repository. You should keep the header rows as they are, as these need to match the game languages.

In addition, it is recommended to include an offline copy of your translation spreadsheet with the release files, so that anyone behind a restrictive firewall can still use translations even if slightly outdated. To do this, use the link you created in the step above to save the file to your disk, and include it alongside your mod DLL when zipping it up. Then, directly above the `AddTranslationsFromWebCsv()` line, call `AddTranslationsFromCsv()` on the local copy. The web version will override the local version if the request is successful:

```csharp
translations.AddTranslationsFromCsv(Path.Combine(modEntry.Path, "offline_translations.csv"));
string url = "https://docs.google.com/spreadsheets/d/0000000000000000000000000/export?format=csv";
translations.AddTranslationsFromWebCsv(url);
```

## Using translated values at runtime:
You must include a reference to DV.Localization.dll in your project to use the localization API.
### Simple value:
```csharp
using DV.Localization;
...
string translated = LocalizationAPI.L("example_key");
```

### Translations with variable parameters:
When creating a translation string, you can include spots where a variable value will be inserted by the API (a "template string"). Each variable gets represented by an index that starts at 0, and surrounded by 2 sets of braces:
`Transport a train from {[0]} to {[1]}`
Then, in the code of your mod, you can translate the string as usual and include the parameter values as extra arguments. The order of arguments must match the indices in your template string:
```csharp
string translated = LocalizationAPI.L("example_key", source.yardId, destination.yardId);
```
