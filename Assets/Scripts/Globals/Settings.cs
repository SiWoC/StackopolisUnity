using UnityEngine;

namespace SiWoC.Stackopolis.Globals
{
    public static class Settings
    {
        const string PrefHighScoreFloors = "HighScoreFloors";

        public static int HighScoreFloors => PlayerPrefs.GetInt(PrefHighScoreFloors, 0);

        /// <summary>
        /// Writes the high score only when <paramref name="floors"/> is higher than the stored value.
        /// </summary>
        /// <returns>True if a new high score was stored.</returns>
        public static bool TrySetHighScoreFloors(int floors)
        {
            if (floors <= HighScoreFloors)
                return false;

            PlayerPrefs.SetInt(PrefHighScoreFloors, floors);
            PlayerPrefs.Save();
            return true;
        }
    }
}
