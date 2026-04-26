using System;
using UnityEngine;

namespace ShatteredForge.Localization
{
    /// <summary>
    /// Resolves UI language before Unity Localization tables are ready: saved locale (PlayerPrefs) first, then OS language.
    /// </summary>
    public static class LocalePreferencePreview
    {
        public static bool PreferCyrillicUi()
        {
            var code = LocalizationPreferences.GetSelectedLocaleCodeOrEmpty();
            if (!string.IsNullOrWhiteSpace(code))
            {
                if (IsCyrillicLocaleCode(code))
                {
                    return true;
                }

                if (code.Equals("en", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return Application.systemLanguage is SystemLanguage.Russian
                or SystemLanguage.Ukrainian
                or SystemLanguage.Belarusian;
        }

        private static bool IsCyrillicLocaleCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            return code.Equals("ru", StringComparison.OrdinalIgnoreCase)
                   || code.StartsWith("ru-", StringComparison.OrdinalIgnoreCase)
                   || code.Equals("uk", StringComparison.OrdinalIgnoreCase)
                   || code.Equals("be", StringComparison.OrdinalIgnoreCase);
        }
    }
}
