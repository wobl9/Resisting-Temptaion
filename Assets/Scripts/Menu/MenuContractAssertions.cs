namespace ShatteredForge.Menu
{
    public readonly struct MenuContractSnapshot
    {
        public readonly int profileCount;
        public readonly bool hasActiveProfileId;
        public readonly bool hasActiveExpeditionForActiveProfile;

        public readonly bool expectFirstLaunchThreeButtonLayout;
        public readonly bool expectProfilePicker;
        public readonly bool expectContinueVisible;
        public readonly bool expectProfileEntryVisible;

        public MenuContractSnapshot(
            int profileCount,
            bool hasActiveProfileId,
            bool hasActiveExpeditionForActiveProfile,
            bool expectFirstLaunchThreeButtonLayout,
            bool expectProfilePicker,
            bool expectContinueVisible,
            bool expectProfileEntryVisible)
        {
            this.profileCount = profileCount;
            this.hasActiveProfileId = hasActiveProfileId;
            this.hasActiveExpeditionForActiveProfile = hasActiveExpeditionForActiveProfile;
            this.expectFirstLaunchThreeButtonLayout = expectFirstLaunchThreeButtonLayout;
            this.expectProfilePicker = expectProfilePicker;
            this.expectContinueVisible = expectContinueVisible;
            this.expectProfileEntryVisible = expectProfileEntryVisible;
        }
    }

    public static class MenuContractAssertions
    {
        public static MenuContractSnapshot BuildSnapshot(ProfileStorageService profiles)
        {
            var list = profiles.LoadProfiles(out var activeId);
            var profileCount = list.Count;
            var hasActive = !string.IsNullOrEmpty(activeId);
            var hasExpedition = hasActive && profiles.HasActiveExpedition(activeId);

            var firstLaunch = profileCount == 0;
            var picker = profileCount > 0 && !hasActive;
            var profileEntry = profileCount > 0 && hasActive;
            var continueVisible = profileCount > 0 && hasActive && hasExpedition;

            return new MenuContractSnapshot(
                profileCount,
                hasActive,
                hasExpedition,
                firstLaunch,
                picker,
                continueVisible,
                profileEntry);
        }
    }
}
