using System;

namespace ShatteredForge.UI
{
    public interface IPauseMenuView
    {
        void EnsureBuilt();
        void SetOpen(bool open);
        void ShowMainPage();
        void ShowSettingsPage(float volume, bool fullscreen, string resolutionText);
        void Bind(PauseMenuBinding binding);
        void Configure(PauseMenuConfig config);
    }

    public sealed class PauseMenuBinding
    {
        public Action onContinue;
        public Action onOpenSettings;
        public Action onExit;
        public Action<float> onVolumeChanged;
        public Action onToggleFullscreen;
        public Action onNextResolution;
        public Action onBackFromSettings;
    }

    public sealed class PauseMenuConfig
    {
        public string title;
        public string continueLabel;
        public string settingsLabel;
        public string exitLabel;
        public bool showSettingsButton = true;
        public bool showExitButton = true;
    }
}
