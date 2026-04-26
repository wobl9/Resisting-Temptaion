using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ShatteredForge.Localization
{
    public static class Loc
    {
        public static string Ui(string entryKey)
        {
            try
            {
                if (LocalizationSettings.HasSettings
                    && !LocalizationSettings.InitializationOperation.IsDone)
                {
                    return MenuWarmStrings.UiFallback(entryKey);
                }

                var localized = new LocalizedString(UiKeys.Table, entryKey);
                return localized.GetLocalizedString();
            }
            catch
            {
                return MenuWarmStrings.UiFallback(entryKey);
            }
        }

        public static string UiFormat(string entryKey, params object[] args)
        {
            try
            {
                if (LocalizationSettings.HasSettings
                    && !LocalizationSettings.InitializationOperation.IsDone)
                {
                    return MenuWarmStrings.UiFormatFallback(entryKey, args);
                }

                var localized = new LocalizedString(UiKeys.Table, entryKey);
                return localized.GetLocalizedString(args);
            }
            catch
            {
                return MenuWarmStrings.UiFormatFallback(entryKey, args);
            }
        }
    }
}
