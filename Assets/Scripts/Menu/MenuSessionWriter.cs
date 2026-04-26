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

        public static void SetPendingDungeonEntry(bool pending)
        {
            if (pending)
            {
                PlayerPrefs.SetInt(MenuSessionPrefs.PendingDungeonEntryKey, 1);
            }
            else
            {
                PlayerPrefs.DeleteKey(MenuSessionPrefs.PendingDungeonEntryKey);
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Returns whether the key was set to 1 and clears it.
        /// </summary>
        public static bool ConsumePendingDungeonEntry()
        {
            var v = PlayerPrefs.GetInt(MenuSessionPrefs.PendingDungeonEntryKey, 0) == 1;
            if (v)
            {
                PlayerPrefs.DeleteKey(MenuSessionPrefs.PendingDungeonEntryKey);
                PlayerPrefs.Save();
            }

            return v;
        }
    }
}
