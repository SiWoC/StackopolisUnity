using UnityEngine;

namespace SiWoC.Stackopolis.Globals
{
    public static class Settings
    {
        const string PrefHighScoreFloors = "HighScoreFloors";

        public static int GetHighScoreFloors(Difficulty difficulty)
        {
            return PlayerPrefs.GetInt(PrefHighScoreFloors + difficulty, 0);
        }

        /// <summary>
        /// Writes the high score for <paramref name="difficulty"/> only when
        /// <paramref name="floors"/> is higher than the stored value.
        /// </summary>
        /// <returns>True if a new high score was stored.</returns>
        public static bool TrySetHighScoreFloors(int floors, Difficulty difficulty)
        {
            if (floors <= GetHighScoreFloors(difficulty))
                return false;

            PlayerPrefs.SetInt(PrefHighScoreFloors + difficulty, floors);
            PlayerPrefs.Save();
            return true;
        }
    }
}
