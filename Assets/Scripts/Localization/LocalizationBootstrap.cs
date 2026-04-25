using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ShatteredForge.Localization
{
    /// <summary>
    /// Ensures Unity Localization selects a predictable default (ru) unless the player chose otherwise.
    /// Attach to the same bootstrap object as the main menu (executes in Awake before other UI scripts on the same GO only if scripted execution order is configured; otherwise rely on Unity script order on the object).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class LocalizationBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // IMGUI uses synchronous localized string evaluation in this prototype.
            // Note: synchronous init is not supported on WebGL per package docs.
            LocalizationSettings.InitializeSynchronously = true;

            ApplyLocalePreference();
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
