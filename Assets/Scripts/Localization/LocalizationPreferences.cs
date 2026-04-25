using UnityEngine;

namespace ShatteredForge.Localization
{
    public static class LocalizationPreferences
    {
        public const string SelectedLocaleCodeKey = "sf.selected_locale_code";

        public static string GetSelectedLocaleCodeOrEmpty()
        {
            return PlayerPrefs.GetString(SelectedLocaleCodeKey, string.Empty);
        }

        public static void SetSelectedLocaleCode(string localeCode)
        {
            PlayerPrefs.SetString(SelectedLocaleCodeKey, localeCode);
            PlayerPrefs.Save();
        }
    }
}
