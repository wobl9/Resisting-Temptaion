using System.Collections.Generic;

namespace ShatteredForge.Menu
{
    public interface IProfileStorage
    {
        List<ProfileSummary> LoadProfiles(out string activeProfileId);

        string CreateProfile(string displayName);

        void SetActiveProfile(string profileId);

        bool HasAnyProfile();

        bool TryLoadProfile(string profileId, out ProfileData profile);

        bool HasActiveExpedition(string profileId);

        void SaveProfile(ProfileData profile);

        bool DeleteProfile(string profileId);
    }
}
