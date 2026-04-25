using UnityEngine;

namespace ShatteredForge.Menu
{
    public static class MenuSessionWriter
    {
        public static void WriteGameplayLaunchIntent(string profileId, bool resumeExpedition)
        {
            PlayerPrefs.SetString(MenuSessionPrefs.ActiveProfileIdKey, profileId);
            PlayerPrefs.SetInt(MenuSessionPrefs.ResumeExpeditionKey, resumeExpedition ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ClearResumeIntent()
        {
            PlayerPrefs.DeleteKey(MenuSessionPrefs.ResumeExpeditionKey);
            PlayerPrefs.Save();
        }
    }
}
