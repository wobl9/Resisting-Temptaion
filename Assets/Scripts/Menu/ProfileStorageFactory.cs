namespace ShatteredForge.Menu
{
    public enum ProfileStorageMode
    {
        Local,
        Remote
    }

    public static class ProfileStorageFactory
    {
        /// <summary>
        /// Creates profile storage. Remote mode requires a non-empty base URL; otherwise falls back to local.
        /// </summary>
        public static IProfileStorage Create(
            ProfileStorageMode mode,
            string remoteBaseUrl = null,
            string authBearerToken = null)
        {
            var local = new LocalJsonProfileStorage();
            if (mode != ProfileStorageMode.Remote || string.IsNullOrWhiteSpace(remoteBaseUrl))
            {
                return local;
            }

            var normalized = remoteBaseUrl.Trim().TrimEnd('/');
            return new RemoteProfileStorage(normalized, authBearerToken, local);
        }
    }
}
