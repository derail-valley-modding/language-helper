using DV.Localization;
using HarmonyLib;
using System.Collections;

namespace DVLangHelper.Runtime
{
    [HarmonyPatch(typeof(LocalizationLoader))]
    public static class LocalizationLoaderPatch
    {
        [HarmonyPatch("Start")]
        public static void Postfix(ref IEnumerator __result)
        {
            __result = WrapEnumerator(__result);
        }

        private static IEnumerator WrapEnumerator(IEnumerator source)
        {
            while (source.MoveNext())
            {
                yield return source.Current;
            }

            TranslationInjector.StartInjection();
        }
    }
}
