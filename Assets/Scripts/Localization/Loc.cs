using UnityEngine;
using UnityEngine.Localization;

namespace ShatteredForge.Localization
{
    public static class Loc
    {
        public static string Ui(string entryKey)
        {
            try
            {
                var localized = new LocalizedString(UiKeys.Table, entryKey);
                return localized.GetLocalizedString();
            }
            catch
            {
                return entryKey;
            }
        }

        public static string UiFormat(string entryKey, params object[] args)
        {
            try
            {
                var localized = new LocalizedString(UiKeys.Table, entryKey);
                return localized.GetLocalizedString(args);
            }
            catch
            {
                return entryKey;
            }
        }
    }
}
