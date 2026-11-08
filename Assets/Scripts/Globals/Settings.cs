using UnityEngine;

namespace SiWoC.Stackopolis.Globals
{
    public static class Settings
    {
        const string PrefHighScoreFloors = "HighScoreFloors";
        const string PrefHighScorePopulation = "HighScorePopulation";
        const string PrefDifficulty = "Difficulty";

        public static Difficulty GetDifficulty()
        {
            return (Difficulty)PlayerPrefs.GetInt(PrefDifficulty, (int)Difficulty.Hard);
        }

        public static void SetDifficulty(Difficulty difficulty)
        {
            PlayerPrefs.SetInt(PrefDifficulty, (int)difficulty);
            PlayerPrefs.Save();
        }

        public static int GetHighScoreFloors(Difficulty difficulty)
        {
            return PlayerPrefs.GetInt(PrefHighScoreFloors + difficulty, 0);
        }

        public static int GetHighScorePopulation(Difficulty difficulty)
        {
            return PlayerPrefs.GetInt(PrefHighScorePopulation + difficulty, 0);
        }

        /// <summary>
        /// Writes the high score for <paramref name="difficulty"/> only when
        /// <paramref name="floors"/> is higher than the stored value.
        /// Stores <paramref name="population"/> beside that floors record.
        /// </summary>
        /// <returns>True if a new high score was stored.</returns>
        public static bool TrySetHighScoreFloors(int floors, int population, Difficulty difficulty)
        {
            if (floors <= GetHighScoreFloors(difficulty))
                return false;

            PlayerPrefs.SetInt(PrefHighScoreFloors + difficulty, floors);
            PlayerPrefs.SetInt(PrefHighScorePopulation + difficulty, population);
            PlayerPrefs.Save();
            return true;
        }
    }
}
