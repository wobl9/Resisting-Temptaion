using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ShatteredForge.Menu
{
    /// <summary>
    /// HTTP-backed profile storage with local JSON mirror for offline reads and durability.
    /// Expected server routes (relative to base URL): GET/PUT <c>index</c>, GET/PUT/DELETE <c>profiles/{profileId}</c>.
    /// </summary>
    public sealed class RemoteProfileStorage : IProfileStorage
    {
        private readonly string _baseUrl;
        private readonly string _authBearerToken;
        private readonly LocalJsonProfileStorage _local;

        public RemoteProfileStorage(string baseUrl, string authBearerToken, LocalJsonProfileStorage localMirror)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _authBearerToken = authBearerToken ?? string.Empty;
            _local = localMirror ?? throw new ArgumentNullException(nameof(localMirror));
        }

        public List<ProfileSummary> LoadProfiles(out string activeProfileId)
        {
            var indexJson = HttpGet(BuildUrl("index"));
            if (!string.IsNullOrEmpty(indexJson))
            {
                var remoteIndex = JsonUtility.FromJson<ProfileIndexData>(indexJson);
                if (remoteIndex != null)
                {
                    _local.SaveIndex(remoteIndex);
                    activeProfileId = remoteIndex.activeProfileId ?? string.Empty;
                    return remoteIndex.profiles ?? new List<ProfileSummary>();
                }
            }

            return _local.LoadProfiles(out activeProfileId);
        }

        public string CreateProfile(string displayName)
        {
            var profileId = _local.CreateProfile(displayName);
            if (!_local.TryLoadProfile(profileId, out var created))
            {
                return profileId;
            }

            TryHttpPutProfile(created);
            TryHttpPutIndex(_local.LoadIndex());
            return profileId;
        }

        public void SetActiveProfile(string profileId)
        {
            _local.SetActiveProfile(profileId);
            TryHttpPutIndex(_local.LoadIndex());
        }

        public bool HasAnyProfile()
        {
            return LoadProfiles(out _).Count > 0;
        }

        public bool TryLoadProfile(string profileId, out ProfileData profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return false;
            }

            var body = HttpGet(BuildUrl($"profiles/{profileId}"));
            if (!string.IsNullOrEmpty(body))
            {
                var data = JsonUtility.FromJson<ProfileData>(body);
                if (data != null && !string.IsNullOrWhiteSpace(data.profileId))
                {
                    profile = data;
                    _local.PersistProfileSnapshot(data);
                    return true;
                }
            }

            return _local.TryLoadProfile(profileId, out profile);
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
            TryHttpPutProfile(profile);
            _local.PersistProfileSnapshot(profile);
        }

        public bool DeleteProfile(string profileId)
        {
            var ok = _local.DeleteProfile(profileId);
            if (!ok)
            {
                return false;
            }

            HttpDelete(BuildUrl($"profiles/{profileId}"));
            TryHttpPutIndex(_local.LoadIndex());
            return true;
        }

        private string BuildUrl(string relativePath)
        {
            var trimmed = relativePath.TrimStart('/');
            return $"{_baseUrl}/{trimmed}";
        }

        private void ApplyAuth(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(_authBearerToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + _authBearerToken);
            }
        }

        private string HttpGet(string url)
        {
            using var request = UnityWebRequest.Get(url);
            ApplyAuth(request);
            var op = request.SendWebRequest();
            while (!op.isDone)
            {
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"RemoteProfileStorage GET failed: {url} ({request.error})");
                return null;
            }

            return request.downloadHandler?.text;
        }

        private bool HttpPutJson(string url, string jsonBody)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonBody ?? string.Empty);
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            ApplyAuth(request);
            var op = request.SendWebRequest();
            while (!op.isDone)
            {
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"RemoteProfileStorage PUT failed: {url} ({request.error})");
                return false;
            }

            return true;
        }

        private void HttpDelete(string url)
        {
            using var request = UnityWebRequest.Delete(url);
            ApplyAuth(request);
            var op = request.SendWebRequest();
            while (!op.isDone)
            {
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"RemoteProfileStorage DELETE failed: {url} ({request.error})");
            }
        }

        private void TryHttpPutProfile(ProfileData profile)
        {
            var url = BuildUrl($"profiles/{profile.profileId}");
            HttpPutJson(url, JsonUtility.ToJson(profile, true));
        }

        private void TryHttpPutIndex(ProfileIndexData index)
        {
            HttpPutJson(BuildUrl("index"), JsonUtility.ToJson(index, true));
        }
    }
}
