using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ShatteredForge.Localization
{
    /// <summary>
    /// Loads Unity Localization tables in the background; UI uses <see cref="MenuWarmStrings"/> until <see cref="AreTablesReady"/>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class LocalizationBootstrap : MonoBehaviour
    {
        /// <summary>
        /// True after <see cref="LocalizationSettings.InitializationOperation"/> completes and locale prefs are applied.
        /// </summary>
        public static bool AreTablesReady { get; private set; }

        private void Awake()
        {
            AreTablesReady = false;
            LocalizationSettings.InitializeSynchronously = false;
            StartCoroutine(LoadTablesAndApplyLocaleCoroutine());
        }

        private void OnDestroy()
        {
            AreTablesReady = false;
        }

        private static IEnumerator LoadTablesAndApplyLocaleCoroutine()
        {
            if (!LocalizationSettings.HasSettings)
            {
                AreTablesReady = true;
                yield break;
            }

            yield return LocalizationSettings.InitializationOperation;

            LocalizationSettings.InitializeSynchronously = true;
            ApplyLocalePreference();
            AreTablesReady = true;
        }

        private static void ApplyLocalePreference()
        {
            if (!LocalizationSettings.HasSettings)
            {
                return;
            }

            var saved = LocalizationPreferences.GetSelectedLocaleCodeOrEmpty();
            Locale target = null;

            if (!string.IsNullOrWhiteSpace(saved))
            {
                target = LocalizationSettings.AvailableLocales.GetLocale(saved);
            }

            if (target == null)
            {
                target = LocalizationSettings.AvailableLocales.GetLocale("ru")
                         ?? LocalizationSettings.AvailableLocales.GetLocale("en");
            }

            if (target == null)
            {
                return;
            }

            LocalizationSettings.SelectedLocale = target;
            LocalizationPreferences.SetSelectedLocaleCode(target.Identifier.Code);
        }
    }
}
