using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShatteredForge.Menu
{
    [Serializable]
    public class ProfileSummary
    {
        public string id;
        public string displayName;
        public string createdAtUtc;
        public string lastPlayedAtUtc;
    }

    [Serializable]
    internal class ProfileIndexData
    {
        public string activeProfileId;
        public List<ProfileSummary> profiles = new();
    }

    [Serializable]
    internal class ProfileData
    {
        public string profileId;
        public string profileName;
        public int forgeDust = 2500;
        public int emberCore = 5;
        public int sigilToken = 20;
        public int insuranceSeal = 1;
        public string createdAtUtc;
        public string updatedAtUtc;
    }

    public class ProfileStorageService
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
                updatedAtUtc = now
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

        private ProfileIndexData LoadIndex()
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

        private void SaveIndex(ProfileIndexData index)
        {
            File.WriteAllText(IndexPath, JsonUtility.ToJson(index, true));
        }

        private string GetProfilePath(string profileId)
        {
            return Path.Combine(ProfilesFolder, $"profile_{profileId}.json");
        }
    }
}
