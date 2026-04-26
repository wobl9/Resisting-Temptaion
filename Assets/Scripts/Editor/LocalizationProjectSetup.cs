using System.Collections.Generic;
using System.IO;
using ShatteredForge.Localization;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace ShatteredForge.EditorTools
{
    public static class LocalizationProjectSetup
    {
        private const string SettingsAssetPath = "Assets/Localization/Localization Settings.asset";
        private const string LocalesFolder = "Assets/Localization/Locales";
        private const string StringTablesFolder = "Assets/Localization/StringTables";
        private const string MenuScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Shattered Forge/Localization/Initialize (ru default + UI table)")]
        public static void Initialize()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsAssetPath) ?? "Assets/Localization");
            Directory.CreateDirectory(LocalesFolder);
            Directory.CreateDirectory(StringTablesFolder);

            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;

            var ru = FindOrCreateLocaleAsset("ru", "Russian (ru)");
            var en = FindOrCreateLocaleAsset("en", "English (en)");

            LocalizationEditorSettings.AddLocale(ru, createUndo: false);
            LocalizationEditorSettings.AddLocale(en, createUndo: false);

            // Startup selectors: remember player choice if present, otherwise fall back to ru, then en.
            LocalizationSettings.StartupLocaleSelectors.Clear();
            LocalizationSettings.StartupLocaleSelectors.Add(new PlayerPrefLocaleSelector());
            LocalizationSettings.StartupLocaleSelectors.Add(new SystemLocaleSelector());
            LocalizationSettings.StartupLocaleSelectors.Add(new SpecificLocaleSelector { LocaleId = ru.Identifier });
            LocalizationSettings.StartupLocaleSelectors.Add(new SpecificLocaleSelector { LocaleId = en.Identifier });

            EnsureUiStringTable(ru, en);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EnsureMenuSceneHasBootstrap();

            Debug.Log("Localization initialized. Default startup locale is forced to 'ru' via SpecificLocaleSelector fallback ordering + runtime bootstrap.");
        }

        private static Locale FindOrCreateLocaleAsset(string code, string displayName)
        {
            var assetPath = $"{LocalesFolder}/Locale_{code}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Locale>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var locale = Locale.CreateLocale(code);
            locale.LocaleName = displayName;
            AssetDatabase.CreateAsset(locale, assetPath);
            return locale;
        }

        private static void EnsureUiStringTable(Locale ru, Locale en)
        {
            StringTableCollection collection = null;
            var collections = LocalizationEditorSettings.GetStringTableCollections();
            for (var i = 0; i < collections.Count; i++)
            {
                if (collections[i].TableCollectionName == UiKeys.Table)
                {
                    collection = collections[i];
                    break;
                }
            }

            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    UiKeys.Table,
                    StringTablesFolder,
                    new List<Locale> { ru, en });
            }

            void Set(string key, string ruValue, string enValue)
            {
                var ruTable = collection.GetTable(ru.Identifier) as StringTable;
                var enTable = collection.GetTable(en.Identifier) as StringTable;
                if (ruTable == null || enTable == null)
                {
                    return;
                }

                ruTable.AddEntry(key, ruValue);
                enTable.AddEntry(key, enValue);

                EditorUtility.SetDirty(ruTable);
                EditorUtility.SetDirty(enTable);
                EditorUtility.SetDirty(collection.SharedData);
            }

            Set(UiKeys.GameTitle, "SHATTERED FORGE", "SHATTERED FORGE");
            Set(UiKeys.BootOpeningLine, "Кузня пробуждается…", "The forge stirs…");
            Set(UiKeys.Welcome, "Добро пожаловать в Shattered Forge", "Welcome to Shattered Forge");

            Set(UiKeys.NewGame, "Новая игра", "New Game");
            Set(UiKeys.ContinueGame, "Продолжить игру", "Continue Game");
            Set(UiKeys.Settings, "Настройки", "Settings");
            Set(UiKeys.QuitGame, "Выход из игры", "Quit Game");

            Set(UiKeys.NoActiveProfile, "Нет активного профиля. Выберите профиль.", "No active profile. Please select a profile.");
            Set(UiKeys.SelectProfile, "Выбрать профиль", "Select profile");

            Set(UiKeys.ProfileButton, "Профиль: {0}", "Profile: {0}");
            Set(UiKeys.ProfileMenuTitle, "Профиль", "Profile");
            Set(UiKeys.ProfileMenuCurrent, "Текущий: {0}", "Current: {0}");
            Set(UiKeys.ProfileMenuOpened, "Меню профиля.", "Profile menu opened.");
            Set(UiKeys.SwitchProfile, "Сменить профиль", "Switch profile");
            Set(UiKeys.CreateNewProfile, "Создать новый профиль", "Create new profile");

            Set(UiKeys.ProfilesTitle, "Выбор профиля", "Select profile");
            Set(UiKeys.ProfilesEmpty, "Профилей нет. Создайте новую игру.", "No profiles yet. Start a new game.");
            Set(UiKeys.ProfileSelected, "Профиль выбран: {0}", "Profile selected: {0}");
            Set(UiKeys.ActiveSuffix, "  (АКТИВНЫЙ)", "  (ACTIVE)");
            Set(UiKeys.Delete, "Удалить", "Delete");
            Set(UiKeys.DeleteProfilePrompt, "Удалить профиль '{0}'?", "Delete profile '{0}'?");
            Set(UiKeys.YesDelete, "Да, удалить", "Yes, delete");
            Set(UiKeys.Cancel, "Отмена", "Cancel");

            Set(UiKeys.NewProfileTitle, "Новая игра", "New Game");
            Set(UiKeys.ProfileNameLabel, "Имя профиля:", "Profile name:");
            Set(UiKeys.ConfirmAndStart, "Подтвердить и начать", "Confirm and start");
            Set(UiKeys.Back, "Назад", "Back");

            Set(UiKeys.NewExpeditionTitle, "Новая вылазка", "New expedition");
            Set(UiKeys.NewExpeditionProfileLine, "Профиль: {0}", "Profile: {0}");
            Set(UiKeys.NewExpeditionPrompt, "У вас есть активная вылазка. Начать новую вылазку и заменить текущую?",
                "You have an active expedition. Start a new expedition and replace the current one?");
            Set(UiKeys.NewExpeditionConfirm, "Да, начать новую вылазку", "Yes, start a new expedition");

            Set(UiKeys.SettingsTitle, "Настройки", "Settings");
            Set(UiKeys.VolumeLabel, "Громкость: {0}%", "Volume: {0}%");
            Set(UiKeys.FullscreenOn, "Полный экран: ВКЛ", "Fullscreen: ON");
            Set(UiKeys.FullscreenOff, "Полный экран: ВЫКЛ", "Fullscreen: OFF");
            Set(UiKeys.ResolutionLabel, "Разрешение: {0}", "Resolution: {0}");
            Set(UiKeys.NextResolution, "Следующее разрешение", "Next resolution");
            Set(UiKeys.LanguageLabel, "Язык: {0}", "Language: {0}");
            Set(UiKeys.NextLanguage, "Сменить язык", "Change language");

            Set(UiKeys.DeleteCurrentProfile, "Удалить текущий профиль", "Delete current profile");
            Set(UiKeys.DeleteActiveProfilePrompt, "Удалить активный профиль '{0}'?", "Delete active profile '{0}'?");
            Set(UiKeys.NoActiveProfileToDelete, "Нет активного профиля для удаления.", "No active profile to delete.");

            Set(UiKeys.ErrorNoExpeditionToContinue, "Нет активной вылазки для продолжения.", "No active expedition to continue.");
            Set(UiKeys.ErrorGameplaySceneMissing, "Не удалось запустить игру: не настроена сцена геймплея.", "Failed to start: gameplay scene is not configured.");
            Set(UiKeys.ErrorHubSceneMissing, "Не удалось открыть лагерь: сцена лагеря не в Build Settings.", "Failed to open camp: hub scene is missing from Build Settings.");
            Set(UiKeys.LoadingGameplay, "Загрузка...", "Loading...");
            Set(UiKeys.LoadingErrorTitle, "Ошибка загрузки сцены", "Scene load error");
            Set(UiKeys.LoadingBackToMenu, "В меню", "Back to menu");
            Set(UiKeys.ErrorDeleteNotFound, "Удаление не удалось: профиль не найден.", "Delete failed: profile not found.");

            Set(UiKeys.StatusProfileDeleted, "Профиль удалён.", "Profile deleted.");
            Set(UiKeys.StatusProfileCreated, "Создан профиль: {0}", "Created profile: {0}");

            Set(UiKeys.CommonNone, "нет", "none");
            Set(UiKeys.CommonUnknown, "неизвестно", "unknown");
            Set(UiKeys.CommonNotApplicable, "н/д", "n/a");

            Set(UiKeys.DefaultNewProfileName, "Новый охотник", "New Hunter");

            EditorUtility.SetDirty(collection);
        }

        private static void EnsureMenuSceneHasBootstrap()
        {
            var previousScenePath = SceneManager.GetActiveScene().path;
            var menuScene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            var menu = Object.FindFirstObjectByType<ShatteredForge.Menu.MainMenuController>();
            if (menu == null)
            {
                Debug.LogWarning($"OpenScene failed to find {nameof(ShatteredForge.Menu.MainMenuController)} in {MenuScenePath}.");
            }
            else
            {
                var go = menu.gameObject;
                if (go.GetComponent<LocalizationBootstrap>() == null)
                {
                    go.AddComponent<LocalizationBootstrap>();
                    EditorSceneManager.MarkSceneDirty(menuScene);
                    EditorSceneManager.SaveScene(menuScene);
                }
            }

            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }
    }
}
