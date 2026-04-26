using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShatteredForge.Menu
{
    public sealed class LocalJsonProfileStorage : IProfileStorage
    {
        private const string RootFolderName = "ShatteredForge";
        private const string ProfilesFolderName = "Profiles";
        private const string IndexFileName = "profiles_index.json";

        private string RootFolder => Path.Combine(Application.persistentDataPath, RootFolderName);
        private string ProfilesFolder => Path.Combine(RootFolder, ProfilesFolderName);
        private string IndexPath => Path.Combine(RootFolder, IndexFileName);

        public List<ProfileSummary> LoadProfiles(out string activeProfileId)
        {
            EnsureFolders();
            var index = LoadIndex();
            activeProfileId = index.activeProfileId;
            return index.profiles ?? new List<ProfileSummary>();
        }

        public string CreateProfile(string displayName)
        {
            EnsureFolders();
            var safeName = string.IsNullOrWhiteSpace(displayName) ? "New Hunter" : displayName.Trim();
            var profileId = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow.ToString("O");

            var profileData = new ProfileData
            {
                profileId = profileId,
                profileName = safeName,
                createdAtUtc = now,
                updatedAtUtc = now,
                profileRevision = 0
            };

            var profilePath = GetProfilePath(profileId);
            File.WriteAllText(profilePath, JsonUtility.ToJson(profileData, true));

            var index = LoadIndex();
            index.profiles ??= new List<ProfileSummary>();
            index.profiles.Add(new ProfileSummary
            {
                id = profileId,
                displayName = safeName,
                createdAtUtc = now,
                lastPlayedAtUtc = now
            });
            index.activeProfileId = profileId;
            SaveIndex(index);
            return profileId;
        }

        public void SetActiveProfile(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return;
            }

            EnsureFolders();
            var index = LoadIndex();
            index.activeProfileId = profileId;

            if (index.profiles != null)
            {
                foreach (var summary in index.profiles)
                {
                    if (summary.id == profileId)
                    {
                        summary.lastPlayedAtUtc = DateTime.UtcNow.ToString("O");
                        break;
                    }
                }
            }

            SaveIndex(index);
        }

        public bool HasAnyProfile()
        {
            var profiles = LoadProfiles(out _);
            return profiles.Count > 0;
        }

        public bool TryLoadProfile(string profileId, out ProfileData profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return false;
            }

            EnsureFolders();
            var path = GetProfilePath(profileId);
            if (!File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var data = JsonUtility.FromJson<ProfileData>(json);
            if (data == null || string.IsNullOrWhiteSpace(data.profileId))
            {
                return false;
            }

            profile = data;
            return true;
        }

        public bool HasActiveExpedition(string profileId)
        {
            if (!TryLoadProfile(profileId, out var profile))
            {
                return false;
            }

            return profile.hasActiveExpedition;
        }

        public void SaveProfile(ProfileData profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
            {
                return;
            }

            profile.updatedAtUtc = DateTime.UtcNow.ToString("O");
            profile.profileRevision++;
            PersistProfileSnapshot(profile);
        }

        /// <summary>
        /// Writes profile JSON without bumping <see cref="ProfileData.profileRevision"/> (used by remote mirror layer).
        /// </summary>
        internal void PersistProfileSnapshot(ProfileData profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
            {
                return;
            }

            EnsureFolders();
            var path = GetProfilePath(profile.profileId);
            File.WriteAllText(path, JsonUtility.ToJson(profile, true));
        }

        public bool DeleteProfile(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return false;
            }

            EnsureFolders();
            var index = LoadIndex();
            var profiles = index.profiles ?? new List<ProfileSummary>();

            var removed = false;
            for (var i = profiles.Count - 1; i >= 0; i--)
            {
                if (profiles[i].id != profileId)
                {
                    continue;
                }

                profiles.RemoveAt(i);
                removed = true;
            }

            var profilePath = GetProfilePath(profileId);
            if (File.Exists(profilePath))
            {
                File.Delete(profilePath);
                removed = true;
            }

            if (!removed)
            {
                return false;
            }

            if (index.activeProfileId == profileId)
            {
                index.activeProfileId = profiles.Count > 0 ? profiles[0].id : string.Empty;
            }

            index.profiles = profiles;
            SaveIndex(index);
            return true;
        }

        internal ProfileIndexData LoadIndex()
        {
            if (!File.Exists(IndexPath))
            {
                return new ProfileIndexData();
            }

            var json = File.ReadAllText(IndexPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ProfileIndexData();
            }

            var index = JsonUtility.FromJson<ProfileIndexData>(json);
            return index ?? new ProfileIndexData();
        }

        internal void SaveIndex(ProfileIndexData index)
        {
            EnsureFolders();
            File.WriteAllText(IndexPath, JsonUtility.ToJson(index, true));
        }

        internal string GetProfilePath(string profileId)
        {
            return Path.Combine(ProfilesFolder, $"profile_{profileId}.json");
        }

        private void EnsureFolders()
        {
            if (!Directory.Exists(RootFolder))
            {
                Directory.CreateDirectory(RootFolder);
            }

            if (!Directory.Exists(ProfilesFolder))
            {
                Directory.CreateDirectory(ProfilesFolder);
            }
        }
    }
}
