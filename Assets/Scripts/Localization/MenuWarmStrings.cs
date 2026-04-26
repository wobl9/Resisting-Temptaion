using System;
using System.Globalization;

namespace ShatteredForge.Localization
{
    /// <summary>
    /// Offline UI copy keyed by <see cref="UiKeys"/> — same wording as string tables, language from prefs then OS.
    /// Used until Unity Localization finishes loading tables (no blocking splash).
    /// </summary>
    public static class MenuWarmStrings
    {
        public static string GameTitle => "SHATTERED FORGE";

        public static string Welcome => P(
            "Добро пожаловать в Shattered Forge",
            "Welcome to Shattered Forge");

        public static string DefaultNewProfileName => P("Новый охотник", "New Hunter");

        public static string CommonNone => P("нет", "none");

        public static string CommonUnknown => P("неизвестно", "unknown");

        public static string CommonNotApplicable => P("н/д", "n/a");

        public static string UiFallback(string entryKey)
        {
            return entryKey switch
            {
                UiKeys.GameTitle => GameTitle,
                UiKeys.BootOpeningLine => P("Кузня пробуждается…", "The forge stirs…"),
                UiKeys.Welcome => Welcome,
                UiKeys.NewGame => P("Новая игра", "New Game"),
                UiKeys.ContinueGame => P("Продолжить игру", "Continue Game"),
                UiKeys.Settings => P("Настройки", "Settings"),
                UiKeys.QuitGame => P("Выход из игры", "Quit Game"),
                UiKeys.NoActiveProfile => P("Нет активного профиля. Выберите профиль.", "No active profile. Please select a profile."),
                UiKeys.SelectProfile => P("Выбрать профиль", "Select profile"),
                UiKeys.ProfileMenuTitle => P("Профиль", "Profile"),
                UiKeys.ProfileMenuOpened => P("Меню профиля.", "Profile menu opened."),
                UiKeys.SwitchProfile => P("Сменить профиль", "Switch profile"),
                UiKeys.CreateNewProfile => P("Создать новый профиль", "Create new profile"),
                UiKeys.ProfilesTitle => P("Выбор профиля", "Select profile"),
                UiKeys.ProfilesEmpty => P("Профилей нет. Создайте новую игру.", "No profiles yet. Start a new game."),
                UiKeys.ActiveSuffix => P("  (АКТИВНЫЙ)", "  (ACTIVE)"),
                UiKeys.Delete => P("Удалить", "Delete"),
                UiKeys.YesDelete => P("Да, удалить", "Yes, delete"),
                UiKeys.Cancel => P("Отмена", "Cancel"),
                UiKeys.NewProfileTitle => P("Новая игра", "New Game"),
                UiKeys.ProfileNameLabel => P("Имя профиля:", "Profile name:"),
                UiKeys.ConfirmAndStart => P("Подтвердить и начать", "Confirm and start"),
                UiKeys.Back => P("Назад", "Back"),
                UiKeys.NewExpeditionTitle => P("Новая вылазка", "New expedition"),
                UiKeys.NewExpeditionPrompt => P(
                    "У вас есть активная вылазка. Начать новую вылазку и заменить текущую?",
                    "You have an active expedition. Start a new expedition and replace the current one?"),
                UiKeys.NewExpeditionConfirm => P("Да, начать новую вылазку", "Yes, start a new expedition"),
                UiKeys.SettingsTitle => P("Настройки", "Settings"),
                UiKeys.NextResolution => P("Следующее разрешение", "Next resolution"),
                UiKeys.NextLanguage => P("Сменить язык", "Change language"),
                UiKeys.DeleteCurrentProfile => P("Удалить текущий профиль", "Delete current profile"),
                UiKeys.NoActiveProfileToDelete => P("Нет активного профиля для удаления.", "No active profile to delete."),
                UiKeys.ErrorNoExpeditionToContinue => P("Нет активной вылазки для продолжения.", "No active expedition to continue."),
                UiKeys.ErrorGameplaySceneMissing => P(
                    "Не удалось запустить игру: не настроена сцена геймплея.",
                    "Failed to start: gameplay scene is not configured."),
                UiKeys.LoadingGameplay => P("Загрузка...", "Loading..."),
                UiKeys.LoadingErrorTitle => P("Ошибка загрузки сцены", "Scene load error"),
                UiKeys.LoadingBackToMenu => P("В меню", "Back to menu"),
                UiKeys.ErrorDeleteNotFound => P("Удаление не удалось: профиль не найден.", "Delete failed: profile not found."),
                UiKeys.StatusProfileDeleted => P("Профиль удалён.", "Profile deleted."),
                UiKeys.CommonNone => CommonNone,
                UiKeys.CommonUnknown => CommonUnknown,
                UiKeys.CommonNotApplicable => CommonNotApplicable,
                UiKeys.DefaultNewProfileName => DefaultNewProfileName,
                _ => entryKey
            };
        }

        public static string UiFormatFallback(string entryKey, params object[] args)
        {
            var fmt = entryKey switch
            {
                UiKeys.ProfileButton => P("Профиль: {0}", "Profile: {0}"),
                UiKeys.ProfileMenuCurrent => P("Текущий: {0}", "Current: {0}"),
                UiKeys.ProfileSelected => P("Профиль выбран: {0}", "Profile selected: {0}"),
                UiKeys.DeleteProfilePrompt => P("Удалить профиль '{0}'?", "Delete profile '{0}'?"),
                UiKeys.NewExpeditionProfileLine => P("Профиль: {0}", "Profile: {0}"),
                UiKeys.VolumeLabel => P("Громкость: {0}%", "Volume: {0}%"),
                UiKeys.ResolutionLabel => P("Разрешение: {0}", "Resolution: {0}"),
                UiKeys.LanguageLabel => P("Язык: {0}", "Language: {0}"),
                UiKeys.DeleteActiveProfilePrompt => P("Удалить активный профиль '{0}'?", "Delete active profile '{0}'?"),
                UiKeys.StatusProfileCreated => P("Создан профиль: {0}", "Created profile: {0}"),
                _ => null
            };

            if (fmt == null)
            {
                return UiFallback(entryKey);
            }

            return args is { Length: > 0 }
                ? string.Format(CultureInfo.InvariantCulture, fmt, args)
                : fmt;
        }

        private static string P(string ru, string en)
        {
            return LocalePreferencePreview.PreferCyrillicUi() ? ru : en;
        }
    }
}
